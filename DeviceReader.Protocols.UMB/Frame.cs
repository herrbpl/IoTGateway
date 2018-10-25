using System;
using System.Collections.Generic;
using System.Text;
using DotNetty.Buffers;

namespace DeviceReader.Protocols.UMB
{


    public enum DeviceClass : byte
    {
        Class_0_Broadcast = 0x00,
        Class_1_RoadSensor = 0x10,
        Class_2_RainSensor = 0x20,
        Class_3_VisibilitySensor = 0x30,
        Class_4_ActiveRoadSensor = 0x40,
        Class_5_NonInvasiveRoadSensor = 0x50,
        Class_6_UniversalMeasurementTransmitter = 0x60,
        Class_7_CompactWeatherStation = 0x70,
        Class_8_WindSensor = 0x80,
        Class_15_Master = 0xF0        
    }

    public class FrameAddress
    {
        public DeviceClass DeviceClass { get => (DeviceClass)(_address >> 8); }
        public byte DeviceId { get => (byte)(0x00FF & _address); }
        public ushort Address { get => _address; }

        private ushort _address;

        public FrameAddress(DeviceClass classId, byte deviceId)
        {
            _address = (ushort) (((byte)classId << 8) + deviceId);
        }

        public FrameAddress(ushort address)
        {
            _address = address;
        }

    }

    // how to differenticate between request and response payloads?
    // Only way is to know expected payload length for each command


    /// <summary>
    /// Lots of this is converted from https://github.com/pklaus/opus20
    /// </summary>

    public class Frame
    {
        public const byte SOH = 0x01;
        public const byte STX = 0x02;
        public const byte ETX = 0x03;
        public const byte EOT = 0x04;
        public const byte VER = 0x10;

        // frame info        
        private byte[] data = new byte[] { };
        private ushort _sender;
        private ushort _receiver;
        private ushort _crc;
        private byte _cmd;
        private byte _cmdversion;
        private ArraySegment<byte> _payload;


        // public properties
        public FrameAddress Receiver { get => new FrameAddress(_receiver); }
        public FrameAddress Sender { get => new FrameAddress(_sender); }
        public byte Command { get => _cmd; }
        public byte CommandVersion { get => _cmdversion; }
        public byte[] Payload { get => (_payload != null ? this._payload.ToArray() : new byte[] { }); }
        public ushort Checksum { get => _crc; }
        public byte[] Data { get => data; } // probably should make copy

        // construct frame
        public Frame(FrameAddress sender, FrameAddress receiver, byte cmd, byte[] payload)
        {
            this.data = new byte[12 + payload.Length+2];
            data[0] = SOH;
            data[1] = VER;

            // LO-HI
            data[2] = receiver.DeviceId;
            data[3] = (byte)receiver.DeviceClass;
            data[4] = sender.DeviceId;
            data[5] = (byte)sender.DeviceClass;            
            data[6] = (byte)(payload.Length + 2);
            data[7] = STX;
            data[8] = cmd;
            data[9] = 0x10;
            Array.Copy(payload, 0, data, 10, payload.Length);
            data[10 + payload.Length] = ETX;

            var crc = Crc16((new ArraySegment<byte>(data, 0, 11 + payload.Length)).ToArray());

            Array.Copy(BitConverter.GetBytes(crc), 0, data, 11 + payload.Length, 2);

            data[13 + payload.Length] = EOT;            
            this.Validate();
        }
        
        public Frame(byte[] data)
        {
            this.data = data;            
            this.Validate();
        }

        /// <summary>
        /// Validate frame (but not payload) generated from datagram.
        /// </summary>
        /// <returns></returns>
        public bool Validate()
        {
            var data = this.data;

            // check total length.
            if (data.Length < 12) throw new FrameIncompleteException($"Expected at least 12 bytes, got {data.Length}");

            // check start.
            if (data[0] != SOH) throw new FrameValidationException($"Expected SOH at beginning at frame, got {string.Format("{0:X}", data[0])}");

            // check protocol
            if (data[1] != VER) throw new FrameVersionUnsupportedException($"Expected version 1 at beginning at frame, got {string.Format("{0:X}", data[1])}");

            // check length.
            var length = data[6];
            if (data.Length < (12+length)) throw new FrameIncompleteException($"Expected at least ({12+length}) bytes, got {data.Length}");

            // check stx
            if (data[7] != STX) throw new FrameValidationException($"Expected STX at offet 7, got {string.Format("{0:X}", data[7])}");

            this._receiver = BitConverter.ToUInt16(data, 2);
            this._sender = BitConverter.ToUInt16(data, 4);

            this._cmd = data[8];
            this._cmdversion = data[9];

            this._payload = new ArraySegment<byte>(data, 10, length - 2);

            // check if ETX is ok
            if (data[8+length] != ETX) throw new FrameValidationException($"Expected ETX at offet {8+length}, got {string.Format("{0:X}", data[8+length])}");


            // after ETX, three bytes must be.
            var crcsource = (new ArraySegment<byte>(data, 0, 9+length)).ToArray();
            var crcourcestr = BitConverter.ToString(crcsource); 
            var calculated_crc = Crc16(crcsource);


            // Byte order in frame is LO HI
            var received_crc_bytes = (new ArraySegment<byte>(data, 9 + length, 2)).ToArray();
            //Array.Reverse(received_crc_bytes);
            
            var frame_crc = BitConverter.ToUInt16(received_crc_bytes, 0);

            if (calculated_crc != frame_crc) throw new FrameValidationException($"Expected CRC '{string.Format("{0:X}",calculated_crc)}' at offet {9 + length}, got {string.Format("{0:X}", frame_crc)}");
            this._crc = frame_crc;
            // check eot.
            if (data[11+length] != EOT) throw new FrameValidationException($"Expected EOT at offet {11 + length}, got {string.Format("{0:X}", data[11+length])}");


            return true;

        }

        public override string ToString()
        {
            return $"UMB Frame: [len: {data.Length}, crc16:{string.Format("{0:X}", Checksum)} to:{string.Format("{0:X}",Receiver.Address)} from:{string.Format("{0:X}", Sender.Address)} cmd:{string.Format("{0:X}", Command)}, payload: {BitConverter.ToString(Payload)}]";
        }

        public override bool Equals(object obj)
        {
            return (obj is Frame && obj != null && ((Frame)obj).Checksum == this.Checksum);
        }

        /// <summary>
        /// Calculates CRC16CCIT checksum
        /// from https://github.com/pklaus/opus20
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public static ushort Crc16(byte[] data)
        {
            var crc16_table = new ushort[] {
               0x0000, 0x1189, 0x2312, 0x329B, 0x4624, 0x57AD, 0x6536, 0x74BF,
               0x8C48, 0x9DC1, 0xAF5A, 0xBED3, 0xCA6C, 0xDBE5, 0xE97E, 0xF8F7,
               0x1081, 0x0108, 0x3393, 0x221A, 0x56A5, 0x472C, 0x75B7, 0x643E,
               0x9CC9, 0x8D40, 0xBFDB, 0xAE52, 0xDAED, 0xCB64, 0xF9FF, 0xE876,
               0x2102, 0x308B, 0x0210, 0x1399, 0x6726, 0x76AF, 0x4434, 0x55BD,
               0xAD4A, 0xBCC3, 0x8E58, 0x9FD1, 0xEB6E, 0xFAE7, 0xC87C, 0xD9F5,
               0x3183, 0x200A, 0x1291, 0x0318, 0x77A7, 0x662E, 0x54B5, 0x453C,
               0xBDCB, 0xAC42, 0x9ED9, 0x8F50, 0xFBEF, 0xEA66, 0xD8FD, 0xC974,
               0x4204, 0x538D, 0x6116, 0x709F, 0x0420, 0x15A9, 0x2732, 0x36BB,
               0xCE4C, 0xDFC5, 0xED5E, 0xFCD7, 0x8868, 0x99E1, 0xAB7A, 0xBAF3,
               0x5285, 0x430C, 0x7197, 0x601E, 0x14A1, 0x0528, 0x37B3, 0x263A,
               0xDECD, 0xCF44, 0xFDDF, 0xEC56, 0x98E9, 0x8960, 0xBBFB, 0xAA72,
               0x6306, 0x728F, 0x4014, 0x519D, 0x2522, 0x34AB, 0x0630, 0x17B9,
               0xEF4E, 0xFEC7, 0xCC5C, 0xDDD5, 0xA96A, 0xB8E3, 0x8A78, 0x9BF1,
               0x7387, 0x620E, 0x5095, 0x411C, 0x35A3, 0x242A, 0x16B1, 0x0738,
               0xFFCF, 0xEE46, 0xDCDD, 0xCD54, 0xB9EB, 0xA862, 0x9AF9, 0x8B70,
               0x8408, 0x9581, 0xA71A, 0xB693, 0xC22C, 0xD3A5, 0xE13E, 0xF0B7,
               0x0840, 0x19C9, 0x2B52, 0x3ADB, 0x4E64, 0x5FED, 0x6D76, 0x7CFF,
               0x9489, 0x8500, 0xB79B, 0xA612, 0xD2AD, 0xC324, 0xF1BF, 0xE036,
               0x18C1, 0x0948, 0x3BD3, 0x2A5A, 0x5EE5, 0x4F6C, 0x7DF7, 0x6C7E,
               0xA50A, 0xB483, 0x8618, 0x9791, 0xE32E, 0xF2A7, 0xC03C, 0xD1B5,
               0x2942, 0x38CB, 0x0A50, 0x1BD9, 0x6F66, 0x7EEF, 0x4C74, 0x5DFD,
               0xB58B, 0xA402, 0x9699, 0x8710, 0xF3AF, 0xE226, 0xD0BD, 0xC134,
               0x39C3, 0x284A, 0x1AD1, 0x0B58, 0x7FE7, 0x6E6E, 0x5CF5, 0x4D7C,
               0xC60C, 0xD785, 0xE51E, 0xF497, 0x8028, 0x91A1, 0xA33A, 0xB2B3,
               0x4A44, 0x5BCD, 0x6956, 0x78DF, 0x0C60, 0x1DE9, 0x2F72, 0x3EFB,
               0xD68D, 0xC704, 0xF59F, 0xE416, 0x90A9, 0x8120, 0xB3BB, 0xA232,
               0x5AC5, 0x4B4C, 0x79D7, 0x685E, 0x1CE1, 0x0D68, 0x3FF3, 0x2E7A,
               0xE70E, 0xF687, 0xC41C, 0xD595, 0xA12A, 0xB0A3, 0x8238, 0x93B1,
               0x6B46, 0x7ACF, 0x4854, 0x59DD, 0x2D62, 0x3CEB, 0x0E70, 0x1FF9,
               0xF78F, 0xE606, 0xD49D, 0xC514, 0xB1AB, 0xA022, 0x92B9, 0x8330,
               0x7BC7, 0x6A4E, 0x58D5, 0x495C, 0x3DE3, 0x2C6A, 0x1EF1, 0x0F78
            };
            ushort crc_buff = 0xffff;


            for(var i = 0; i<data.Length;i++)
            {
                var c = data[i];
                var x = crc16_table[c ^ (crc_buff & 0xFF)];
                var y = (ushort)(crc_buff >> 8);
                crc_buff = (ushort)(y ^ x);
            }

            return crc_buff;

        }

    }
}
