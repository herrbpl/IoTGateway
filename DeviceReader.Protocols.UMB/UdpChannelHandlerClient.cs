using DotNetty.Buffers;
using DotNetty.Transport.Channels;
using DotNetty.Transport.Channels.Sockets;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DeviceReader.Protocols.UMB
{
    class MyClass: ChannelDuplexHandler
    {
        
    }

    class UdpChannelHandlerClient : SimpleChannelInboundHandler<DatagramPacket>
    {
        private IChannelHandlerContext ctx;
        
        private TaskCompletionSource<Frame> tcs;
        

        public override void ChannelActive(IChannelHandlerContext ctx)
        {
            this.ctx = ctx;
            /*
            // ask device 
            var frame = new Frame(new FrameAddress(DeviceClass.Class_15_Master, 0x01), new FrameAddress(DeviceClass.Class_7_CompactWeatherStation, 0x01),
                0x20, new byte[] { });

            var buf2 = Unpooled.CopiedBuffer(frame.Data);
            //var buf = Unpooled.CopiedBuffer("MMUUUUUU!!", Encoding.UTF8);
            var dg = new DatagramPacket(buf2, ctx.Channel.RemoteAddress);
            
            //var d = new DatagramPacket()
            ctx.WriteAndFlushAsync(dg);
            */
        }

        protected override void ChannelRead0(IChannelHandlerContext ctx, DatagramPacket msg)
        {
            Console.WriteLine($"Client Received => {msg}");

            if (!msg.Content.IsReadable())
            {
                return;
            }

            Frame frame = null;
            try
            {
                var buf = new byte[msg.Content.ReadableBytes];
                msg.Content.GetBytes(0, buf);
                frame = new Frame(buf);
                Console.WriteLine(frame.ToString());
            } catch (Exception e)
            {
                Console.WriteLine($"Got data: {BitConverter.ToString(msg.Content.Array)}");
                Console.WriteLine($"Error while veryifying frame: {e}");
                tcs?.TrySetException(e);
            }
                        
            tcs?.TrySetResult(frame);                        
        }
       

        private async Task<Frame> WaitFrame(int delay)
        {
            await Task.Delay(delay);
            /*
            // cancel tcs
            if (tcs != null && tcs.Task.Status == TaskStatus.Running)
            {
                tcs.TrySetException(new TimeoutException("Frame wait timeout exceeded"));
            } 
            */
            return null;
        }

        public async Task<string> SendRequest(ushort sender, ushort receiver, byte cmd, byte[] payload )
        {
            
            // retry logic ? Esp for powersave mode 2 this is needed. 

            if (ctx == null) throw new Exception("Connection not open!");

            if (tcs != null) throw new Exception("Cannot call two requests simultanously!");

            var frame = new Frame(new FrameAddress(sender), new FrameAddress(receiver), cmd, payload);

            // wait for answer
            var buf = Unpooled.CopiedBuffer(frame.Data);
            //var buf = Unpooled.CopiedBuffer("MMUUUUUU!!", Encoding.UTF8);

            Console.WriteLine($"Remote address is {ctx.Channel?.RemoteAddress.ToString()}");

            var dg = new DatagramPacket(buf, ctx.Channel.RemoteAddress);
            
            string result = "";

           
            int retrycount = 3;
            int delay = 1000;
            
            while (retrycount > 0)
            {
                retrycount--;
                try
                {
                    tcs = new TaskCompletionSource<Frame>();

                    await ctx.WriteAndFlushAsync(dg);
                    /*
                    await Task.WhenAny(tcs.Task, Task.Delay(10000));

                    if (tcs.Task.IsCompletedSuccessfully)
                    {
                        Console.WriteLine("Got result!");
                        result = tcs.Task.Result.ToString();
                        break;
                    }
                    */
                    var ttt = await Task.WhenAny(tcs.Task, Task.Run(async () => await WaitFrame(delay))); // what happens to this task? Just sits idle on background?
                    if (ttt.Equals(tcs.Task))
                    {
                        Console.WriteLine("Got result!");
                        result = ttt.Result.ToString();
                        break;
                    } else
                    {
                        // wait expired
                        Console.WriteLine($"Got timeout! Retrying {retrycount} more times.");
                        tcs = null;
                    }
                    
                   
                }
                catch (Exception e)
                {
                    Console.WriteLine($"oioioi: {e}");
                }
            }
            tcs = null;

            return result;

        }
        

        public override void ChannelInactive(IChannelHandlerContext context)
        {
            Console.WriteLine("Channel inactive!");
            base.ChannelInactive(context);
        }


        public override void ExceptionCaught(IChannelHandlerContext context, Exception exception)
        {
            Console.WriteLine("Exception: " + exception);
            tcs?.TrySetException(exception);
            context.CloseAsync();
        }
    }
}
