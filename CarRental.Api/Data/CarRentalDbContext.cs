using CarRental.Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace CarRental.Api.Data;

public class CarRentalDbContext : DbContext
{
    public CarRentalDbContext(DbContextOptions<CarRentalDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();

    public DbSet<UserRoleAssignment> UserRoleAssignments => Set<UserRoleAssignment>();

    public DbSet<Car> Cars => Set<Car>();

    public DbSet<RentalRequest> RentalRequests => Set<RentalRequest>();

    public DbSet<RentalAgreement> RentalAgreements => Set<RentalAgreement>();

    public DbSet<MaintenancePeriod> MaintenancePeriods => Set<MaintenancePeriod>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(CarRentalDbContext).Assembly);
    }
}
