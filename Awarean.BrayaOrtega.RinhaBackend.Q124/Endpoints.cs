using System.Collections.Concurrent;
using System.Threading.Channels;
using Awarean.BrayaOrtega.RinhaBackend.Q124.Infra;
using Microsoft.AspNetCore.Mvc;
using NATS.Client.Core;
using NATS.Client.Serializers.Json;

namespace Awarean.BrayaOrtega.RinhaBackend.Q124;

public static class Endpoints
{
    private static readonly IResult NotFoundResponse = Results.NotFound();
    private static readonly IResult UnprocessableEntityResponse = Results.UnprocessableEntity();
    private static readonly IResult EmptyOkResponse = Results.Ok();

    public static async Task<IResult> MakeTransactionAsync(
            int id,
            [FromBody] TransactionRequest request,
            [FromServices] IRepository repo,
            [FromKeyedServices("NatsDestination")] string natsDestinationQueue,
            [FromServices] INatsConnection connection,
            Channel<int> channel,
            CancellationToken token)
    {
        if (request.IsInvalid())
            return UnprocessableEntityResponse;

        var account = await repo.GetAccountByIdAsync(id);

        if (account is null || account.IsEmpty())
            return NotFoundResponse;

        if (account.CanExecute(request) is false)
            return UnprocessableEntityResponse;

        var createdTransaction = account.Execute(request);

        await repo.Save(createdTransaction);

        await connection.PublishAsync<Transaction>(
            natsDestinationQueue,
            createdTransaction,
            cancellationToken: token);

        channel.Writer.TryWrite(default);

        return Results.Ok(new TransactionResponse(account.Limite, account.Saldo));
    }

    public static async Task<IResult> GetBankStatementAsync(int id, [FromServices] IDecoratedRepository repo)
    {
        var bankStatement = await repo.GetBankStatementAsync(id);

        if (bankStatement is not null && !bankStatement.IsEmpty())
            return Results.Ok(bankStatement);

        return NotFoundResponse;
    }
}