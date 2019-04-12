using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Newtonsoft.Json;

namespace Gizmo.Connection
{
    public class QueryResultSet<T> : IReadOnlyCollection<T>, IOperationResult
    {
        public IReadOnlyCollection<T> Data { get; }
        public string Query { get; }
        public TimeSpan ElapsedTime { get; }

        public double RequestCharge { get; }

        /// <summary>
        ///     Gets or sets the status attributes from the gremlin response
        /// </summary>
        public IReadOnlyDictionary<string, object> StatusAttributes { get; }

        /// <summary>
        ///     Initializes a new instance of the ResultSet class for the specified data and status attributes.
        /// </summary>
        /// <param name="data"></param>
        /// <param name="attributes"></param>
        public QueryResultSet(string query, IReadOnlyCollection<T> data, TimeSpan elapsedTime, double requestCharge, IReadOnlyDictionary<string, object> attributes)
        {
            Query = query;
            Data = data;
            ElapsedTime = elapsedTime;
            RequestCharge = requestCharge;
            this.StatusAttributes = attributes;
        }

        /// <inheritdoc />
        public IEnumerator<T> GetEnumerator()
        {
            return Data.GetEnumerator();
        }

        /// <inheritdoc />
        IEnumerator IEnumerable.GetEnumerator()
        {
            return Data.GetEnumerator();
        }

        /// <inheritdoc />
        public int Count => Data.Count;

        public string Message => this.ToString();

        public string Details => this.ResultsToJson(Formatting.Indented);

        /// <inheritdoc />
        public override string ToString()
        {
            return $"[{ElapsedTime}, RUs {RequestCharge}, {Count} results] {Query}";
        }

        public string ResultsToJson(Formatting formatting = Formatting.None) => JsonConvert.SerializeObject(Data, formatting);
    }

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
            base(q.Query, q.Data, q.ElapsedTime, q.RequestCharge, q.StatusAttributes)
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
            return $@"{ThreadId,2}:{PercentComplete,6:P1} qps:{QueriesPerSecond:f2} eta: {RemainingTime:hh\:mm\:ss} query: {ElapsedTime.TotalSeconds:n4}s, {RequestCharge:f2} RUs, {Count} results, {QueriesProcessed}/{QueriesTotal}, {LoadElapsedTime.TotalSeconds:n4} @ {FileName}:{LineNumber}";
        }
    }
}