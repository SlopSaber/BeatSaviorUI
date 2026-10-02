using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BeatSaviorUI.Models;

namespace BeatSaviorUI.Stats;

internal static class PlayDataPreparation
{
    private static readonly object Gate = new();
    private static readonly Queue<WorkItem> Queue = new();
    private static Task? worker;

    internal static long Generation { get; private set; }
    internal static Task<Result>? Pending { get; private set; }

    internal static void BeginPlay()
    {
        ++Generation;
        Pending = null;
        PluginConfig.LastKnownPlayData = null;
    }

    internal static void Submit(PlayData.PreparationRequest request)
    {
        var item = new WorkItem(request);
        Pending = item.Completion.Task;
        lock (Gate)
        {
            Queue.Enqueue(item);
            StartWorker();
        }
    }

    internal static bool IsCurrent(Task<Result> task, long generation) =>
        Generation == generation && ReferenceEquals(Pending, task);

    internal static PlayData? Publish(Task<Result> task, long generation)
    {
        if (!task.IsCompleted || !IsCurrent(task, generation))
            return null;
        Result result = task.GetAwaiter().GetResult();
        Pending = null;
        if (result.Error != null)
            Plugin.Log.Error(result.Error);
        PluginConfig.LastKnownPlayData = result.Data;
        return result.Data;
    }

    private static void StartWorker()
    {
        if (worker != null || Queue.Count == 0)
            return;
        worker = Task.Factory.StartNew(ProcessQueue, CancellationToken.None,
            TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);
        worker.ContinueWith(WorkerFinished, CancellationToken.None,
            TaskContinuationOptions.None, TaskScheduler.Default);
    }

    private static void ProcessQueue()
    {
        while (true)
        {
            WorkItem item;
            lock (Gate)
            {
                if (Queue.Count == 0)
                    return;
                item = Queue.Dequeue();
            }
            Result result;
            try
            {
                result = new Result(new PlayData(item.Request), null);
            }
            catch (Exception error)
            {
                result = new Result(null, error);
            }
            item.Completion.TrySetResult(result);
        }
    }

    private static void WorkerFinished(Task completed)
    {
        lock (Gate)
        {
            if (ReferenceEquals(worker, completed))
            {
                worker = null;
                StartWorker();
            }
        }
    }

    private sealed class WorkItem
    {
        internal readonly PlayData.PreparationRequest Request;
        internal readonly TaskCompletionSource<Result> Completion =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        internal WorkItem(PlayData.PreparationRequest request) => Request = request;
    }

    internal sealed class Result
    {
        internal readonly PlayData? Data;
        internal readonly Exception? Error;

        internal Result(PlayData? data, Exception? error)
        {
            Data = data;
            Error = error;
        }
    }
}
