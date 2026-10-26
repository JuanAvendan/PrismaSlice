namespace PrismaSlice.Application.Common;

/// <summary>Acumula errores por campo antes de lanzar una sola <see cref="ValidationException"/>.</summary>
internal sealed class ValidationErrors
{
    private readonly Dictionary<string, List<string>> _errors = [];

    public void Add(string field, string message)
    {
        if (!_errors.TryGetValue(field, out var list))
            _errors[field] = list = [];
        list.Add(message);
    }

    public void ThrowIfAny()
    {
        if (_errors.Count > 0)
            throw new ValidationException(_errors.ToDictionary(e => e.Key, e => e.Value.ToArray()));
    }
}
