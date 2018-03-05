using System;
using System.Threading;
using System.Threading.Tasks;

namespace Brandmuscle.LocationData.Graph.GremlinConsole
{
    public interface IQueryExecutor : IDisposable
    {
        Task<string> ExecuteQuery(string query, CancellationToken ct = default(CancellationToken));
        Task<bool> TestConnection(CancellationToken ct = default(CancellationToken));
        string RemoteMessage { get; }
    }
}
