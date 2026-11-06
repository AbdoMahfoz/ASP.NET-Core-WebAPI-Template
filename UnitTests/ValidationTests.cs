using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Routing;
using Services.DTOs;
using Services.Exceptions;
using ValidationException = Services.Exceptions.ValidationException;
using WebAPI.Filters;
using Xunit;

namespace UnitTests;

/// <summary>
///     Validation contract tests: rules live in DataAnnotations + IValidatableObject, every failure is a machine-readable
///     token, and ValidatorActionFilter converts them into a typed ValidationException. The HTTP-level proof (400 plus a
///     reason body through the test factory) arrives with the WebApplicationFactory harness task. 
/// </summary> 
public class ValidationTests 
{ 
    private static List<ValidationResult> ResultsOf(object instance) 
    { 
        var results = new List<ValidationResult>(); 
        Validator.TryValidateObject(instance, new ValidationContext(instance), results, validateAllProperties: true); 
        return results; 
    } 

    private static string[] Codes(IEnumerable<ValidationResult> results) => results.Select(r => r.ErrorMessage).ToArray(); 

    [Fact] 
    public void LoginRequestRulesAreDataAnnotationsAndReportMachineReadableCodes() 
    { 
        var empty = new UserAuthenticationRequest(); 

        Assert.Contains(nameof(FailureReasons.UsernameRequired), Codes(ResultsOf(empty))); 
        Assert.Contains(nameof(FailureReasons.PasswordRequired), Codes(ResultsOf(empty))); 

        Assert.Contains(nameof(FailureReasons.PasswordTooShort), Codes(ResultsOf(new UserAuthenticationRequest { Username = "user", Password = "short" }))); 

        Assert.Empty(ResultsOf(new UserAuthenticationRequest { Username = "user", Password = "longenough" })); 
    } 

    [Fact] 
    public void PagedRequestRejectsPageIndexBelowOneAndPerPageOutsideItsBounds() 
    { 
        Assert.Contains(nameof(PaginationError.PageIndexMustBeAtLeastOne), Codes(ResultsOf(new PagedRequestBase { PageIndex = 0 }))); 
        Assert.Contains(nameof(PaginationError.PageIndexMustBeAtLeastOne), Codes(ResultsOf(new PagedRequestBase { PageIndex = -3 }))); 
        Assert.Contains(nameof(PaginationError.PerPageMustBeAtLeastOne), Codes(ResultsOf(new PagedRequestBase { PerPage = 0 }))); 
        Assert.Contains(nameof(PaginationError.PerPageExceedsMaximum), Codes(ResultsOf(new PagedRequestBase { PerPage = 501 }))); 

        Assert.Empty(ResultsOf(new PagedRequestBase { PageIndex = 1, PerPage = 500 })); 
    } 

    [Fact] 
    public void PagedRequestRejectsPerPageLargerThanTheCollectionItPagesOver() 
    { 
        Assert.Contains(nameof(PaginationError.PerPageExceedsTotalItems), Codes(ResultsOf(new TenItemRequest { PageIndex = 1, PerPage = 50 }))); 

        Assert.Empty(ResultsOf(new TenItemRequest { PageIndex = 1, PerPage = 10 })); 
    } 

    [Fact] 
    public void PagedRequestRejectsOrderingColumnsTheEndpointDoesNotSupport() 
    { 
        Assert.Contains(nameof(PaginationError.InvalidOrderBy), Codes(ResultsOf(new NameOrderedRequest { OrderBy = "Password" }))); 

        Assert.Empty(ResultsOf(new NameOrderedRequest { OrderBy = "Name" })); 
    } 

    [Fact] 
    public void LookupRequestBoundsItsQueryText() 
    { 
        Assert.Contains(nameof(LookupError.QueryTooShort), Codes(ResultsOf(new LookupRequestBase { Query = "a" }))); 
        Assert.Contains(nameof(LookupError.QueryTooLong), Codes(ResultsOf(new LookupRequestBase { Query = new string('q', 101) }))); 

        Assert.Empty(ResultsOf(new LookupRequestBase())); 
        Assert.Empty(ResultsOf(new LookupRequestBase { Query = "abc" })); 
    } 

    [Fact] 
    public void FilterTurnsModelStateFailuresIntoTypedValidationException() 
    { 
        var filterContext = Context(out var modelState); 
        modelState.AddModelError("PageIndex", nameof(PaginationError.PageIndexMustBeAtLeastOne)); 
        modelState.AddModelError("PerPage", nameof(PaginationError.PerPageExceedsMaximum)); 

        var exception = Assert.Throws<ValidationException>(() => new ValidatorActionFilter().OnActionExecuting(filterContext)); 

        Assert.Equal(FailureReasons.InvalidPagination, exception.Reason); 

        var pageFailure = exception.Failures.Single(f => f.Field == "PageIndex"); 
        Assert.Equal(nameof(PaginationError.PageIndexMustBeAtLeastOne), pageFailure.Code); 
        Assert.Equal(FailureReasons.InvalidPagination, pageFailure.Reason); 

        Assert.Equal(exception.Failures.Count, exception.Failures.Select(f => f.Code + f.Field).Distinct().Count()); 
    } 

    [Fact] 
    public void FilterValidatesIValidatableObjectArgumentsThatNeverReachedModelState() 
    { 
        var filterContext = Context(out _); 
        filterContext.ActionArguments["request"] = new SelfValidatingRequest(); 

        var exception = Assert.Throws<ValidationException>(() => new ValidatorActionFilter().OnActionExecuting(filterContext)); 

        var failure = Assert.Single(exception.Failures); 
        Assert.Equal(nameof(FailureReasons.MissingModificationReasons), failure.Code); 
        Assert.Equal(nameof(SelfValidatingRequest.Reason), failure.Field); 
    } 

    [Fact] 
    public void FilterExposesTheRequestServicesToIValidatableObjectRules() 
    { 
        var withServices = Context(out _, new MarkerProvider()); 
        withServices.ActionArguments["request"] = new ServiceAwareRequest(); 

        // The rule only fails when its service is missing, so a clean run proves the scope reached ValidationContext.
        new ValidatorActionFilter().OnActionExecuting(withServices); 

        var withoutServices = Context(out _); 
        withoutServices.ActionArguments["request"] = new ServiceAwareRequest(); 

        var exception = Assert.Throws<ValidationException>(() => new ValidatorActionFilter().OnActionExecuting(withoutServices)); 

        Assert.Equal(nameof(FailureReasons.InvalidParameter), Assert.Single(exception.Failures).Code); 
    } 

    [Fact] 
    public void FilterLeavesValidRequestsAlone() 
    { 
        var filterContext = Context(out _); 
        filterContext.ActionArguments["request"] = new PagedRequestBase { PageIndex = 2, PerPage = 25 }; 

        new ValidatorActionFilter().OnActionExecuting(filterContext); 

        Assert.Null(filterContext.Result); 
    } 

    [Fact] 
    public void FilterReportsModelBindingFailuresAsInvalidParameter()
    {
        var filterContext = Context(out var modelState);
        var metadata = new EmptyModelMetadataProvider().GetMetadataForType(typeof(int));
        modelState.AddModelError("PageIndex", new InvalidOperationException("The value 'x' is not valid for PageIndex."), metadata);

        var exception = Assert.Throws<ValidationException>(() => new ValidatorActionFilter().OnActionExecuting(filterContext));

        var failure = Assert.Single(exception.Failures);
        Assert.Equal(nameof(FailureReasons.InvalidParameter), failure.Code);
        Assert.Equal(FailureReasons.InvalidParameter, failure.Reason);
    }

    private static ActionExecutingContext Context(out ModelStateDictionary modelState, IServiceProvider services = null)
    {
        var httpContext = new DefaultHttpContext();

        if (services != null) httpContext.RequestServices = services;

        modelState = new ModelStateDictionary();

        return new ActionExecutingContext(
            new ActionContext(httpContext, new RouteData(), new ActionDescriptor(), modelState),
            new List<IFilterMetadata>(),
            new Dictionary<string, object>(),
            controller: null);
    }

    private sealed class TenItemRequest : PagedRequestBase
    {
        protected override int? GetTotalItems(ValidationContext validationContext) => 10; 
    } 

    private sealed class NameOrderedRequest : LookupRequestBase 
    { 
        protected override bool IsOrderable(string orderBy) => orderBy == "Name"; 
    } 

    private sealed class SelfValidatingRequest : IValidatableObject 
    { 
        public string Reason { get; set; } 

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext) 
        { 
            if (string.IsNullOrWhiteSpace(Reason)) yield return new ValidationResult(nameof(FailureReasons.MissingModificationReasons), new[] { nameof(Reason) }); 
        } 
    } 

    public interface IMarker 
    { 
    } 

    private sealed class MarkerProvider : IServiceProvider 
    { 
        public object GetService(Type serviceType) => serviceType == typeof(IMarker) ? new object() : null; 
    } 

    private sealed class ServiceAwareRequest : IValidatableObject 
    { 
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext) 
        { 
            if (validationContext.GetService(typeof(IMarker)) == null) yield return new ValidationResult(nameof(FailureReasons.InvalidParameter), Array.Empty<string>()); 
        } 
    } 

} 
