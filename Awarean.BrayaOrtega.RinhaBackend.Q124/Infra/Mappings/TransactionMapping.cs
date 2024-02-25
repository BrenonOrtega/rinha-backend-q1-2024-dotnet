using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Awarean.BrayaOrtega.RinhaBackend.Q124.Infra.Mappings;

public sealed class TransactionMapping : IEntityTypeConfiguration<Transaction>
{
    public void Configure(EntityTypeBuilder<Transaction> builder)
    {
        builder.ToTable("transactions");
        builder.HasNoKey();
        builder.Property(x => x.AccountId).HasColumnName("accountid");
        builder.Property(x => x.Descricao).HasColumnName("descricao").HasColumnType("varchar(10)");
        builder.Property(x => x.Tipo).HasColumnName("tipo").HasColumnType("varchar(1)").IsRequired();
        builder.Property(x => x.Valor).HasColumnName("valor").IsRequired();
        builder.Property(x => x.Limite).HasColumnName("limite");
        builder.Property(x => x.Saldo).HasColumnName("saldo");
        builder.Property(x => x.RealizadaEm).HasColumnName("realizadaem").HasDefaultValueSql("now()");
    }
}