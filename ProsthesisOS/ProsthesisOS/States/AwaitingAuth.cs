using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using ProsthesisCore.Messages;
using ProsthesisOS.States.Base;

namespace ProsthesisOS.States
{
    internal class AwaitingAuth : ProsthesisStateBase
    {
        public AwaitingAuth(IProsthesisContext context) : base(context) { }

        public override ProsthesisStateBase OnEnter()
        {
            return this;
        }

        public override void OnExit()
        {
            
        }

        public override ProsthesisStateBase OnClientAuthorization(TCP.ConnectionState authedClient, bool isAuthorized)
        {
            ProsthesisHandshakeResponse response = new ProsthesisHandshakeResponse();
            response.AuthorizedConnection = isAuthorized;
            if (isAuthorized)
            {
                byte[] data = ProsthesisCore.Messages.ProsthesisDataPacket.BoxMessage<ProsthesisHandshakeResponse>(response).Bytes;
                authedClient.Write(data, 0, data.Length);
                return new OperationalSuperState(mContext);
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
