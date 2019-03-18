using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Newtonsoft.Json;

namespace Gizmo
{
    public class QueryResultSet<T> : IReadOnlyCollection<T>, IOperationResult
    {
        private readonly IReadOnlyCollection<T> _data;
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
            _data = data;
            ElapsedTime = elapsedTime;
            RequestCharge = requestCharge;
            this.StatusAttributes = attributes;
        }

        /// <inheritdoc />
        public IEnumerator<T> GetEnumerator()
        {
            return _data.GetEnumerator();
        }

        /// <inheritdoc />
        IEnumerator IEnumerable.GetEnumerator()
        {
            return _data.GetEnumerator();
        }

        /// <inheritdoc />
        public int Count => _data.Count;

        public string Message => this.ToString();

        public string Details => this.ResultsToJson(Formatting.Indented);

        /// <inheritdoc />
        public override string ToString()
        {
            return $"{Query} executed in {ElapsedTime}, RUs {RequestCharge}, {Count} results.";
        }

        public string ResultsToJson(Formatting formatting = Formatting.None) => JsonConvert.SerializeObject(_data, formatting);
    }
}