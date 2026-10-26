namespace PrismaSlice.Application.Common;

/// <summary>La petición no cumple las reglas del caso de uso. La API la traduce a 400.</summary>
public sealed class ValidationException(IReadOnlyDictionary<string, string[]> errors)
    : Exception("La petición tiene errores de validación.")
{
    public IReadOnlyDictionary<string, string[]> Errors { get; } = errors;
}
