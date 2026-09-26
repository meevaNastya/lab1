using CarRental.Api.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CarRental.Api.Data.Configurations;

public class RentalRequestConfiguration : IEntityTypeConfiguration<RentalRequest>
{
    public void Configure(EntityTypeBuilder<RentalRequest> builder)
    {
        builder.ToTable("RentalRequests");

        builder.HasKey(request => request.Id);

        builder.Property(request => request.StartDate)
            .HasColumnType("date")
            .IsRequired();

        builder.Property(request => request.EndDate)
            .HasColumnType("date")
            .IsRequired();

        builder.Property(request => request.Status)
            .HasConversion<int>()
            .IsRequired();

        builder.HasOne(request => request.Client)
            .WithMany()
            .HasForeignKey(request => request.ClientId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(request => request.Car)
            .WithMany()
            .HasForeignKey(request => request.CarId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
