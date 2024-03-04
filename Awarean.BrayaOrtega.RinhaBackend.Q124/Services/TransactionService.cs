using System.Threading.Channels;
using Awarean.BrayaOrtega.RinhaBackend.Q124.Infra;
using Awarean.BrayaOrtega.RinhaBackend.Q124.Models;
using NATS.Client.Core;

namespace Awarean.BrayaOrtega.RinhaBackend.Q124.Services;

public sealed class TransactionService : ITransactionService
{
    private readonly ICachedRepository cachedRepository;
    private readonly string natsDestinationQueue;
    private readonly INatsConnection connection;
    private readonly Channel<int> channel;

    public TransactionService(
        ICachedRepository cachedRepository,
        [FromKeyedServices("NatsDestination")] string natsDestinationQueue,
        INatsConnection connection,
        Channel<int> channel)
    {
        this.cachedRepository = cachedRepository ?? throw new ArgumentNullException(nameof(cachedRepository));
        this.natsDestinationQueue = natsDestinationQueue ?? throw new ArgumentNullException(nameof(natsDestinationQueue));
        this.connection = connection ?? throw new ArgumentNullException(nameof(connection));
        this.channel = channel ?? throw new ArgumentNullException(nameof(channel));
    }

    public async Task<ExecuteTransactionResponse> TryExecuteTransactionAsync(
        int accountId, TransactionRequest transaction, CancellationToken token)
    {
        var account = await cachedRepository.GetAccountByIdAsync(accountId).ConfigureAwait(false);

        if (account is null || account.IsEmpty())
            return new ExecuteTransactionResponse(false, -1, -1, TransactionExecutionError.AccountNotFound);

        if (account.CanExecute(transaction) is false)
            return new ExecuteTransactionResponse(false, -1, -1, TransactionExecutionError.ExceedLimitError);

        var createdTransaction = account.Execute(transaction);

        await cachedRepository.Save(createdTransaction).ConfigureAwait(false);

        await connection.PublishAsync<Transaction>(
           natsDestinationQueue,
           createdTransaction,
           cancellationToken: token)
           .ConfigureAwait(false);

        channel.Writer.TryWrite(default);

        return new ExecuteTransactionResponse(true, createdTransaction.Limite, createdTransaction.Saldo);
    }

    public async Task<BankStatement> GetBankStatementAsync(int id)
    {
        return await cachedRepository.GetBankStatementAsync(id).ConfigureAwait(false);
    }
}