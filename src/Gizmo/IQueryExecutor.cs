using System;
using System.Threading;
using System.Threading.Tasks;

namespace Gizmo
{
    public interface IQueryExecutor : IDisposable
    {
        Task<QueryResultSet<T>> ExecuteQuery<T>(string query, CancellationToken ct = default);
        Task<bool> TestConnection(CancellationToken ct = default);
        string RemoteMessage { get; }
    }
}
