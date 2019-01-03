using System;
using System.Collections.Generic;

namespace CDotNetty.Common
{
    public class RequestMessage
    {
        public RequestMessage()
        {
            Headers = new Dictionary<string, List<string>>(StringComparer.CurrentCultureIgnoreCase);
        }

        public Dictionary<string, List<string>> Headers { get; set; }

        public string Method { get; set; }

        public string Uri { get; set; }

        public byte[] Content { get; set; }
    }
}
