// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using DotNetty.Codecs.Http;
using DotNetty.Common.Utilities;
using DotNetty.Transport.Channels;
using System;
using Newtonsoft.Json;
using System.Collections.Generic;
using CDotNetty.Common;
using System.Threading.Tasks;
using DotNetty.Buffers;
using System.Diagnostics;

namespace DotNettyServer
{
    sealed class HttpServerHandler : ChannelHandlerAdapter
    {
        public override void ChannelRead(IChannelHandlerContext ctx, object message)
        {
            Task.Factory.StartNew(() => {
                if (message is IFullHttpRequest request)
                {
                    try
                    {
                        this.Process(ctx, request);
                    }
                    finally
                    {
                        ReferenceCountUtil.Release(message);
                    }
                }
                else
                {
                    ctx.FireChannelRead(message);
                }
            });           
        }

        void Process(IChannelHandlerContext ctx, IFullHttpRequest request)
        {
            string uri = request.Uri;
            int i = 0;
            byte[] destination = new byte[request.Content.Capacity];
            while (i < request.Content.Capacity)
            {
                destination[i] = request.Content.ReadByte();
                i++;
            }
            var requestMessage = new RequestMessage();
            requestMessage.Method = request.Method.ToString();
            requestMessage.Uri = uri;
            foreach (var header in request.Headers)
            {
                string key = header.Key.ToString().ToLower();
                if (!requestMessage.Headers.TryGetValue(key, out List<string> values))
                {
                    values = new List<string>();
                }
                values.Add(header.Value.ToString());
                requestMessage.Headers[key] = values;
            }
            string mytraceid = Guid.NewGuid().ToString("N").ToLower();
            requestMessage.Headers["mytraceid"] = new List<string>() { mytraceid };
            requestMessage.Content = destination;
            ProxyServerHandler.group.WriteAndFlushAsync(JsonConvert.SerializeObject(requestMessage) + "\r\n");
            ProxyServerHandler.group.Flush();
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            ResponseMessage responseMessage = null;
            while (!ProxyServerHandler.dictResponse.TryRemove(mytraceid, out responseMessage) && stopwatch.ElapsedMilliseconds < 100 * 1000)
            {
                System.Threading.Thread.Sleep(5);
            }
            if (stopwatch.ElapsedMilliseconds < 100 * 1000)
            {
                IByteBuffer buf = Unpooled.WrappedBuffer(responseMessage.Content);
                var statue = new HttpResponseStatus(responseMessage.StatusCode, new AsciiString(responseMessage.ReasonPhrase));
                var response = new DefaultFullHttpResponse(HttpVersion.Http11
                    , statue, buf, false);
                HttpHeaders headers = response.Headers;
                foreach (var header in responseMessage.Headers)
                {
                    headers.Set(new AsciiString(header.Key), header.Value.ToArray());
                }
                ctx.WriteAndFlushAsync(response);
                ctx.CloseAsync();
            }
            else
            {
                var response = new DefaultFullHttpResponse(HttpVersion.Http11, HttpResponseStatus.NotFound, Unpooled.Empty, false);
                ctx.WriteAndFlushAsync(response);
                ctx.CloseAsync();
            }
        }

        public override void ExceptionCaught(IChannelHandlerContext context, Exception exception) => context.CloseAsync();

        public override void ChannelReadComplete(IChannelHandlerContext context) => context.Flush();
    }
}
