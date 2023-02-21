namespace Perception.Services
{
    public class NeuralNetworkService : BackgroundService
    {
        private readonly SemaphoreSlim semaphore=new SemaphoreSlim(4,4);
        private TaskQueue Tasks { get; }
        public NeuralNetworkService(TaskQueue tasks)
        {
            Tasks = tasks;
        }


        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            stoppingToken.ThrowIfCancellationRequested();
            while (!stoppingToken.IsCancellationRequested)
            {
                await semaphore.WaitAsync(stoppingToken);
                _ = Task.Run(async () => 
                {
                    Console.WriteLine(semaphore.CurrentCount);
                    var t = await Tasks.DequeueAsync();
                    await t;
                    semaphore.Release();
                    Console.WriteLine("释放信号量");
                });
            }
            Console.WriteLine("退出死循环");
        }


        public override async Task StopAsync(CancellationToken stoppingToken)
        {
            Console.WriteLine("Queued Hosted Service is stopping.");
            await base.StopAsync(stoppingToken);
        }
    }
}
