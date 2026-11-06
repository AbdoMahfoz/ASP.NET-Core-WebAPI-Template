namespace Services.Exceptions;

/// <summary>
///     One machine-readable validation failure.
///     <see cref="Code" /> is the stable token clients switch on ("Required", "Range", "InvalidPerPage", ...),
///     <see cref="Field" /> is the offending member of the request, <see cref="Message" /> is human-readable detail.
/// </summary>
public sealed record ValidationFailure(FailureReasons Reason, string Code, string Field = "", string Message = "")
{
    public override string ToString()
    {
        return Field.Length == 0 ? $"{Reason}/{Code}" : $"{Reason}/{Code} ({Field})";
    }
}
