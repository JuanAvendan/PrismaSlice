using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PrismaSlice.Domain.Entities;

namespace PrismaSlice.Infrastructure.Persistence.Configurations;

public class LabelConfiguration : IEntityTypeConfiguration<Label>
{
    public void Configure(EntityTypeBuilder<Label> builder)
    {
        builder.ToTable("Labels");
        builder.HasKey(l => l.Id);

        builder.Property(l => l.Name).HasMaxLength(60).IsRequired();
        builder.Property(l => l.Color).HasMaxLength(9).IsRequired();
        builder.HasIndex(l => l.Name).IsUnique();
    }
}