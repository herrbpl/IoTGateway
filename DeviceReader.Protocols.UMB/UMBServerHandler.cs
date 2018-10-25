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
    class UMBServerHandler : SimpleChannelInboundHandler<DatagramPacket>
    {
        private int timedelay = 100;
        public UMBServerHandler(int timedelay)
        {
            this.timedelay = timedelay;
        }

        protected override void ChannelRead0(IChannelHandlerContext ctx, DatagramPacket packet)
        {
            Console.WriteLine($"Server Received => {packet}");

            if (!packet.Content.IsReadable())
            {
                return;
            }

            try
            {

                var buf = new byte[packet.Content.ReadableBytes];
                packet.Content.GetBytes(0, buf);
                var frame = new Frame(buf);
                Console.WriteLine(frame.ToString());

                if (frame.Command == 0xD0)
                {
                    // compile response with delay. actually, should put this to queue somewhere..

                    Console.WriteLine($"Got D0, Sending response after {timedelay} ms");

                    
                    /*
                    Thread.Sleep(timedelay);
                    */
                    var response = new Frame(frame.Receiver, frame.Sender, frame.Command, new byte[] { 0x01 });
                    
                    
                    var x = Unpooled.CopiedBuffer(response.Data);
                    
                    
                    ctx.WriteAsync(new DatagramPacket(x, packet.Sender));
                
                    
                    
                    /* Async task run causes running thread to fail i guess.
                    Task.Run( async () => {

                        var response = new Frame(frame.Receiver, frame.Sender, frame.Command, new byte[] { 0x01 });
                        var buf2 = Unpooled.CopiedBuffer(response.Data);
                        var dg = new DatagramPacket(buf2, ctx.Channel.RemoteAddress);
                        await Task.Delay(timedelay);

                        try
                        {
                            Console.WriteLine($"Now sending packet {response.ToString()}");
                            if (ctx.Channel.IsWritable) {
                                await ctx.WriteAsync(dg);
                            } else
                            {
                                Console.WriteLine("Not writable!!!");
                            }
                        } catch(Exception e)
                        {
                            Console.WriteLine($"Oops: {e}");
                        }
                    });
                    */
                }


            }
            catch (Exception e)
            {                
                Console.WriteLine($"Error while veryifying frame: {e}");
            }

        }

        public override void ChannelReadComplete(IChannelHandlerContext context) => context.Flush();

        public override void ChannelInactive(IChannelHandlerContext context)
        {
            Console.WriteLine("Channel inactive!");
            base.ChannelInactive(context);
        }

        public override void ExceptionCaught(IChannelHandlerContext context, Exception exception)
        {
            Console.WriteLine("Exception!!!!: " + exception);
            //context.CloseAsync();
        }

    }
}
