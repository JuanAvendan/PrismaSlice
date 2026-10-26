using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PrismaSlice.Domain.Entities;

namespace PrismaSlice.Infrastructure.Persistence.Configurations;

public class DepartmentConfiguration : IEntityTypeConfiguration<Department>
{
    public void Configure(EntityTypeBuilder<Department> builder)
    {
        builder.ToTable("Departments");
        builder.HasKey(d => d.Id);

        builder.Property(d => d.Name).HasMaxLength(120).IsRequired();
        builder.Property(d => d.Code).HasMaxLength(20).IsRequired();
        builder.HasIndex(d => d.Code).IsUnique();

        builder.HasMany(d => d.Teams)
            .WithOne(t => t.Department)
            .HasForeignKey(t => t.DepartmentId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}