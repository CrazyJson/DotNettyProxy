using System;
using System.Collections.Generic;

namespace CDotNetty.Common
{
    public class ResponseMessage
    {
        public ResponseMessage()
        {
            Headers = new Dictionary<string, List<string>>(StringComparer.CurrentCultureIgnoreCase);
        }

        public Dictionary<string, List<string>> Headers { get; set; }

        public int StatusCode { get; set; }

        public string ReasonPhrase { get; set; }

        public byte[] Content { get; set; }
    }
}
