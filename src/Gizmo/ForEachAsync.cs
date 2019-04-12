using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Gizmo
{
    public static class EnumerableExtensions
    {
        // Adapted from https://blogs.msdn.microsoft.com/pfxteam/2012/03/05/implementing-a-simple-foreachasync-part-2/
        public static Task ForEachAsync<T,R>(this IEnumerable<T> source, int degreeOfParallelism, Func<T,Task<R>> body, IProgress<R> progress = null)
        {
            return Task.WhenAll(
                Partitioner.Create(source).GetPartitions(degreeOfParallelism)
                    .Select(partition => Task.Run(async () => {
                        using (partition)
                            while (partition.MoveNext())
                            {
                                progress?.Report(await body(partition.Current));
                            }
                    }))
            );
        }
    }
}