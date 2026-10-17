using System.Collections.Concurrent;
using System.Reflection;

namespace Meezan.Application.Common.Results;

public static class ResultFactory
{
    private static readonly ConcurrentDictionary<Type, Func<Error, object>> FailureFactories = new();

    public static TResponse CreateFailure<TResponse>(Error error)
    {
        var targetType = typeof(TResponse);

        if (targetType == typeof(Result))
        {
            return (TResponse)(object)Result.Failure(error);
        }

        var factory = FailureFactories.GetOrAdd(targetType, t =>
        {
            if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(Result<>))
            {
                var valueType = t.GetGenericArguments()[0];
                var method = typeof(Result<>)
                    .MakeGenericType(valueType)
                    .GetMethod("Failure", BindingFlags.Public | BindingFlags.Static, [typeof(Error)]);

                if (method != null)
                {
                    return (err) => method.Invoke(null, [err])!;
                }
            }

            throw new InvalidOperationException($"Type {t.FullName} is not a supported Result type.");
        });

        return (TResponse)factory(error);
    }
}
