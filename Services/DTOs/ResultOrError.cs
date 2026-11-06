#nullable enable

using System.Diagnostics.CodeAnalysis;

namespace Services.DTOs;

/// <summary>
///     A logic-layer return type that is either a value or a domain error, without throwing for expected failures.
///     Both directions convert implicitly, so a method declared as ResultOrError&lt;T, TE&gt; can return T or TE directly.     
/// </summary>     
public class ResultOrError<T, TE>     
{     
    private ResultOrError(T value)     
    {         
        Value = value;         
        IsSuccess = true;     
    }     

    private ResultOrError(TE error)     
    {         
        Error = error;         
        IsSuccess = false;     
    }     

    /// <summary>The produced value when the operation succeeded.</summary>     
    public T? Value { get; }     

    /// <summary>The domain error when the operation failed.</summary>     
    public TE? Error { get; }     

    [MemberNotNullWhen(true, nameof(Value))]     
    [MemberNotNullWhen(false, nameof(Error))]     
    public bool IsSuccess { get; }     

    public static implicit operator ResultOrError<T, TE>(T value) => new(value);     

    public static implicit operator ResultOrError<T, TE>(TE error) => new(error);     

    /// <summary>Explicit factories for call sites that cannot use the implicit conversion (e.g. generic callers).</summary>     
    public static ResultOrError<T, TE> Success(T value) => new(value);     

    public static ResultOrError<T, TE> Failure(TE error) => new(error);     
} 
