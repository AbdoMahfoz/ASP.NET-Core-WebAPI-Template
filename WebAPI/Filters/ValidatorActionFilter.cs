using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Services.DTOs;
using Services.Exceptions;
using ValidationFailure = Services.Exceptions.ValidationFailure;
using ValidationException = Services.Exceptions.ValidationException;

namespace WebAPI.Filters;

/// <summary>
///     Turns model validation output - DataAnnotations attributes and IValidatableObject rules - into a typed
///     <see cref="ValidationException" /> whose failures carry machine-readable codes, so that the exception middleware is the single place deciding how a validation failure looks on the wire. 
///     Requires SuppressModelStateInvalidFilter (set in Program.cs) because [ApiController] otherwise answers with its own ProblemDetails before any action filter runs. 
/// </summary> 
public class ValidatorActionFilter : IActionFilter 
{ 
    public void OnActionExecuting(ActionExecutingContext filterContext) 
    { 
        var failures = new List<ValidationFailure>(); 
        var seen = new HashSet<string>(); 

        // Model state already carries model binding errors plus everything the DataAnnotations pipeline produced for the bound instances, which includes IValidatableObject results. 
        foreach (var entry in filterContext.ModelState) 
            foreach (var error in entry.Value.Errors) 
                Add(failures, seen, ToFailure(entry.Key, error)); 

        // Arguments that never went through the model binding pipeline still get their IValidatableObject rules executed. 
        foreach (var argument in filterContext.ActionArguments.Values) 
        { 
            if (argument is not IValidatableObject) continue; 

            var services = filterContext.HttpContext?.RequestServices; 
            var context = services == null ? new ValidationContext(argument) : new ValidationContext(argument, services, null); 

            var results = new List<ValidationResult>(); 
            Validator.TryValidateObject(argument, context, results, validateAllProperties: true); 

            foreach (var result in results) Add(failures, seen, ToFailure(result.MemberNames.FirstOrDefault() ?? string.Empty, result)); 
        } 

        if (failures.Count == 0) return; 

        throw new ValidationException(Classify(failures), failures); 
    } 

    public void OnActionExecuted(ActionExecutedContext filterContext) 
    { 
    } 

    private static void Add(ICollection<ValidationFailure> failures, ISet<string> seen, ValidationFailure failure) 
    { 
        if (seen.Add(failure.Code + "|" + failure.Field)) failures.Add(failure); 
    } 

    private static ValidationFailure ToFailure(string field, ModelError error) 
    { 
        var message = error.ErrorMessage ?? error.Exception?.Message ?? string.Empty; 

        // Model binding failures surface as an exception rather than as a token-carrying message. 
        var code = error.Exception != null ? nameof(FailureReasons.InvalidParameter) : Code(message); 

        return new ValidationFailure(Reason(message, code), code, field ?? string.Empty, message); 
    } 

    private static ValidationFailure ToFailure(string field, ValidationResult result) 
    { 
        var message = result.ErrorMessage ?? string.Empty; 
        var code = Code(message); 

        return new ValidationFailure(Reason(message, code), code, field ?? string.Empty, message); 
    } 

    /// <summary>
    ///     Codes declared by our own contracts - PaginationError / LookupError / FailureReasons names used as attribute
    ///     messages or ValidationResult messages - are the machine-readable codes clients switch on.
    /// </summary>
    private static string Code(string message)
    {
        var candidate = message?.Trim();

        if (!string.IsNullOrEmpty(candidate) && IsContractToken(candidate)) return candidate; 

        return Enum.TryParse<FailureReasons>(candidate, true, out _) ? candidate : nameof(FailureReasons.InvalidRequest); 
    } 

    private static bool IsContractToken(string candidate)
    {
        return Enum.TryParse<PaginationError>(candidate, true, out _)
               || Enum.TryParse<LookupError>(candidate, true, out _)
               || Enum.TryParse<FailureReasons>(candidate, true, out _);
    }

    private static FailureReasons Reason(string message, string code)
    {
        var candidate = message?.Trim(); 

        if (!string.IsNullOrEmpty(candidate)) 
        { 
            if (Enum.TryParse<FailureReasons>(candidate, true, out var typed)) return typed; 

            if (Enum.TryParse<PaginationError>(candidate, true, out _)) return FailureReasons.InvalidPagination; 

            if (Enum.TryParse<LookupError>(candidate, true, out _)) return FailureReasons.InvalidQuery; 
        } 

        // Codes the filter itself chose classify the failure directly.
        if (Enum.TryParse<FailureReasons>(code, true, out var fromCode)) return fromCode; 

        if (Enum.TryParse<PaginationError>(code, true, out _)) return FailureReasons.InvalidPagination; 

        if (Enum.TryParse<LookupError>(code, true, out _)) return FailureReasons.InvalidQuery; 

        return FailureReasons.InvalidRequest; 
    } 

    private static FailureReasons Classify(IReadOnlyCollection<ValidationFailure> failures) 
    { 
        var only = failures.First().Reason; 
        return failures.All(f => f.Reason == only) ? only : FailureReasons.InvalidRequest; 
    } 
} 
