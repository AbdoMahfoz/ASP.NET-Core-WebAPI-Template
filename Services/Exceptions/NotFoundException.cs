using System;

namespace Services.Exceptions;

/// <summary>
///     The addressed resource does not exist. Carries a machine-readable <see cref="FailureReasons" /> so middleware can emit a stable contract instead of an exception message. 
/// </summary> 
public class NotFoundException(FailureReasons reason = FailureReasons.NotFound) : Exception(reason.ToString()) 
{ 
    public NotFoundException(FailureReasons reason, string detail) : this(reason) 
    { 
        Detail = detail ?? string.Empty; 
    } 

    /// <summary>Classification clients switch on.</summary> 
    public FailureReasons Reason { get; } = reason; 

    /// <summary>Optional human-readable context. Never used to decide the status code.</summary> 
    public string Detail { get; } = string.Empty; 
} 
