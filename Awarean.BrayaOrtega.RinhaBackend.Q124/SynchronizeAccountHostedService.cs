
using System.Collections.Concurrent;
using Awarean.BrayaOrtega.RinhaBackend.Q124.Models;
using NATS.Client.Core;

namespace Awarean.BrayaOrtega.RinhaBackend.Q124;

public sealed class SynchronizeAccountsHostedService : BackgroundService
{
    private readonly ConcurrentDictionary<int, Account> cache;
    private readonly string ownChannel;
    private INatsConnection connection;

    public SynchronizeAccountsHostedService(
        ConcurrentDictionary<int, Account> cache,
        INatsConnection connection,
        [FromKeyedServices("NatsOwnChannel")] string ownChannel)
    {
        if (string.IsNullOrEmpty(ownChannel))
        {
            throw new ArgumentException($"'{nameof(ownChannel)}' cannot be null or empty.", nameof(ownChannel));
        }

        this.cache = cache ?? throw new ArgumentNullException(nameof(cache));
        this.connection = connection ?? throw new ArgumentNullException(nameof(connection));
        this.ownChannel = ownChannel;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await connection.ConnectAsync();

        var thread = StartInThread(stoppingToken);
        while (true)
        {
            if (stoppingToken.IsCancellationRequested)
                thread.Join();

            await Task.Delay(1000, stoppingToken);
        }
    }

    public Thread StartInThread(CancellationToken token)
    {
        var thread = new Thread(async () =>
        {
            var messages = connection.SubscribeAsync<Account>(ownChannel, cancellationToken: token);
            await foreach (var msg in messages)
            {
                var account = msg.Data;
                cache.AddOrUpdate(account.Id, account, (id, acc) => account.Saldo != acc.Saldo ? account : acc);
            }
        })
        {
            IsBackground = true
        };

        thread.Start();

        return thread;
    }
}