using System.Threading.Channels;
using AIQuizHSG.Application.Omr;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AIQuizHSG.Infrastructure.Omr;

public sealed class OmrJobQueue : IOmrJobQueue
{
    private readonly Channel<long> channel = Channel.CreateUnbounded<long>();

    public ChannelReader<long> Reader => channel.Reader;

    public void Enqueue(long sheetId) => channel.Writer.TryWrite(sheetId);
}

// Xử lý từng phiếu sau khi API đã nhận file, không giữ request HTTP đến lúc đọc xong ảnh.
public sealed class OmrProcessingWorker(OmrJobQueue queue, IServiceScopeFactory scopes, ILogger<OmrProcessingWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var sheetId in queue.Reader.ReadAllAsync(stoppingToken))
        {
            try
            {
                await using var scope = scopes.CreateAsyncScope();
                var service = scope.ServiceProvider.GetRequiredService<IOmrService>();
                await service.ProcessSheetAsync(sheetId, stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Không xử lý được phiếu {SheetId}.", sheetId);
            }
        }
    }
}
