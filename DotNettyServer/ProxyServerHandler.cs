// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System;
using System.Collections.Concurrent;
using System.Net;
using CDotNetty.Common;
using DotNetty.Transport.Channels;
using DotNetty.Transport.Channels.Groups;
using Newtonsoft.Json;

namespace DotNettyServer
{
    public class ProxyServerHandler : SimpleChannelInboundHandler<string>
    {
        public static volatile IChannelGroup group;

        public static ConcurrentDictionary<string, ResponseMessage> dictResponse = new ConcurrentDictionary<string, ResponseMessage>();

        public override void ChannelActive(IChannelHandlerContext contex)
        {
            IChannelGroup g = group;
            if (g == null)
            {
                lock (this)
                {
                    if (group == null)
                    {
                        g = group = new DefaultChannelGroup(contex.Executor);
                    }
                }
            }
            //Console.WriteLine(string.Format("Welcome to {0} socket server!\n", Dns.GetHostName()));
            contex.WriteAndFlushAsync(string.Format("Welcome to {0} proxy server!\n", Dns.GetHostName()));
            g.Add(contex.Channel);
        }

        class EveryOneBut : IChannelMatcher
        {
            readonly IChannelId id;

            public EveryOneBut(IChannelId id)
            {
                this.id = id;
            }

            public bool Matches(IChannel channel) => channel.Id != this.id;
        }

        //public override void ChannelRead(IChannelHandlerContext ctx, object msg)
        //{
        //    base.ChannelRead(ctx, msg);
        //}
        protected override void ChannelRead0(IChannelHandlerContext contex, string msg)
        {
            ////send message to all but this one
            //string broadcast = string.Format("[{0}] {1}\n", contex.Channel.RemoteAddress, msg);
            //string response = string.Format("[you] {0}\n", msg);
            //group.WriteAndFlushAsync(broadcast, new EveryOneBut(contex.Channel.Id));
            //contex.WriteAndFlushAsync(response);

            //if (string.Equals("bye", msg, StringComparison.OrdinalIgnoreCase))
            //{
            //    contex.CloseAsync();
            //}
            try
            {
                var response = JsonConvert.DeserializeObject<ResponseMessage>(msg);
                string id = string.Join("", response.Headers["mytraceid"]);
                response.Headers.Remove("mytraceid");
                dictResponse.TryAdd(id, response);
                //Console.WriteLine("response:{0}", id);
                contex.Flush();
                //string broadcast = string.Format("[{0}] {1}\n", contex.Channel.RemoteAddress, msg);
                //group.WriteAndFlushAsync(broadcast, new EveryOneBut(contex.Channel.Id));
            }
            catch { }

        }

        public override void ChannelReadComplete(IChannelHandlerContext ctx) => ctx.Flush();

        public override void ExceptionCaught(IChannelHandlerContext ctx, Exception e)
        {
            Console.WriteLine("{0}", e.StackTrace);
            ctx.CloseAsync();
        }

        public override bool IsSharable => true;
    }
}