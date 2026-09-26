using CarRental.Api.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CarRental.Api.Data.Configurations;

public class CarConfiguration : IEntityTypeConfiguration<Car>
{
    public void Configure(EntityTypeBuilder<Car> builder)
    {
        builder.ToTable("Cars");

        builder.HasKey(car => car.Id);

        builder.Property(car => car.Brand)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(car => car.Model)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(car => car.Vin)
            .HasMaxLength(17)
            .IsFixedLength()
            .IsRequired();

        builder.HasIndex(car => car.Vin)
            .IsUnique();

        builder.Property(car => car.Category)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(car => car.PricePerDay)
            .HasPrecision(10, 2)
            .IsRequired();
    }
}
