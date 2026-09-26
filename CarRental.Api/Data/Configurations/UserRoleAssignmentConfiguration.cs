using CarRental.Api.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CarRental.Api.Data.Configurations;

public class UserRoleAssignmentConfiguration : IEntityTypeConfiguration<UserRoleAssignment>
{
    public void Configure(EntityTypeBuilder<UserRoleAssignment> builder)
    {
        builder.ToTable("UserRoles");

        builder.HasKey(assignment => new
        {
            assignment.UserId,
            assignment.Role,
        });

        builder.Property(assignment => assignment.Role)
            .HasConversion<int>()
            .IsRequired();

        builder.HasOne(assignment => assignment.User)
            .WithMany(user => user.RoleAssignments)
            .HasForeignKey(assignment => assignment.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
