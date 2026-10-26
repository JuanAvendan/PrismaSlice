using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PrismaSlice.Domain.Entities;

namespace PrismaSlice.Infrastructure.Persistence.Configurations;

public class TicketConfiguration : IEntityTypeConfiguration<Ticket>
{
    public void Configure(EntityTypeBuilder<Ticket> builder)
    {
        builder.ToTable("Tickets");
        builder.HasKey(t => t.Id);

        // Código legible generado por la base a partir de una secuencia: TCK-00001.
        // El sentinel "" indica a EF que no lo envíe en el INSERT y lo lea de vuelta.
        builder.Property(t => t.Code)
            .HasMaxLength(20)
            .IsRequired()
            .HasDefaultValueSql(
                $"'TCK-' || lpad(nextval('\"{AppDbContext.TicketNumberSequence}\"')::text, 5, '0')")
            .HasSentinel(string.Empty);
        builder.HasIndex(t => t.Code).IsUnique();

        builder.Property(t => t.Title).HasMaxLength(200).IsRequired();
        builder.Property(t => t.Description).IsRequired();
        builder.Property(t => t.Category).HasMaxLength(80).IsRequired();

        builder.Property(t => t.Status).HasConversion<string>().HasMaxLength(20);
        builder.Property(t => t.Priority).HasConversion<string>().HasMaxLength(20);

        // pgvector: 768 dims (EmbeddingGemma y OpenAI truncado).
        builder.Property(t => t.Embedding).HasColumnType("vector(768)");

        // Índice HNSW con distancia coseno para búsqueda semántica.
        builder.HasIndex(t => t.Embedding)
            .HasMethod("hnsw")
            .HasOperators("vector_cosine_ops");

        // Dos FKs a User: requester (obligatorio) y assignee (opcional).
        builder.HasOne(t => t.Requester)
            .WithMany()
            .HasForeignKey(t => t.RequesterId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(t => t.Assignee)
            .WithMany()
            .HasForeignKey(t => t.AssigneeId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(t => t.Team)
            .WithMany()
            .HasForeignKey(t => t.TeamId)
            .OnDelete(DeleteBehavior.SetNull);

        // Many-to-many con Label vía skip navigations (tabla de join implícita).
        builder.HasMany(t => t.Labels)
            .WithMany(l => l.Tickets)
            .UsingEntity(j => j.ToTable("TicketLabels"));

        builder.HasIndex(t => t.Status);
        builder.HasIndex(t => t.CreatedAt);
    }
}