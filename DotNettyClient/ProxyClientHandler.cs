// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System;
using System.IO;
using System.Net;
using System.Net.Http;
using DotNetty.Transport.Channels;
using Newtonsoft.Json;
using System.Linq;
using CDotNetty.Common;
using System.Threading.Tasks;
using System.Diagnostics;

namespace DotNettyClient
{
    public class ProxyClientHandler : SimpleChannelInboundHandler<string>
    {
        private HttpClient client;

        private string baseUri;
        public ProxyClientHandler()
        {
            client = new HttpClient(new HttpClientHandler
            {
#if NETSTANDARD2_0
                MaxConnectionsPerServer = 200,
#endif
                UseDefaultCredentials = false,
                AllowAutoRedirect = false,
                UseCookies = false,
                Proxy = null,
                UseProxy = false,
                AutomaticDecompression = DecompressionMethods.GZip
            });
            baseUri = ConfigHelper.GetValue<string>("uri");
            Console.WriteLine("HTTP Requests------------ - ");
        }

        protected override void ChannelRead0(IChannelHandlerContext contex, string msg)
        {
            Task.Factory.StartNew(async () =>
            {
                try
                {
                    Stopwatch sw = new Stopwatch();
                    sw.Start();
                    var request = JsonConvert.DeserializeObject<RequestMessage>(msg);
                    using (var requestMessage = new HttpRequestMessage())
                    {
                        var requestMethod = request.Method;
                        using (Stream stream = new MemoryStream(request.Content))
                        {
                            var streamContent = new StreamContent(stream);
                            requestMessage.Content = streamContent;
                            foreach (var header in request.Headers)
                            {
                                if (!requestMessage.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray()) && requestMessage.Content != null)
                                {
                                    requestMessage.Content?.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray());
                                }
                            }
                            requestMessage.RequestUri = new Uri(baseUri + request.Uri);
                            requestMessage.Headers.Host = requestMessage.RequestUri.Authority;
                            requestMessage.Method = new HttpMethod(request.Method);

                            using (var responseMessage = await client.SendAsync(requestMessage))
                            {
                                var response = new ResponseMessage
                                {
                                    StatusCode = (int)responseMessage.StatusCode,
                                    ReasonPhrase = responseMessage.ReasonPhrase
                                };
                                foreach (var header in responseMessage.Headers)
                                {
                                    response.Headers[header.Key] = header.Value.ToList();
                                }

                                foreach (var header in responseMessage.Content.Headers)
                                {
                                    response.Headers[header.Key] = header.Value.ToList();
                                }
                                response.Headers["mytraceid"] = request.Headers["mytraceid"];
                                //response.Headers.Remove("transfer-encoding");
                                response.Content = await responseMessage.Content.ReadAsByteArrayAsync();
                                string rm = JsonConvert.SerializeObject(response);
                                Program.bootstrapChannel.WriteAndFlushAsync(rm + "\r\n");
                                Console.WriteLine("{0} {1} {2} {3},共计耗时：{4}ms", request.Method.ToUpper(), request.Uri, response.StatusCode, response.ReasonPhrase, sw.ElapsedMilliseconds);
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
            });
        }

        public override void ExceptionCaught(IChannelHandlerContext contex, Exception e)
        {
            Console.WriteLine(DateTime.Now.Millisecond);
            Console.WriteLine(e.StackTrace);
            contex.CloseAsync();
        }
    }
}