namespace ME14Server
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Net;
    using System.Text;
    using System.Threading.Tasks;
    using DotNetty.Transport.Channels;
    

    public class ME14ServerHandler : SimpleChannelInboundHandler<string>
    {
        static string servername = "me14emulator";
        bool linestatus = false;
        Dictionary<int, string> lastObservations;
        public static string Identity
        {
            get
            {
                string date = DateTime.Now.ToString("yyyy-MM-dd  HH:mm");
                return $"{date} 01 {servername}";
            }
        }

        static string AsFieldString(string code, string value)
        {
            StringBuilder sb = new StringBuilder();
            if (value.Length > 6) throw new ArgumentException($"Argument length too big '{code}:{value.Length}'");
            sb.Append(code).Append(value.PadLeft(6, ' '));
            return sb.ToString();
        }

        static Dictionary<int,string> RandomObservations()
        {
            Dictionary<int, string> ro = new Dictionary<int, string>();
            Random r = new Random();
            
            for (int i = 0; i < 40; i++)
            {
                double value = r.Next(1, 100) + r.NextDouble();
                if (r.Next(0, 10) == 0) { value = -value; }
                ro.Add(i, value.ToString("F", CultureInfo.InvariantCulture));
            }

            return ro;
        }

        static string GenerateME14Message(bool history, Dictionary<int,string> observations)
        {
            string date = DateTime.Now.ToString("yyyy-MM-dd  HH:mm");
            string mtype = (history ? "H" : "M");
            string header = $"{date},01,{mtype}14,{servername}";


            StringBuilder obs = new StringBuilder();
            List<int> keys = observations.Keys.ToList();
            keys.Sort();

            int column = 1;
            foreach (var item in keys)
            {
                if (column == 8)
                {
                    obs.Append("\r\n");
                    column = 1;
                }
                obs.Append(AsFieldString(item.ToString().PadLeft(2, '0'), observations[item])).Append(";");
                column++;
            }

            var obsbytes = Encoding.ASCII.GetBytes(obs.ToString());
            int chksum = 0;
            for (int i = 0; i < obsbytes.Length; i++)
            {
                chksum += obsbytes[i];
            }

            var CheckSum = chksum.ToString("X").ToUpper();

            obs.Append("\r\n=");
            string message = "";

            if (history)
            {
                message = $"\r\n#\r\n\r\n{header}\r\n{obs.ToString()}\r\n\r\n{CheckSum}\r\n\r\n#\r\n=";
            } else
            {
                message = $"MES14\r\n\r\n{header}\r\n{obs.ToString()}\r\n{CheckSum}\r\n";
            }

            return message;
            
        }

       /* public override void ChannelActive(IChannelHandlerContext contex)
        {
            contex.WriteAsync(string.Format("Welcome to {0} !\r\n", Dns.GetHostName()));
            contex.WriteAndFlushAsync(string.Format("It is {0} now !\r\n", DateTime.Now));
        }
        */
        protected override void ChannelRead0(IChannelHandlerContext contex, string msg)
        {
            // Generate and write a response.
            string response;
            bool close = false;
            if (string.IsNullOrEmpty(msg))
            {
                return;
            }
            else if (string.Equals("OPEN 1", msg, StringComparison.OrdinalIgnoreCase))
            {
                if (!linestatus)
                {
                    response = $"{Identity}\r\n\r\nLINE A OPENED\r\n\r\n>";
                    linestatus = true;
                }  else
                {
                    return;
                }
            }
            else if (string.Equals("CLOSE", msg, StringComparison.OrdinalIgnoreCase))
            {
                if (linestatus)
                {
                    response = $"\r\n{Identity}\r\n\r\nLINE A CLOSED";
                    linestatus = false;
                }
                else
                {
                    return;
                }
            }
            else if (string.Equals("ME14", msg, StringComparison.OrdinalIgnoreCase))
            {
                if (linestatus)
                {
                    lastObservations = RandomObservations();
                    response = GenerateME14Message(false, lastObservations)+"\r\n>";                    
                }
                else
                {
                    return;
                }
            }
            else if (string.Equals("HIST", msg, StringComparison.OrdinalIgnoreCase))
            {
                if (linestatus)
                {
                    if (lastObservations == null) lastObservations = RandomObservations();
                    response = GenerateME14Message(true, lastObservations) + "\r\n>";
                }
                else
                {
                    return;
                }
            } else
            {
                return;
            }
            

            Task wait_close = contex.WriteAndFlushAsync(response);
            if (close)
            {
                Task.WaitAll(wait_close);
                contex.CloseAsync();
            }
        }

        public override void ChannelReadComplete(IChannelHandlerContext contex)
        {
            contex.Flush();
        }

        public override void ExceptionCaught(IChannelHandlerContext contex, Exception e)
        {
            Console.WriteLine("{0}", e.StackTrace);
            contex.CloseAsync();
        }

        public override bool IsSharable => true;
    }
}
