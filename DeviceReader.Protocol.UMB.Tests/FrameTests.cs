using System;
using Xunit;
using DeviceReader.Protocols.UMB;
using Xunit.Abstractions;

namespace DeviceReader.Protocols.UMB.Tests
{
    public class FrameTests
    {
        //private CRCManager _manager = new CRCManager();

        private readonly ITestOutputHelper output;

        public FrameTests(ITestOutputHelper output)
        {
            this.output = output;            
        }

        [Fact]
        public void TestFrameCreation()
        {
            // Test if valid frame passes
            // request frame example from UMB v1.0 protocol specification
            var fd1 = new byte[] { Frame.SOH, Frame.VER, 0x01, 0x70, 0x16, 0xF0, 0x07, Frame.STX, 0x2F, 0x10, 0x02, 0x64, 0x00, 0xC8, 0x00, Frame.ETX, 0x1F, 0xC7, Frame.EOT };
            
            var f1 = new Frame(fd1); 
            Assert.NotNull(f1);

            var f2 = new Frame(new FrameAddress(DeviceClass.Class_15_Master, 0x16), new FrameAddress(DeviceClass.Class_7_CompactWeatherStation, 0x01),
                0x2F, new byte[] { 0x02, 0x64, 0x00, 0xC8, 0x00 });

            output.WriteLine(f1.ToString());
            output.WriteLine(f2.ToString());


            Assert.Equal(f1.ToString(), f2.ToString());

            Assert.True(f1.Equals(f2));

            var f3 = new Frame(new FrameAddress(DeviceClass.Class_15_Master, 0x01), new FrameAddress(DeviceClass.Class_7_CompactWeatherStation, 0x01),
                0x20, new byte[] { });
            output.WriteLine($"Is little endian? {BitConverter.IsLittleEndian}");
            Assert.NotNull(f3);
            output.WriteLine($"F3 is [{BitConverter.ToString(f3.Data)}]");

        }


        [Fact]
        public void TestFrameHeader()
        {

            // Test if valid frame passes
            // request frame example from UMB v1.0 protocol specification
            var fd1 = new byte[] { Frame.SOH, Frame.VER, 0x01, 0x70, 0x16, 0xF0, 0x07, Frame.STX, 0x2F, 0x10, 0x02, 0x64, 0x00, 0xC8, 0x00, Frame.ETX, 0x1F, 0xC7, Frame.EOT };
            var f1 = new Frame(fd1); 
            Assert.NotNull(f1);
         

            // test frame start            
            Assert.Throws<FrameValidationException>(() => {
                var framedata = new byte[] { 0x10, 0x0, 0x2, 0x3, 0x4, 0x5, 0x6, 0x7, 0x8, 0x9, 0x10, 0x11, 0x12 };
                var frame = new Frame(framedata); 
            });

            // test version
            Assert.Throws<FrameVersionUnsupportedException>(() => {
                var framedata = new byte[] { Frame.SOH, 0x20, 0x2, 0x3, 0x4, 0x5, 0x6, 0x7, 0x8, 0x9, 0x10, 0x11, 0x12 };
                var frame = new Frame(framedata); 
            });

            // test STX
            Assert.Throws<FrameValidationException>(() => {
                var framedata = new byte[] { Frame.SOH, Frame.VER, 0x70, 0x01, 0xF0, 0x01, 0x4, 0x7, 0x00, 0x9, 0x10, 0x11, 0x12, 0x00, 0x00, 0x00 };
                var frame = new Frame(framedata); 
            });

            // test ETX
            Assert.Throws<FrameValidationException>(() => {
                var framedata = new byte[] { Frame.SOH, Frame.VER, 0x70, 0x01, 0xF0, 0x01, 0x4, Frame.STX, 0x23, 0x10, 0x01, 0x01, 0x12, 0x00, 0x00, 0x00 };
                var frame = new Frame(framedata);
            });

            
        }


        [Fact] 
        public void TestFrameLength()
        {
            // test 
            Assert.Throws<FrameIncompleteException>(() => { var frame = new Frame(new byte[2]); frame.Validate(); });

            // check frame length            
            Assert.Throws<FrameIncompleteException>(() => {
                var framedata = new byte[] { Frame.SOH, Frame.VER, 0x70, 0x01, 0xF0, 0x01, 0x4, 0x7, 0x8, 0x9, 0x10, 0x11, 0x12 };
                var frame = new Frame(framedata); 
            });

        }



        [Fact]
        public void TestCrc16Calculation()
        {
            ushort expexted = 0xF843;
            byte[] data = new byte[] { 0x30, 0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37 };            
            ushort crc2 = Frame.Crc16(data);
            output.WriteLine($"data [{BytesToHexString(data)}], expected crc [{BitConverter.ToString(BitConverter.GetBytes(expexted))}], got [{BitConverter.ToString(BitConverter.GetBytes(crc2))}]");
            Assert.Equal(expexted, crc2);

        }

        [Fact]
        public void TestFrameAddressConstrutor()
        {
            var masteraddress = new FrameAddress(DeviceClass.Class_15_Master, 0x01);

            ushort expectedmasteraddress = 0xF001;

            var deviceaddress = new FrameAddress(DeviceClass.Class_7_CompactWeatherStation, 0xf1);

            ushort expecteddeviceaddress = 0x70f1;

            Assert.Equal(expectedmasteraddress, masteraddress.Address);
            Assert.Equal(expecteddeviceaddress, deviceaddress.Address);

            Assert.Equal(DeviceClass.Class_15_Master, masteraddress.DeviceClass);
            Assert.Equal(DeviceClass.Class_7_CompactWeatherStation, deviceaddress.DeviceClass);

            Assert.Equal(0x01, masteraddress.DeviceId);
            Assert.Equal(0xf1, deviceaddress.DeviceId);


        }


        private string BytesToHexString(byte[] HexArray)
        {
            if (HexArray == null || HexArray.Length <= 0)
            {
                throw new ArgumentNullException("HexArray");
            }

            var result = BitConverter.ToString(HexArray);
            return result;
        }
    }
}
