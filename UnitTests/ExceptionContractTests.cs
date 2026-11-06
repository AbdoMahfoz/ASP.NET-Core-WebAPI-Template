using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using BusinessLogic.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Services.DTOs;
using Services.Exceptions;
using Xunit;

namespace UnitTests;

/// <summary>
///     Proves the exception contract end to end: a business service throws a typed exception, and the middleware chain turns it into the documented status plus a machine-readable body - while an unmapped failure still reaches the hardened generic handler.     
/// </summary>     
public class ExceptionContractTests     
{     
    private static MainWebApplicationFactory<Program> FactoryWithFault(Func<Exception> fault)     
    {         
        var factory = new MainWebApplicationFactory<Program>();         

        factory.OverrideServices(services =>         
        {             
            services.RemoveAll<IAuth>();             
            services.AddTransient<IAuth>(_ => new FaultyAuth(fault));         
        });         

        factory.Setup();         

        return factory;     
    }     

    /// <summary>A stand-in for the real auth service so a test can make an endpoint fail without any domain-specific failure endpoint.</summary>     
    private sealed class FaultyAuth(Func<Exception> fault) : IAuth     
    {         
        public (string token, int userId) GenerateToken(int UserId) => throw fault();         

        public (string token, int userId) Authenticate(UserAuthenticationRequest request) => throw fault();         

        public void Logout(int UserId)         
        {         
        }         

        public bool Validate(int UserId, DateTime TokenIssuedDate) => throw fault();     
    }     

    private static async Task<string> BodyOf(HttpResponseMessage response) => await response.Content.ReadAsStringAsync();     

    [Fact]     
    public async Task NotFoundExceptionBecomes404WithMachineReadableBody()     
    {         
        using var factory = FactoryWithFault(() => new NotFoundException(FailureReasons.NotFound));         
        var client = factory.CreateClient();         

        var response = await client.PostAsync("/api/Account/Token",             
            new StringContent("{\"username\":\"anyone\",\"password\":\"longenough\"}", Encoding.UTF8, "application/json"));         

        var body = await BodyOf(response);         

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);         
        Assert.Contains("application/json", response.Content.Headers.ContentType?.MediaType ?? "");         
        Assert.Contains("\"Error\":\"NotFound\"", body);         
        Assert.DoesNotContain("at WebAPI", body); // no stack trace on the wire     
    }     

    [Fact]     
    public async Task ForbiddenAccessExceptionBecomes403WithMachineReadableBody()     
    {         
        using var factory = FactoryWithFault(() => new ForbiddenAccessException(FailureReasons.InsufficientRole));         
        var client = factory.CreateClient();         

        var response = await client.PostAsync("/api/Account/Token",             
            new StringContent("{\"username\":\"anyone\",\"password\":\"longenough\"}", Encoding.UTF8, "application/json"));         

        var body = await BodyOf(response);         

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);         
        Assert.Contains("\"Error\":\"InsufficientRole\"", body);     
    }     

    [Fact]     
    public async Task UnmappedFailureStillReachesTheGenericHandlerAs500()     
    {         
        using var factory = FactoryWithFault(() => new InvalidOperationException("unexpected internal failure"));         
        var client = factory.CreateClient();         

        var response = await client.PostAsync("/api/Account/Token",             
            new StringContent("{\"username\":\"anyone\",\"password\":\"longenough\"}", Encoding.UTF8, "application/json"));         

        var body = await BodyOf(response);         

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);         
        Assert.Contains("unexpected internal failure", body);     
    }     

    [Fact]     
    public async Task LoginWithTooShortPasswordIsRejectedWithItsReasonCode()     
    {         
        using var factory = new MainWebApplicationFactory<Program>();         
        factory.Setup();         
        var client = factory.CreateClient();         

        var response = await client.PostAsync("/api/Account/Token",             
            new StringContent("{\"username\":\"someone\",\"password\":\"bad\"}", Encoding.UTF8, "application/json"));         

        var body = await BodyOf(response);         

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);         
        Assert.Contains(nameof(FailureReasons.PasswordTooShort), body);         
        Assert.Contains("\"Field\":\"Password\"", body);     
    }     

    [Fact]     
    public async Task LoginWithoutUsernameIsRejectedWithItsReasonCode()     
    {         
        using var factory = new MainWebApplicationFactory<Program>();         
        factory.Setup();         
        var client = factory.CreateClient();         

        var response = await client.PostAsync("/api/Account/Token",             
            new StringContent("{\"username\":\"\"}", Encoding.UTF8, "application/json"));         

        var body = await BodyOf(response);         

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);         
        Assert.Contains(nameof(FailureReasons.UsernameRequired), body);     
    } 
} 
