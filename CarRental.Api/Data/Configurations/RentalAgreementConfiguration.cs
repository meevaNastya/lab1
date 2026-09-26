using CarRental.Api.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CarRental.Api.Data.Configurations;

public class RentalAgreementConfiguration : IEntityTypeConfiguration<RentalAgreement>
{
    public void Configure(EntityTypeBuilder<RentalAgreement> builder)
    {
        builder.ToTable("RentalAgreements");

        builder.HasKey(agreement => agreement.Id);

        builder.HasIndex(agreement => agreement.RentalRequestId)
            .IsUnique();

        builder.Property(agreement => agreement.StartDate)
            .HasColumnType("date")
            .IsRequired();

        builder.Property(agreement => agreement.EndDate)
            .HasColumnType("date")
            .IsRequired();

        builder.Property(agreement => agreement.ActualReturnDate)
            .HasColumnType("date");

        builder.Property(agreement => agreement.PricePerDay)
            .HasPrecision(10, 2)
            .IsRequired();

        builder.Property(agreement => agreement.RentalCost)
            .HasPrecision(10, 2)
            .IsRequired();

        builder.Property(agreement => agreement.Penalty)
            .HasPrecision(10, 2)
            .IsRequired();

        builder.Property(agreement => agreement.TotalCost)
            .HasPrecision(10, 2)
            .IsRequired();

        builder.HasOne(agreement => agreement.RentalRequest)
            .WithOne()
            .HasForeignKey<RentalAgreement>(agreement => agreement.RentalRequestId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(agreement => agreement.Client)
            .WithMany()
            .HasForeignKey(agreement => agreement.ClientId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(agreement => agreement.Car)
            .WithMany()
            .HasForeignKey(agreement => agreement.CarId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
