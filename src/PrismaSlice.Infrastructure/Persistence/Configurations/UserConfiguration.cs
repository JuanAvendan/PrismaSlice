using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PrismaSlice.Domain.Entities;

namespace PrismaSlice.Infrastructure.Persistence.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users");
        builder.HasKey(u => u.Id);

        builder.Property(u => u.Name).HasMaxLength(120).IsRequired();
        builder.Property(u => u.Email).HasMaxLength(160).IsRequired();
        builder.HasIndex(u => u.Email).IsUnique();

        builder.Property(u => u.Role).HasConversion<string>().HasMaxLength(20);
    }
}