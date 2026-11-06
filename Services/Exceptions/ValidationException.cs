using System;
using System.Collections.Generic;

namespace Services.Exceptions;

/// <summary>
///     The request failed validation. Carries one machine-readable <see cref="Reason" /> plus the individual field
///     failures, so middleware can emit a stable contract instead of an exception message.
/// </summary>
public class ValidationException : Exception
{
    public ValidationException(FailureReasons reason) : this(reason, new ValidationFailure[0])
    {
    }

    public ValidationException(FailureReasons reason, IEnumerable<ValidationFailure> failures) : base(reason.ToString())
    {
        Reason = reason;
        Failures = failures == null ? new List<ValidationFailure>() : new List<ValidationFailure>(failures);
    }

    /// <summary>The classification of this failure ("InvalidRequest", "InvalidPagination", "PasswordTooShort", ...).</summary> 
    public FailureReasons Reason { get; }

    /// <summary>Field-level failures with machine-readable codes.</summary> 
    public IReadOnlyList<ValidationFailure> Failures { get; }

    public override string Message => Failures.Count == 0 ? base.Message : $"{base.Message}: {string.Join(", ", Failures)}"; 

} 
