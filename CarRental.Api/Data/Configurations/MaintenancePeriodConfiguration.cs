using CarRental.Api.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CarRental.Api.Data.Configurations;

public class MaintenancePeriodConfiguration : IEntityTypeConfiguration<MaintenancePeriod>
{
    public void Configure(EntityTypeBuilder<MaintenancePeriod> builder)
    {
        builder.ToTable("MaintenancePeriods");

        builder.HasKey(period => period.Id);

        builder.Property(period => period.StartDate)
            .HasColumnType("date")
            .IsRequired();

        builder.Property(period => period.EndDate)
            .HasColumnType("date")
            .IsRequired();

        builder.Property(period => period.Description)
            .HasMaxLength(500)
            .IsRequired();

        builder.HasOne(period => period.Car)
            .WithMany()
            .HasForeignKey(period => period.CarId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
