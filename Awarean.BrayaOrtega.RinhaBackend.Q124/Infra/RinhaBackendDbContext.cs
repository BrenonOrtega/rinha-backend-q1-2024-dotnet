using Awarean.BrayaOrtega.RinhaBackend.Q124.Infra.Mappings;
using Microsoft.EntityFrameworkCore;

namespace Awarean.BrayaOrtega.RinhaBackend.Q124.Infra;

public class RinhaBackendDbContext : DbContext
{
    public RinhaBackendDbContext(DbContextOptions options) : base(options)
    {
    }

    protected RinhaBackendDbContext()
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
         modelBuilder.ApplyConfiguration(new TransactionMapping());
         base.OnModelCreating(modelBuilder);
    }
}