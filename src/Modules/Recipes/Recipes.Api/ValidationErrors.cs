namespace Recipes.Api;

/// <summary>
/// Collects the domain's argument errors under request field names, for a <c>ValidationProblem</c> response.
/// The domain validates and throws <see cref="ArgumentException"/>; endpoints don't repeat its rules, they translate them.
/// </summary>
internal sealed class ValidationErrors
{
    private readonly Dictionary<string, List<string>> _errors = [];

    public bool IsEmpty => _errors.Count == 0;

    public void Add(string field, string message)
    {
        if (!_errors.TryGetValue(field, out var messages))
        {
            _errors[field] = messages = [];
        }

        messages.Add(message);
    }

    /// <summary>Runs a domain operation; an argument error is recorded under the field <paramref name="fieldFor"/> picks for it.</summary>
    public T? Try<T>(Func<T> operation, Func<string?, string> fieldFor)
        where T : class
    {
        try
        {
            return operation();
        }
        catch (ArgumentException exception)
        {
            Add(fieldFor(exception.ParamName), MessageOf(exception));
            return null;
        }
    }

    public Dictionary<string, string[]> ToDictionary() => _errors.ToDictionary(e => e.Key, e => e.Value.ToArray());

    // ArgumentException appends " (Parameter 'x')" and, when out of range, the actual value: noise for an API client.
    private static string MessageOf(ArgumentException exception)
    {
        var message = exception.Message;
        var parameterSuffix = message.IndexOf(" (Parameter '", StringComparison.Ordinal);
        return parameterSuffix >= 0 ? message[..parameterSuffix] : message;
    }
}
