using System;
using System.Threading;
using System.Threading.Tasks;

namespace Gizmo
{
    public interface IQueryExecutor : IDisposable
    {
        Task<QueryResultSet<T>> ExecuteQuery<T>(string query, CancellationToken ct = default(CancellationToken));
        Task<bool> TestConnection(CancellationToken ct = default(CancellationToken));
        string RemoteMessage { get; }
    }
}
