using System;

namespace Services.Exceptions;

/// <summary>
///     The caller is authenticated but not permitted to perform this operation. Carries a machine-readable
///     <see cref="FailureReasons" /> so the role guard can distinguish "not allowed at all" from "role lacks this permission". 
/// </summary> 
public class ForbiddenAccessException(FailureReasons reason = FailureReasons.AccessDenied) : Exception(reason.ToString()) 
{ 
    public ForbiddenAccessException(FailureReasons reason, string detail) : this(reason) 
    { 
        Detail = detail ?? string.Empty; 
    } 

    /// <summary>Classification clients switch on ("AccessDenied", "InsufficientRole", ...).</summary> 
    public FailureReasons Reason { get; } = reason; 

    /// <summary>Optional human-readable context. Never used to decide the status code.</summary> 
    public string Detail { get; } = string.Empty; 
} 
