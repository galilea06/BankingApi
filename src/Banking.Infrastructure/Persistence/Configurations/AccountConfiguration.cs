using Banking.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Banking.Infrastructure.Persistence.Configurations;

public sealed class AccountConfiguration : IEntityTypeConfiguration<Account>
{
    public void Configure(EntityTypeBuilder<Account> builder)
    {
        builder.ToTable("Accounts");

        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id).ValueGeneratedNever();

        builder.Property(a => a.AccountNumber).IsRequired().HasMaxLength(17);

        // Restricción física: nunca pueden existir dos cuentas con el mismo número.
        builder.HasIndex(a => a.AccountNumber)
            .IsUnique()
            .HasDatabaseName("UX_Accounts_AccountNumber");

        builder.Property(a => a.Balance).HasPrecision(18, 2).IsRequired();

        builder.Property(a => a.Version).IsConcurrencyToken();

        builder.Property(a => a.CreatedAtUtc)
            .HasConversion(v => v, v => DateTime.SpecifyKind(v, DateTimeKind.Utc));

        builder.HasOne<Customer>()
            .WithMany()
            .HasForeignKey(a => a.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(a => a.Transactions)
            .WithOne()
            .HasForeignKey(t => t.AccountId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(a => a.Transactions).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
