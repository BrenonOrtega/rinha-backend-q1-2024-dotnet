namespace Awarean.BrayaOrtega.RinhaBackend.Q124.Models;

public sealed record UpdateRequest(
    Account Account, 
    Transaction CreatedTransaction,
    CancellationToken Token);
