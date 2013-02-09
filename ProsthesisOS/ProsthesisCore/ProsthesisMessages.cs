using System;
using System.Collections.Generic;
using ProtoBuf;

namespace ProsthesisCore.Messages
{
    [ProtoInclude(1,typeof(ProsthesisHandshakeRequest))]
    [ProtoContract]
    public class ProsthesisMessage
    {

    }

    [ProtoContract]
    public class ProsthesisHandshakeRequest : ProsthesisMessage
    {
        [ProtoMember(1)]
        public string VersionId;
    }
}