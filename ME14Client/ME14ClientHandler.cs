using System;
using System.Collections.Generic;
using System.Text;

namespace ME14Client
{
    using System;
    using System.Threading.Tasks;
    using DotNetty.Transport.Channels;

    class ME14ClientHandler : SimpleChannelInboundHandler<string>
    {
        enum MessageType { 
            MSG_NONE,
            MSG_ME14,
            MSG_HIST
        };

        TaskCompletionSource<int> _marker;
        bool LineOpen = false;

        MessageType messageType = MessageType.MSG_NONE;

        StringBuilder completemessage = new StringBuilder();

        public ME14ClientHandler(TaskCompletionSource<int> marker) : base()
        {
            _marker = marker;
        }

        

        // on connection
        public override void ChannelActive(IChannelHandlerContext contex)
        {
            //contex.WriteAsync(string.Format("Welcome to {0} !\r\n", Dns.GetHostName()));
            //contex.WriteAndFlushAsync(string.Format("It is {0} now !\r\n", DateTime.Now));
            Console.WriteLine("Connected!");
            contex.WriteAndFlushAsync("OPEN 1\r\n");
        }

        protected override void ChannelRead0(IChannelHandlerContext contex, string msg)
        {

            byte[] ba = Encoding.Default.GetBytes(msg);
            var hexString = BitConverter.ToString(ba);
            //msg = msg.Trim().Replace("\r\n", "");
            Console.WriteLine($"Received: '{msg}' - {hexString}");

            msg = msg.Trim().Replace(Char.ConvertFromUtf32(7), "");

            ba = Encoding.Default.GetBytes(msg);
            hexString = BitConverter.ToString(ba);
            //msg = msg.Trim().Replace("\r\n", "");
            Console.WriteLine($"Received: '{msg}' - {hexString}");

            if (_marker.Task.Status == TaskStatus.Canceled || _marker.Task.Status == TaskStatus.RanToCompletion || _marker.Task.Status == TaskStatus.Faulted)
            {
                return;
            }

            //_marker.SetResult(0);

            //if (false) return;

            if (!LineOpen)
            {
                if (msg.Equals("LINE A OPENED"))
                {
                    LineOpen = true;
                } else
                {
                    Console.WriteLine($"Discarding input '{msg}'");
                }

              
               
            } else
            {

                if (msg.Equals(">"))
                {
                    if (messageType == MessageType.MSG_NONE)
                    {
                        Console.WriteLine("Should send MES14 or HIST");
                        completemessage.Clear();

                        // sending MES14
                        contex.WriteAndFlushAsync("HIST" + "\r\n").Wait();
                        messageType = MessageType.MSG_HIST;

                    } else if (messageType == MessageType.MSG_HIST || messageType == MessageType.MSG_ME14)
                    {
                        // Complete history message
                        var response = completemessage.ToString();
                        Console.WriteLine($"Response is '{response}'");
                        completemessage.Clear();
                        messageType = MessageType.MSG_NONE;
                        
                        // Sending close
                        contex.WriteAndFlushAsync("CLOSE" + "\r\n").Wait();
                    } else
                    {
                        Console.WriteLine($"Discarding input '{msg}'");
                    }
                } else if (msg.Equals("LINE A CLOSED"))
                {
                    LineOpen = false;
                    contex.CloseAsync();
                    _marker.SetResult(0);
                }

                else
                {
                    if (messageType == MessageType.MSG_HIST || messageType == MessageType.MSG_ME14)
                    {
                        completemessage.Append(msg + "\r\n");
                    } else
                    {
                        Console.WriteLine($"Discarding input '{msg}'");
                    }
                }

            }
            


        }

        
        private void ParseME14History(string msg)
        {
            Console.Write($"MES 14 HISTORY MESSAGE RECEIVED: \r\n{msg}");
        }

        private void ParseME14Message(string msg)
        {
            Console.Write($"MES 14 MESSAGE RECEIVED: \r\n{msg}");
        }

        public override void ExceptionCaught(IChannelHandlerContext contex, Exception e)
        {
            Console.WriteLine(DateTime.Now.Millisecond);
            Console.WriteLine("{0}", e.StackTrace);
            contex.CloseAsync();
            _marker.SetException(e);
        }
        
    }
}
