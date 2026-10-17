using System.Diagnostics.CodeAnalysis;

namespace Meezan.Application.Common.Results;

public class Result<TValue> : Result
{
    private readonly TValue? _value;

    [NotNull]
    public TValue Value
    {
        get
        {
            if (IsFailure)
            {
                throw new InvalidOperationException("Value cannot be accessed when Result is a failure.");
            }

            return _value!;
        }
    }

    public TValue? ValueOrDefault => _value;

    protected internal Result(TValue? value, bool isSuccess, Error error)
        : base(isSuccess, error)
    {
        _value = value;
    }

    public static Result<TValue> Success(TValue value) => new(value, true, Error.None);

    public new static Result<TValue> Failure(Error error) => new(default, false, error);

    public static implicit operator Result<TValue>(TValue value) => Success(value);

    public static implicit operator Result<TValue>(Error error) => Failure(error);
}
