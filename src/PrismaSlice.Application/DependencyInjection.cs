using Microsoft.Extensions.DependencyInjection;
using PrismaSlice.Application.Tickets;

namespace PrismaSlice.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddSingleton(TimeProvider.System);
        services.AddScoped<TicketService>();
        return services;
    }
}
