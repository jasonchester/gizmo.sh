using System;

namespace Gizmo.Connection
{
    public class BulkResult<T> : QueryResultSet<T>
    {
        public int ThreadId { get; }
        public string FileName { get; }
        public long LineNumber { get; }
        public int QueriesProcessed { get; }
        public int QueriesTotal { get; }
        public TimeSpan LoadElapsedTime { get; }
        public TimeSpan BulkElapsedTime { get; }
        public Double PercentComplete => QueriesProcessed * 1.0 / QueriesTotal;
        public TimeSpan RemainingTime => QueriesProcessed == 0 ? TimeSpan.Zero : LoadElapsedTime / QueriesProcessed * (QueriesTotal - QueriesProcessed);
        public Double QueriesPerSecond => QueriesProcessed / LoadElapsedTime.TotalSeconds;

        public BulkResult(QueryResultSet<T> q, int threadId, string fileName, long lineNumber, int queriesProcessed, int queriesTotal, TimeSpan loadElapsedTime, TimeSpan bulkElapsedTime) :
            base(q.Query, q.Data, q.ElapsedTime, q.RequestCharge, q.RetryCount, q.StatusAttributes)
        {
            ThreadId = threadId;
            FileName = fileName;
            LineNumber = lineNumber;
            QueriesProcessed = queriesProcessed;
            QueriesTotal = queriesTotal;
            LoadElapsedTime = loadElapsedTime;
            BulkElapsedTime = bulkElapsedTime;
        }

        public override string ToString()
        {
            return $@"{ThreadId,2}:{PercentComplete,6:P1} qps:{QueriesPerSecond:f2} eta: {RemainingTime:hh\:mm\:ss} query: {ElapsedTime.TotalSeconds:n4}s, {RequestCharge:0000.00} RUs, retries {RetryCount}, {Count} results, {QueriesProcessed}/{QueriesTotal}, {LoadElapsedTime:hh\:mm\:ss} @ {FileName}:{LineNumber}";
        }
    }
}