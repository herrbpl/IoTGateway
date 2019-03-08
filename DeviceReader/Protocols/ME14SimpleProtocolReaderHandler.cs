using System;
using System.Collections.Generic;
using System.Text;

namespace DeviceReader.Protocols
{
    using System;
    using System.Globalization;
    using System.Threading.Tasks;
    using DotNetty.Transport.Channels;

    class ME14SimpleProtocolReaderHandler: SimpleChannelInboundHandler<string>
    {

        enum MessageType
        {
            MSG_NONE,
            MSG_ME14,
            MSG_HIST
        };

        TaskCompletionSource<int> _marker;
                
        bool LineOpen = false;

        MessageType messageType = MessageType.MSG_NONE;
        StringBuilder completemessage = new StringBuilder();
        Action<string> _setResult = null;
        string _messageId = "";

        int msgsWithoutHeader = 0;

        public ME14SimpleProtocolReaderHandler(TaskCompletionSource<int> marker, string messageId, Action<string> setResult) : base()
        {
            _messageId = messageId;
            _marker = marker;            
            _setResult = setResult;
        }

        public override void ChannelActive(IChannelHandlerContext contex)
        {            
            contex.WriteAndFlushAsync($"\r\n@{_messageId} MES 14\r\n");            
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

            msg = msg.Trim(); //.Replace(Char.ConvertFromUtf32(7), "");           

            if (_marker.Task.Status == TaskStatus.Canceled || _marker.Task.Status == TaskStatus.RanToCompletion || _marker.Task.Status == TaskStatus.Faulted)
            {
                return;
            }


            if (msg.Length == 0) return;
            if (completemessage.Length == 0)
            {

                // try to parse input. 
                var headers = msg.Split(',');
                if (headers.Length != 4) // not message we are waiting for.
                {
                    msgsWithoutHeader++;
                    return;
                }

                const DateTimeStyles style = DateTimeStyles.AllowWhiteSpaces;

                DateTime timestamp;

                if (!DateTime.TryParseExact(headers[0].Replace("  ", " "), "yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture, style, out timestamp))
                {
                    msgsWithoutHeader++;
                    return;
                }
                if (msgsWithoutHeader > 10)
                {
                    contex.CloseAsync();
                    Exception e = new Exception("No header received in 10 messages, closing connection");
                    _marker.SetException(e);
                    return;
                }
                // it seems to be header
                completemessage.Append(msg + "\r\n");
            }
            else
            {

                if (msg.Equals("="))
                {
                    // interesting part is over, wrap it up and close connection
                    var response = completemessage.ToString();

                    // setting response in calling object
                    setResult(response);

                    // clear variables
                    completemessage.Clear();
                    response = null;

                    // close TCP connection
                    contex.CloseAsync();

                    // set signal to end waiting on calling thread
                    _marker.SetResult(0);
                }
                else
                {
                    completemessage.Append(msg + "\r\n");
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
