using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace TCC.Lib.AsyncStreams
{
    public static partial class AsyncStreamExtensions
    {
        public static AsyncStream<T> AsAsyncStream<T>(this IEnumerable<T> source, CancellationToken cancellationToken)
        {
            var channel = Channel.CreateUnbounded<StreamedValue<T>>(new UnboundedChannelOptions
            {
                SingleWriter = true,
                SingleReader = false
            });
            var task = Task.Run(async () =>
            {
                try
                {
                    foreach (var item in source)
                    {
                        await channel.Writer.WriteAsync(new StreamedValue<T>(item, ExecutionStatus.Succeeded), cancellationToken);
                    }
                }
                finally
                {
                    channel.Writer.Complete();
                }
            });
            return new AsyncStream<T>(channel, task, cancellationToken);
        }

        public static AsyncStream<T> AsAsyncStream<T>(this IAsyncEnumerable<T> source, CancellationToken cancellationToken)
        {
            var channel = Channel.CreateUnbounded<StreamedValue<T>>(new UnboundedChannelOptions
            {
                SingleWriter = true,
                SingleReader = false
            });
            var task = Task.Run(async () =>
            {
                try
                {
                    await foreach (var item in source)
                    {
                        await channel.Writer.WriteAsync(new StreamedValue<T>(item, ExecutionStatus.Succeeded), cancellationToken);
                    }
                }
                finally
                {
                    channel.Writer.Complete();
                }
            });
            return new AsyncStream<T>(channel, task, cancellationToken);
        }

        public static AsyncStream<T> CountAsync<T>(this AsyncStream<T> source, out Counter counter)
        {
            var localCounter = new Counter();
            counter = localCounter;
            var channel = Channel.CreateUnbounded<StreamedValue<T>>(new UnboundedChannelOptions
            {
                SingleWriter = false,
                SingleReader = false
            });
            var task = Task.Run(async () =>
            {
                try
                {
                    await foreach (var item in source.ChannelReader.ReadAllAsync(source.CancellationToken))
                    {
                        localCounter.Increment();
                        await channel.Writer.WriteAsync(item, source.CancellationToken);
                    }
                }
                finally
                {
                    channel.Writer.Complete();
                }
            });
            return new AsyncStream<T>(channel, task, source.CancellationToken);
        }

        public static AsyncStream<T> ForEachAsync<T>(this AsyncStream<T> source, Func<StreamedValue<T>, CancellationToken, Task> action)
        {
            var channel = Channel.CreateUnbounded<StreamedValue<T>>(new UnboundedChannelOptions
            {
                SingleWriter = false,
                SingleReader = false
            });
            var task = Task.Run(async () =>
            {
                try
                {
                    await foreach (var item in source.ChannelReader.ReadAllAsync(source.CancellationToken))
                    {
                        await action(item, source.CancellationToken);
                        await channel.Writer.WriteAsync(item, source.CancellationToken);
                    }
                }
                finally
                {
                    channel.Writer.Complete();
                }
            });
            return new AsyncStream<T>(channel, task, source.CancellationToken);
        }

        public static async Task<IReadOnlyCollection<T>> AsReadOnlyCollectionAsync<T>(this AsyncStream<T> source)
        {
            var items = new ConcurrentBag<T>();
            await foreach (var item in source.ChannelReader.ReadAllAsync(source.CancellationToken))
            {
                items.Add(item.Item);
            }
            return items;
        }

        public static AsyncStream<TResult> SelectAsync<TSource, TResult>(this AsyncStream<TSource> source, Func<StreamedValue<TSource>, CancellationToken, Task<TResult>> action)
        {
            var channel = Channel.CreateUnbounded<StreamedValue<TResult>>();
            var writer = channel.Writer;
            var task = Task.Run(async () =>
            {
                await foreach (var sourceValue in source.ChannelReader.ReadAllAsync(source.CancellationToken))
                {
                    await sourceValue.ExecuteAndStreamAsync(action, writer, source.CancellationToken);
                }
                channel.Writer.Complete();
            });
            return new AsyncStream<TResult>(channel, task, source.CancellationToken);
        }

        private static async Task ExecuteAndStreamAsync<TSource, TResult>(this StreamedValue<TSource> sourceValue,
            Func<StreamedValue<TSource>, CancellationToken, Task<TResult>> action,
            ChannelWriter<StreamedValue<TResult>> writer,
            CancellationToken cancellationToken)
        {
            TResult result;
            try
            {
                result = await action(sourceValue, cancellationToken);
            }
            catch (TaskCanceledException tce)
            {
                await writer.WriteAsync(new StreamedValue<TResult>(default, ExecutionStatus.Canceled, tce), cancellationToken);
                return;
            }
            catch (Exception e)
            {
                await writer.WriteAsync(new StreamedValue<TResult>(default, ExecutionStatus.Faulted, e), cancellationToken);
                return;
            }
            await writer.WriteAsync(new StreamedValue<TResult>(result, ExecutionStatus.Succeeded), cancellationToken);
        }
    }

    public class Counter
    {
        private int _count;
        public int Count => _count;

        public void Increment()
        {
            Interlocked.Increment(ref _count);
        }
    }
}