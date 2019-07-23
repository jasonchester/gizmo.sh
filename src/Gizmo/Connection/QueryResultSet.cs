using System;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Gizmo.Connection
{
    public class QueryResultSet<T> : IReadOnlyCollection<T>, IOperationResult
    {
        public IReadOnlyCollection<T> Data { get; }
        public string Query { get; }
        public TimeSpan ElapsedTime { get; }
        public int RetryCount {get; }

        public double RequestCharge { get; }

        /// <summary>
        /// Gets or sets the status attributes from the gremlin response
        /// </summary>
        public IReadOnlyDictionary<string, object> StatusAttributes { get; }

        /// <summary>
        /// Initializes a new instance of the ResultSet class for the specified data and status attributes.
        /// </summary>
        /// <param name="data"></param>
        /// <param name="attributes"></param>
        public QueryResultSet(string query, IReadOnlyCollection<T> data, TimeSpan elapsedTime, double requestCharge, int retryCount, IReadOnlyDictionary<string, object> attributes)
        {
            Query = query;
            Data = data;
            ElapsedTime = elapsedTime;
            RequestCharge = requestCharge;
            RetryCount = retryCount;
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

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public string Message => this.ToString();

        public string Details => this.ResultsToJson(Formatting.Indented);

        /// <inheritdoc />
        public override string ToString()
        {
            return $"[{ElapsedTime}, RUs {RequestCharge}, retries {RetryCount}, {Count} results] {Query}";
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="formatting"></param>
        /// <returns></returns>
        public string ResultsToJson(Formatting formatting = Formatting.None) => JsonConvert.SerializeObject(Data, formatting);
    }
}