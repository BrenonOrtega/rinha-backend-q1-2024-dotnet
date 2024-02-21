
using System.Threading.Channels;
using Awarean.BrayaOrtega.RinhaBackend.Q124.Infra;
using Awarean.BrayaOrtega.RinhaBackend.Q124.Models;

namespace Awarean.BrayaOrtega.RinhaBackend.Q124;

public class UpdateRequestBackgroundService : BackgroundService
{
    private readonly Repository repo;
    private readonly Channel<UpdateRequest> channel;

    public UpdateRequestBackgroundService(Repository repo, Channel<UpdateRequest> channel)
    {
        this.repo = repo ?? throw new ArgumentNullException(nameof(repo));
        this.channel = channel ?? throw new ArgumentNullException(nameof(channel));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            if (channel.Reader.TryRead(out var updateRequest))
            {
                if (updateRequest.Token.IsCancellationRequested)
                    continue;

                try 
                {
                    await repo.Save(updateRequest.Account, updateRequest.CreatedTransaction);
                } 
                catch (Exception ex)
                {
                    await channel.Writer.WriteAsync(updateRequest, updateRequest.Token);
                    Console.WriteLine(ex.Message);
                }
            }

            await Task.Delay(5, stoppingToken);
        }
    }
}