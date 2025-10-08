namespace NoAutoCheckpointsSVC
{
    public class Worker(NacSVC service, IConfiguration configuration) : BackgroundService
    {
        private readonly NacSVC _service = service;
        private readonly IConfiguration _configuration = configuration;

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            int delayMs = _configuration.GetValue<int>("WorkerSettings:DelayMilliseconds");
            if (delayMs == 0)
            {
                delayMs = 1000;
            }

            _service.Run();

            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(delayMs, stoppingToken);
            }
            if (stoppingToken.IsCancellationRequested)
            {
                _service.Stop();
            }
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            _service.Stop();
            await base.StopAsync(cancellationToken);
        }

    }
}