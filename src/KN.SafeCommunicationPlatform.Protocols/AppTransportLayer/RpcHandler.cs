using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KN.SafeCommunicationPlatform.Protocols.AppTransportLayer
{
    public class RpcHandler
    {
        public Task<string> SendHttpToService(string serviceName, string path, Dictionary<string,string> header, string bodyJson)
        {
            return Task.FromResult("");
        }

        
    }
}
