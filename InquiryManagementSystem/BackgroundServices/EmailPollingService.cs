using InquiryManagementSystem.Services;

namespace InquiryManagementSystem.BackgroundServices
{
    public class EmailPollingService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<EmailPollingService> _logger;

        public EmailPollingService(
            IServiceScopeFactory scopeFactory,
            ILogger<EmailPollingService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Email polling service started.");

            await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();

                    var emailReader = scope.ServiceProvider
                        .GetRequiredService<IEmailReaderService>();

                    var newCount = await emailReader.CheckEmailsAsync();

                    if (newCount > 0)
                    {
                        _logger.LogInformation(
                            "Email check complete. {Count} new inquiry(ies) created.", newCount);
                    }
                    else
                    {
                        _logger.LogInformation("Email check complete. No new emails.");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Email check failed: {Message}", ex.Message);
                }

                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
        }
    }
}