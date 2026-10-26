using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace PrismaSlice.Api.Hosting;

/// <summary>
/// Límites por IP para la demo pública: protegen la cuota gratuita (cada búsqueda
/// corre el modelo) y evitan que se llene la base de tickets basura.
/// </summary>
public static class RateLimiting
{
    public const string SearchPolicy = "search";
    public const string WritePolicy = "write";

    public static IServiceCollection AddApiRateLimiting(
        this IServiceCollection services, IConfiguration config)
    {
        var searchPerMinute = config.GetValue("RateLimiting:SearchPerMinute", 30);
        var writesPerMinute = config.GetValue("RateLimiting:WritesPerMinute", 5);

        services.AddRateLimiter(options =>
        {
            options.AddPolicy(SearchPolicy, http => PerClientPerMinute(http, searchPerMinute));
            options.AddPolicy(WritePolicy, http => PerClientPerMinute(http, writesPerMinute));

            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.OnRejected = async (context, ct) =>
            {
                if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
                    context.HttpContext.Response.Headers.RetryAfter =
                        ((int)retryAfter.TotalSeconds).ToString();

                var problems = context.HttpContext.RequestServices
                    .GetRequiredService<IProblemDetailsService>();
                await problems.WriteAsync(new ProblemDetailsContext
                {
                    HttpContext = context.HttpContext,
                    ProblemDetails = new ProblemDetails
                    {
                        Status = StatusCodes.Status429TooManyRequests,
                        Title = "Demasiadas peticiones.",
                        Detail = "Espera un momento y vuelve a intentarlo.",
                    },
                });
            };
        });

        return services;
    }

    private static RateLimitPartition<string> PerClientPerMinute(HttpContext http, int permits) =>
        RateLimitPartition.GetFixedWindowLimiter(ClientKey(http), _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = permits,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0,
        });

    // Detrás del ingress de Azure (y del rewrite de Vercel) la IP real llega en
    // X-Forwarded-For. Un cliente puede falsear la cabecera: es una protección de
    // demo, no un control de seguridad.
    private static string ClientKey(HttpContext http)
    {
        var forwarded = http.Request.Headers["X-Forwarded-For"].ToString();
        if (!string.IsNullOrWhiteSpace(forwarded))
            return forwarded.Split(',')[0].Trim();

        return http.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }
}
