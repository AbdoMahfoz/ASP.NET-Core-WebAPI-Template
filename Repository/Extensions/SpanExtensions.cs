using System;

using System.Globalization;

using System.Runtime.CompilerServices;


using OpenTelemetry.Trace;


namespace Repository.Extensions;


/// <summary>
///     Span attribute helpers. A DateTime is written as a UTC instant in the invariant culture so a trace is readable the same way everywhere - a culture-sensitive ToString would render differently on machines with different regional settings and would sort wrong in a tracer UI. A nullable timestamp is simply skipped rather than recorded as an empty attribute, so absence stays absence instead of looking like a bogus value.  
/// </summary>
public static class SpanExtensions

{

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TelemetrySpan SetAttribute(this TelemetrySpan span, string key, DateTimeOffset value) =>
        span.SetAttribute(key, value.UtcDateTime.ToString(CultureInfo.InvariantCulture));


    /// <summary>
    ///     A DateTime carries no offset of its own: when its Kind is Utc it is written unchanged, when it is Local or Unspecified ToUniversalTime interprets it against the machine's own zone - which is what a trace recorded on that machine means, but is not something a timestamp with an explicit instant should rely on. Use the DateTimeOffset overload when the instant is known.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TelemetrySpan SetAttribute(this TelemetrySpan span, string key, DateTime value) =>
        span.SetAttribute(key, value.ToUniversalTime().ToString(CultureInfo.InvariantCulture));


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TelemetrySpan SetAttribute(this TelemetrySpan span, string key, DateTime? value)

    {

        if (value.HasValue) span.SetAttribute(key, value.Value.ToString(CultureInfo.InvariantCulture));


        return span;

    }


    /// <summary>Builds the template's span naming shape: Layer<Model>::Method - for example Repository&lt;User&gt;::Insert - so a trace reads as which layer touched which model.</summary>  
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string SpanName(string layer, Type model, string method) => $"{layer}<{model.Name}>::{method}";

}
