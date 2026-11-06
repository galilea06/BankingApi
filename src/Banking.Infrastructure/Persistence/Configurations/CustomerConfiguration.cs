using Banking.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Banking.Infrastructure.Persistence.Configurations;

public sealed class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> builder)
    {
        builder.ToTable("Customers");

        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).ValueGeneratedNever();

        builder.Property(c => c.FullName).IsRequired().HasMaxLength(Customer.FullNameMaxLength);
        builder.Property(c => c.DateOfBirth).IsRequired();
        builder.Property(c => c.Gender).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(c => c.MonthlyIncome).HasPrecision(18, 2).IsRequired();

        builder.Property(c => c.CreatedAtUtc)
            .HasConversion(v => v, v => DateTime.SpecifyKind(v, DateTimeKind.Utc));
    }
}
