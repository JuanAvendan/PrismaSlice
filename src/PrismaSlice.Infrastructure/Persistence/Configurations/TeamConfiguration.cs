using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PrismaSlice.Domain.Entities;

namespace PrismaSlice.Infrastructure.Persistence.Configurations;

public class TeamConfiguration : IEntityTypeConfiguration<Team>
{
    public void Configure(EntityTypeBuilder<Team> builder)
    {
        builder.ToTable("Teams");
        builder.HasKey(t => t.Id);

        builder.Property(t => t.Name).HasMaxLength(120).IsRequired();
        builder.Property(t => t.Code).HasMaxLength(20).IsRequired();
        builder.HasIndex(t => t.Code).IsUnique();

        builder.HasMany(t => t.Members)
            .WithOne(u => u.Team)
            .HasForeignKey(u => u.TeamId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}