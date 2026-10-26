using System.Text.Json.Serialization;
using PrismaSlice.Api.Endpoints;
using PrismaSlice.Api.ErrorHandling;
using PrismaSlice.Api.Hosting;
using PrismaSlice.Application;
using PrismaSlice.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ApplicationExceptionHandler>();
builder.Services.AddApiRateLimiting(builder.Configuration);
builder.Services.AddHealthChecks();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// CORS solo si se configuran orígenes (p. ej. la página web corriendo en local).
// En producción la página llama a la API por un rewrite de Vercel: mismo origen.
var corsOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
if (corsOrigins.Length > 0)
    builder.Services.AddCors(options => options.AddDefaultPolicy(policy =>
        policy.WithOrigins(corsOrigins).AllowAnyHeader().AllowAnyMethod()));

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();

if (corsOrigins.Length > 0)
    app.UseCors();

app.UseRateLimiter();

if (app.Environment.IsDevelopment() || app.Configuration.GetValue<bool>("Swagger:Enabled"))
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Sin UseHttpsRedirection: en contenedor el TLS lo termina el proxy/ingress.
// /health no toca la base: así las sondas no despiertan a Neon cuando está en reposo.
app.MapHealthChecks("/health");
app.MapTicketEndpoints();
app.Run();
