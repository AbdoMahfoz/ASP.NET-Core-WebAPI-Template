namespace Services.Exceptions;

/// <summary>
///     Machine-readable failure reasons used by the validation contract.
///     Kept domain-neutral on purpose - concrete applications extend it with their own reasons.
/// </summary>
public enum FailureReasons
{
    /// <summary>Catch-all for a request that failed model validation.</summary>
    InvalidRequest,

    /// <summary>A single parameter/argument was rejected.</summary>
    InvalidParameter,

    /// <summary>Paging arguments were outside their allowed range (see Services.DTOs.PaginationError).</summary>
    InvalidPagination,

    /// <summary>The supplied query text was rejected.</summary>
    InvalidQuery,

    /// <summary>Credentials were missing or malformed.</summary>
    InvalidCredentials,

    /// <summary>Username was absent or blank.</summary>
    UsernameRequired,

    /// <summary>Password was absent or blank.</summary>
    PasswordRequired,

    /// <summary>Password did not meet the minimum length.</summary>
    PasswordTooShort,

    /// <summary>A from/to date pair was inverted or otherwise invalid.</summary>
    InvalidDateRange,

    /// <summary>A date must not be in the future.</summary>
    DateShouldNotBeInTheFuture,

    /// <summary>A date must be in the future.</summary>
    DateShouldBeInTheFuture,

    /// <summary>An operation that changes data was sent without a modification reason.</summary>
    MissingModificationReasons,

    /// <summary>The addressed resource does not exist in the caller's scope.</summary>
    NotFound,

    /// <summary>The caller is not allowed to perform this operation.</summary>
    AccessDenied,

    /// <summary>The caller's role does not grant the permission this operation requires.</summary>
    InsufficientRole,

    /// <summary>Too many failed attempts for this account within the lockout window.</summary>
    AccountLockedOut,

    /// <summary>The session/token backing this request is no longer valid.</summary>
    InvalidSession
}
