using System;
using System.Collections.Generic;

namespace AtMycelia.Hyphlow.EditorExt
{
    public interface IConnectionGatherer : IDisposable
    {
        IList<ConnectionInfo> GatherConnections(DrawBlockContext drawCtx);
    }
}