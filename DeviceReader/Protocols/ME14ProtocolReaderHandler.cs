using System;
using System.Collections.Generic;
using System.Text;

namespace DeviceReader.Protocols
{
    using System;
    using System.Threading.Tasks;
    using DotNetty.Transport.Channels;

    class ME14ProtocolReaderHandler: SimpleChannelInboundHandler<string>
    {

        enum MessageType
        {
            MSG_NONE,
            MSG_ME14,
            MSG_HIST
        };

        TaskCompletionSource<int> _marker;
        
        ME14RetOptions _retrieve;
        bool LineOpen = false;

        MessageType messageType = MessageType.MSG_NONE;
        StringBuilder completemessage = new StringBuilder();
        Action<string> _setResult = null;


        public ME14ProtocolReaderHandler(TaskCompletionSource<int> marker, ME14RetOptions retrieve, Action<string> setResult) : base()
        {

            _marker = marker;
            _retrieve = retrieve;
            _setResult = setResult;
        }

        public override void ChannelActive(IChannelHandlerContext contex)
        {
            
            contex.WriteAndFlushAsync("OPEN 1\r\n");            
        }

        private void setResult(string input)
        {
            if (_setResult != null)
            {
                _setResult(input);
            }

        }

        protected override void ChannelRead0(IChannelHandlerContext contex, string msg)
        {           

            msg = msg.Trim().Replace(Char.ConvertFromUtf32(7), "");           

            if (_marker.Task.Status == TaskStatus.Canceled || _marker.Task.Status == TaskStatus.RanToCompletion || _marker.Task.Status == TaskStatus.Faulted)
            {
                return;
            }
          
            if (!LineOpen)
            {
                if (msg.Equals("LINE A OPENED"))
                {
                    LineOpen = true;
                }
                else
                {
                    //Console.WriteLine($"Discarding input '{msg}'");
                }

            }
            else
            {

                if (msg.Equals(">"))
                {
                    if (messageType == MessageType.MSG_NONE)
                    {
                        //Console.WriteLine("Should send MES14 or HIST");
                        completemessage.Clear();

                        // sending MES14
                        if (_retrieve == ME14RetOptions.MES14)
                        {
                            contex.WriteAndFlushAsync("MES 14" + "\r\n").Wait();
                            messageType = MessageType.MSG_ME14;
                        }
                        else
                        {
                            contex.WriteAndFlushAsync("HIST" + "\r\n").Wait();
                            messageType = MessageType.MSG_HIST;
                        }

                    }
                    else if (messageType == MessageType.MSG_HIST || messageType == MessageType.MSG_ME14)
                    {
                        // Complete history message
                        var response = completemessage.ToString();
                        
                        // setting response in calling object
                        setResult(response);

                        response = null;

                        completemessage.Clear();
                        messageType = MessageType.MSG_NONE;

                        // Sending close
                        contex.WriteAndFlushAsync("CLOSE" + "\r\n").Wait();
                    }
                    else
                    {
                        //Console.WriteLine($"Discarding input '{msg}'");
                    }
                }
                else if (msg.Equals("LINE A CLOSED"))
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
                    }
                    else
                    {
                        //Console.WriteLine($"Discarding input '{msg}'");
                    }
                }

            }

        }
       

        public override void ExceptionCaught(IChannelHandlerContext contex, Exception e)
        {            
            contex.CloseAsync();
            _marker.SetException(e);
        }

    }
}
