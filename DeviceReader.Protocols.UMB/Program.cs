using CommandLine;
using DotNetty.Codecs;
using DotNetty.Common.Internal.Logging;
using DotNetty.Handlers.Logging;
using DotNetty.Transport.Bootstrapping;
using DotNetty.Transport.Channels;
using DotNetty.Transport.Channels.Sockets;
using Microsoft.Extensions.Logging.Console;
using System;
using System.Net;
using System.Threading.Tasks;

namespace DeviceReader.Protocols.UMB
{

    [Verb("server", HelpText = "Server options")]
    class ServerSubOptions
    {
        [Option('a', "address", HelpText = "Listen on address", Default = "127.0.0.1")]
        public string Address { get; set; }
        
        [Option('p', "port", HelpText = "Listen on port", Default = 5000)]
        public int Port { get; set; }

        [Option('d', "delay", HelpText = "Time delay when sending response to command 'D0'", Default = 100)]
        public int Delay { get; set; }
    }

    [Verb("client", HelpText = "Client options")]
    class ClientSubOptions
    {
        [Option('a', "address", HelpText = "Contact to address", Default = "127.0.0.1")]
        public string Address { get; set; }

        [Option('p', "port", HelpText = "Connect to port", Default = 5000)]
        public int Port { get; set; }

        [Option('r', "receiver", HelpText = "Receiver as hex string (2 bytes)", Default = "7001")]
        public string Receiver { get; set; }

        [Option('s', "sender", HelpText = "sender as hex string (2 bytes)", Default = "F001")]
        public string Sender { get; set; }

        [Option('c', "command", HelpText = "Command as hex string (1 byte)", Default = "F001")]
        public string Command { get; set; }

        [Option('l', "payload", HelpText = "Payload as hex string ", Default = "")]
        public string Payload { get; set; }

        [Option('t', "timeout", HelpText = "Wait after command", Default = 100)]
        public uint Timeout { get; set; }

    }


    public class Program
    {

        static int Main(string[] args) {

            return CommandLine.Parser.Default.ParseArguments<ServerSubOptions, ClientSubOptions>(args)
                .MapResult(
                    (ServerSubOptions opts) => {
                        UMBServer.RunServerAsync(opts.Address, opts.Port, opts.Delay).Wait();
                        return 0;
                    },
                    (ClientSubOptions opts) => {

                        // try to coonvert.
                        if (opts.Receiver.Length != 4)
                        {
                            Console.WriteLine("Invalid Receiver value");
                            return -1;
                        }

                        //ushort receiver = BitConverter.ToUInt16(HexadecimalStringToByteArray_Original(opts.Receiver), 0);
                        

                        var sr = HexadecimalStringToByteArray_Original(opts.Receiver);
                        Array.Reverse(sr);
                        //ushort receiver = (ushort)((sr[1] << 8) + sr[0]);
                        ushort receiver = BitConverter.ToUInt16(sr, 0);


                        // Sender
                        if (opts.Sender.Length != 4)
                        {
                            Console.WriteLine("Invalid Sender value");
                            return -1;
                        }

                        var sb = HexadecimalStringToByteArray_Original(opts.Sender);
                        Array.Reverse(sb);

                        //ushort sender = (ushort)((sb[1] << 8) + sb[0]);
                        ushort sender = BitConverter.ToUInt16(sb, 0);

                        // Command
                        if (opts.Command.Length != 2)
                        {
                            Console.WriteLine("Invalid Command value");
                            return -1;
                        }

                        byte command = HexadecimalStringToByteArray_Original(opts.Command)[0];

                        // Payload
                        var payload = HexadecimalStringToByteArray_Original(opts.Payload);





                        UMBClient.RunClientAsync(opts.Address, opts.Port,receiver,sender,command,payload, opts.Timeout).Wait();
                        return 0;
                    },                    
                    errs => 1);
        }

        public static byte[] HexadecimalStringToByteArray_Original(string input)
        {
            var outputLength = input.Length / 2;
            var output = new byte[outputLength];
            for (var i = 0; i < outputLength; i++)
                output[i] = Convert.ToByte(input.Substring(i * 2, 2), 16);
            return output;
        }

    }
}
