using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using PrismaSlice.Application.Common;

namespace PrismaSlice.Api.ErrorHandling;

/// <summary>Traduce las excepciones de los casos de uso a respuestas ProblemDetails.</summary>
internal sealed class ApplicationExceptionHandler(IProblemDetailsService problemDetails)
    : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        ProblemDetails? problem = exception switch
        {
            ValidationException ex => new HttpValidationProblemDetails(ex.Errors.ToDictionary())
            {
                Status = StatusCodes.Status400BadRequest,
                Title = ex.Message,
            },
            EmbeddingUnavailableException ex => new ProblemDetails
            {
                Status = StatusCodes.Status503ServiceUnavailable,
                Title = ex.Message,
                Detail = "La búsqueda semántica no está disponible en este momento.",
            },
            _ => null,
        };

        if (problem is null)
            return false;

        httpContext.Response.StatusCode = problem.Status!.Value;
        return await problemDetails.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = problem,
            Exception = exception,
        });
    }
}
