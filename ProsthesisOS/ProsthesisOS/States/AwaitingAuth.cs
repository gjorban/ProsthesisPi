using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using ProsthesisCore.Messages;

namespace ProsthesisOS.States
{
    internal class AwaitingAuth : ProsthesisStateBase
    {
        public override ProsthesisStateBase OnEnter(ProsthesisContext context)
        {
            return this;
        }

        public override void OnExit(ProsthesisContext context)
        {
            
        }

        public override ProsthesisStateBase OnClientAuthorization(ProsthesisContext context, TCP.ConnectionState authedClient, bool isAuthorized)
        {
            ProsthesisHandshakeResponse response = new ProsthesisHandshakeResponse();
            response.AuthorizedConnection = isAuthorized;
            if (isAuthorized)
            {
                byte[] data = ProsthesisCore.Messages.ProsthesisDataPacket.BoxMessage<ProsthesisHandshakeResponse>(response).Bytes;
                authedClient.Write(data, 0, data.Length);
                return new OperationalSuperState();
            }
            else
            {
                response.ErrorString = "OS Version mismatch";
                byte[] data = ProsthesisCore.Messages.ProsthesisDataPacket.BoxMessage<ProsthesisHandshakeResponse>(response).Bytes;
                authedClient.Write(data, 0, data.Length);
                //authedClient._server.DropConnection(authedClient);
                return this;
            }
        }
    }
}
