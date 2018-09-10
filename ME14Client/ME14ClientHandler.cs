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

            msg = msg.Trim().Replace("\r\n", "");
            Console.WriteLine($"Received: '{msg}'");
            if (_marker.Task.Status == TaskStatus.Canceled || _marker.Task.Status == TaskStatus.RanToCompletion || _marker.Task.Status == TaskStatus.Faulted)
            {
                return;
            }


            if (!LineOpen)
            {
                if (msg.Equals("\a>"))
                {
                    var response = completemessage.ToString();
                    Console.WriteLine($"Response is '{response}'");
                    completemessage.Clear();
                    LineOpen = true;
                    Task.Delay(200);
                    Console.WriteLine(">>>!!!OPENING LINE A!!!");
                    contex.WriteAndFlushAsync("MES14" + "\r\n").Wait();
                } else
                {
                    completemessage.Append(msg);
                }
              
               
            } else
            {



                if (msg.EndsWith(">"))
                {
                    var response = completemessage.ToString();
                    Console.WriteLine($"Response is '{response}'");
                    if (response.StartsWith("#"))
                    {
                        ParseME14History(response);
                    } else
                    {
                        ParseME14Message(response);
                    }

                    completemessage.Clear();

                    Task.Delay(200);
                    Console.WriteLine(">>>!!!CLOSING LINE A!!!");
                    contex.WriteAndFlushAsync("CLOSE" + "\r\n").Wait();


                } else if (msg.IndexOf("LINE A CLOSED") > 0)
                {
                    LineOpen = false;
                    _marker.SetResult(0);
                } else 
                {
                    completemessage.Append(msg + "\r\n");
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
