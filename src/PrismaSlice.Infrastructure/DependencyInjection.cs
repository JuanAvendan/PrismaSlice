using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PrismaSlice.Application.Abstractions;
using PrismaSlice.Infrastructure.Embeddings;
using PrismaSlice.Infrastructure.Persistence;

namespace PrismaSlice.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services, IConfiguration config)
    {
        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(
                config.GetConnectionString("Default"),
                npgsql => npgsql.UseVector()));

        services.AddScoped<ITicketRepository, TicketRepository>();
        services.AddEmbeddings(config);

        return services;
    }
}