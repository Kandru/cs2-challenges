using CounterStrikeSharp.API.Core;
using System.Collections.Concurrent;

namespace Challenges
{
    public partial class Challenges : BasePlugin
    {
        private readonly ConcurrentQueue<Func<Task>> _challengeQueue = new();
        private readonly SemaphoreSlim _queueSemaphore = new(1, 1);

        private async Task ProcessQueueAsync()
        {
            while (true)
            {
                await _queueSemaphore.WaitAsync();
                try
                {
                    if (_challengeQueue.TryDequeue(out var challengeTask))
                    {
                        await challengeTask();
                    }
                }
                finally
                {
                    _queueSemaphore.Release();
                }
                // Add a small delay to prevent tight loop
                await Task.Delay(100);
            }
        }

        public void EnqueueChallengeTask(Func<Task> challengeTask)
        {
            _challengeQueue.Enqueue(challengeTask);
        }

    }
}
