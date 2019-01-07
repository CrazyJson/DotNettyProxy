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
                            requestMessage.Method = new HttpMethod(request.Method);

                            using (var responseMessage = await client.SendAsync(requestMessage, HttpCompletionOption.ResponseHeadersRead))
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
                                await Program.bootstrapChannel.WriteAndFlushAsync(rm + "\r\n");
                                //Program.bootstrapChannel.Flush();
                                //contex.Flush();
                                Console.WriteLine("{0} {1} {2} {3}", request.Method.ToUpper(), request.Uri, response.StatusCode, response.ReasonPhrase);
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