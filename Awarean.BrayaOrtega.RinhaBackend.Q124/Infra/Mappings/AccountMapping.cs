// using Awarean.BrayaOrtega.RinhaBackend.Q124.Models;
// using Microsoft.EntityFrameworkCore;
// using Microsoft.EntityFrameworkCore.Metadata.Builders;

// namespace Awarean.BrayaOrtega.RinhaBackend.Q124.Infra.Mappings;

// public sealed class AccountMapping : IEntityTypeConfiguration<Account>
// {
//     public void Configure(EntityTypeBuilder<Account> builder)
//     {
//         builder.HasKey(x => x.Id);

//         builder.Property(x => x.Id).UseIdentityColumn();

//         builder.Property(x => x.Saldo).IsRequired();

//         builder.Property(x => x.Limite).IsRequired();

//         builder.OwnsMany(x => x.Transactions,
//             c =>
//             {
//                 c.Property(x => x.AccountId);
//                 c.Property(x => x.Descricao).HasColumnType("varchar(10)");
//                 c.Property(x => x.Tipo).HasColumnType("varchar(1)").IsRequired();
//                 c.Property(x => x.Valor).IsRequired();
//                 c.Property(x => x.RealizadaEm).HasDefaultValueSql("now()");
//                 c.WithOwner().HasForeignKey(x => x.AccountId);
//             });
//     }
// }
