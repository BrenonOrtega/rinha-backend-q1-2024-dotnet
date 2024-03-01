using System.Threading.Channels;
using Awarean.BrayaOrtega.RinhaBackend.Q124.Infra;
using Awarean.BrayaOrtega.RinhaBackend.Q124.Services;
using Microsoft.AspNetCore.Mvc;
using NATS.Client.Core;

namespace Awarean.BrayaOrtega.RinhaBackend.Q124;

public static class Endpoints
{
    private static readonly IResult NotFoundResponse = Results.NotFound();
    private static readonly IResult UnprocessableEntityResponse = Results.UnprocessableEntity();

    public static async Task<IResult> MakeTransactionAsync(
            int id,
            [FromBody] TransactionRequest request,
            [FromServices] ITransactionService service,
            CancellationToken token)
    {
        if (request.IsInvalid())
            return UnprocessableEntityResponse;

        var response = await service.TryExecuteTransactionAsync(id, request);

        if (!response.IsSuccess)
        {
            return response.Error switch {
                ExecuteTransactionError.AccountNotFound => NotFoundResponse,
                ExecuteTransactionError.ExceedLimitError => UnprocessableEntityResponse,
                null => throw new InvalidOperationException(); 
            };
        }

        return Results.Ok(new TransactionResponse(response.Limite, response.Saldo));
    }

    public static async Task<IResult> GetBankStatementAsync(int id, [FromServices] ITransactionService service)
    {
        var bankStatement = await service.GetBankStatementAsync(id);

        if (bankStatement is not null && !bankStatement.IsEmpty())
            return Results.Ok(bankStatement);

        return NotFoundResponse;
    }
}