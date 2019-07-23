using System;
using System.Net.WebSockets;
using System.Threading.Tasks;
using Gremlin.Net.Driver.Exceptions;
using Polly;

namespace Gizmo.Connection
{

    public static class GizmoPolicies
    {
        public static AsyncPolicy CosmosRetryAfterWait(RetryOption _options) => Policy
            .Handle<ResponseException>(ex => ex.CosmosDbStatusCode() == 429)
            .Or<WebSocketException>()
            .WaitAndRetryAsync(
                _options.RetryCount,
                sleepDurationProvider: (attempt, exception, context) => 
                {
                    switch(exception) 
                    {
                        case ResponseException rex :
                            return rex.CosmosDbRetryAfter()
                                .Add(TimeSpan.FromSeconds(Math.Pow(_options.WaitTime, attempt - 1)));

                        default:
                            return TimeSpan.FromSeconds(Math.Pow(_options.WaitTime, attempt));
                    }
                },
                onRetryAsync: (exception, waitTime, attempt, context) => {
                    context["retryCount"] = attempt;
                    return Task.CompletedTask;
                });
                
    }

    public class RetryOption
    {
        public int WaitTime { get; set; } = 30;
        public int RetryCount { get; set; } = 5;
    }

    public static class ResponseExceptionExtensions
    {
        public static int CosmosDbStatusCode(this ResponseException source)
        {
            if (!source.StatusAttributes.TryGetValue("x-ms-status-code", out var code))
            {
                throw new InvalidOperationException("Header 'x-ms-status-code' is not presented.");
            }

            return int.Parse(code.ToString());
        }

        public static TimeSpan CosmosDbRetryAfter(this ResponseException source)
        {
            if (!source.StatusAttributes.TryGetValue("x-ms-retry-after-ms", out var time))
            {
                throw new InvalidOperationException("Header 'x-ms-retry-after-ms' is not presented.");
            }

            return TimeSpan.Parse(time.ToString());
        }
    }

}