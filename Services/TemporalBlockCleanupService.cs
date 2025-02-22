namespace BlockedConuntries.Services
{
    public class TemporalBlockCleanupService : BackgroundService
    {
        private readonly InMemoryStorageService _storage;

        public TemporalBlockCleanupService(InMemoryStorageService storage)
        {
            _storage = storage;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                _storage.RemoveExpiredTemporalBlocks();
                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
            }
        }
    }
}
