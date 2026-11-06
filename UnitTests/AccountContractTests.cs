using System;
using System.Collections.Generic;
using System.Linq;

using System.Net;

using System.Net.Http;

using System.Net.Http.Headers;

using System.Text;

using System.Text.Json;

using System.Threading.Tasks;


using BusinessLogic.Interfaces;


using Microsoft.Extensions.DependencyInjection;

using Microsoft.Extensions.Options;


using Models;

using Models.Helpers;


using Repository.ExtendedRepositories;


using Services.DTOs;

using Services.DTOs.Requests;


using Xunit;


namespace UnitTests;


/// <summary>
///     Account, session and role-administration evidence through the booted application. Everything here goes over HTTP against the real pipeline - JWT authentication, the session handler that consults the account row, the role guards, and the validation filter - because that is what a consumer of the template actually depends on: a token that stops working when it should, a locked account that answers 403 instead of pretending the password was wrong, a deactivated account whose existing sessions stop working immediately, and an insufficient role that is refused with 403 rather than 401. Accounts are seeded with unique names so no test can observe another test's state through the shared harness database.  
/// </summary>
public class AccountContractTests : MainWebApplicationFactory<Program>

{

    public AccountContractTests() => Setup();


    private const string StrongPassword = "Harness123!";

    private const string OtherStrongPassword = "Harness456!";


    private static readonly JsonSerializerOptions ReadOptions = new() { PropertyNameCaseInsensitive = true };


    private static StringContent Json(object body) => new StringContent(
        JsonSerializer.Serialize(body),
        Encoding.UTF8,
        "application/json");


    private static async Task<T> Read<T>(HttpResponseMessage response)

    {

        var body = await response.Content.ReadAsStringAsync();


        return JsonSerializer.Deserialize<T>(body, ReadOptions)!;

    }


    private HttpClient ClientWithToken(string token)

    {

        var client = CreateClient();

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);


        return client;

    }


    /// <summary>Signs in through the public token endpoint and returns the access token, or null when the sign-in was refused.</summary>  
    private async Task<string?> IssueToken(string userName, string password)

    {

        var response = await CreateClient()
            .PostAsync("/api/Account/Token", Json(new UserAuthenticationRequest { Username = userName, Password = password }));


        if (!response.IsSuccessStatusCode) return null;


        return (await Read<UserAuthenticationResult>(response)).Token;

    }


    private async Task<HttpStatusCode> TryLogin(string userName, string password) => (await CreateClient()
        .PostAsync("/api/Account/Token", Json(new UserAuthenticationRequest { Username = userName, Password = password })))
        .StatusCode;


    /// <summary>Seeds an account with a unique name and returns its id and name.</summary>  
    private (int Id, string UserName) SeedAccount(string prefix, RoleNames roleName)

    {

        var userName = $"{prefix}-{Guid.NewGuid():N}";

        userName = userName[..28];


        return (SeedUser(userName, StrongPassword, roleName.ToString()), userName);

    }


    [Fact]
    public async Task RefreshedTokenWorksAndLoggingOutOfAllDevicesDeniesEveryEarlierToken()

    {

        var (userId, userName) = SeedAccount("session-flow", RoleNames.Admin);



        var originalToken = await IssueToken(userName, StrongPassword);



        Assert.NotNull(originalToken);



        var refreshedClient = ClientWithToken(originalToken);



        var refreshResponse = await refreshedClient.PostAsync("/api/Account/RefreshToken", null);



        Assert.Equal(HttpStatusCode.OK, refreshResponse.StatusCode);



        var refreshed = await Read<UserAuthenticationResult>(refreshResponse);



        Assert.False(string.IsNullOrEmpty(refreshed.Token));



        // A single-device logout is the client's business: the server leaves every session alone.  
        var singleDeviceLogout = await refreshedClient.PostAsync(
            "/api/Account/Logout",
            Json(new LogoutRequest { IsFromAllDevices = false }));



        Assert.Equal(HttpStatusCode.OK, singleDeviceLogout.StatusCode);



        Assert.Equal(HttpStatusCode.OK, (await refreshedClient.PostAsync("/api/Account/RefreshToken", null)).StatusCode);



        // From all devices records a logout instant, so every token issued before it is now older than the account's own logout.  
        var allDevicesLogout = await refreshedClient.PostAsync(
            "/api/Account/Logout",
            Json(new LogoutRequest { IsFromAllDevices = true }));



        Assert.Equal(HttpStatusCode.OK, allDevicesLogout.StatusCode);



        // The caller still presents a well-formed, unexpired token, so authentication succeeds; what fails is the session policy, and ASP.NET answers that as 403 rather than 401.
        Assert.Equal(HttpStatusCode.Forbidden, (await refreshedClient.PostAsync("/api/Account/RefreshToken", null)).StatusCode);



        // The token that was replaced by the refresh is denied too - it was issued before the logout instant as well.  
        Assert.Equal(HttpStatusCode.Forbidden, (await ClientWithToken(originalToken).PostAsync("/api/Account/Logout", Json(new LogoutRequest { IsFromAllDevices = false }))).StatusCode);
    }


    [Fact]
    public async Task FiveWrongPasswordsLockTheAccountAndASuccessfulSignInClearsTheCounter()

    {

        var threshold = Scope().ServiceProvider.GetRequiredService<IOptions<AppSettings>>().Value.AccountLockThreshold;



        Assert.Equal(5, threshold);



        var (_, userName) = SeedAccount("lockout", RoleNames.Admin);



        for (var attempt = 1; attempt <= 3; attempt++)
            Assert.Equal(HttpStatusCode.NotFound, await TryLogin(userName, "WrongPassword1!"));



        // Signing in successfully is what clears the counter; without that clear the next stretch of failures would lock far earlier.  
        Assert.Equal(HttpStatusCode.Accepted, await TryLogin(userName, StrongPassword));



        for (var attempt = 1; attempt <= threshold; attempt++)
            Assert.Equal(HttpStatusCode.NotFound, await TryLogin(userName, "WrongPassword1!"));



        // Now the threshold is reached: even the correct password is refused, and with 403 so a client can tell this apart from a wrong password.  
        Assert.Equal(HttpStatusCode.Forbidden, await TryLogin(userName, StrongPassword));



        // The lock belongs to that account only - another account in the same tenant still signs in.  
        var otherAccount = SeedAccount("lockout-bystander", RoleNames.DataManager);



        Assert.Equal(HttpStatusCode.Accepted, await TryLogin(otherAccount.UserName, StrongPassword));
    }


    [Fact]
    public async Task DeactivatingAnAccountKillsItsSessionsAndActivateReturnsItToService()

    {

        var (userId, userName) = SeedAccount("deactivate", RoleNames.DataManager);



        var theirToken = await IssueToken(userName, StrongPassword);



        Assert.NotNull(theirToken);



        var theirClient = ClientWithToken(theirToken);



        var admin = CreateAuthenticatedClient(RoleNames.Admin.ToString(), "deactivate-admin");



        var deactivate = await admin.PutAsync(
            "/api/Account",
            Json(new UpdateUserRequest { UserId = userId, UserName = userName, Deactivated = true }));



        Assert.Equal(HttpStatusCode.OK, deactivate.StatusCode);



        // Their existing token stops working at once: the session check refuses a deactivated account.  
        Assert.Equal(HttpStatusCode.Forbidden, (await theirClient.PostAsync("/api/Account/Logout", Json(new LogoutRequest { IsFromAllDevices = false }))).StatusCode);



        var activeUsers = await Read<List<UserDTO>>(await admin.GetAsync("/api/Account"));



        Assert.DoesNotContain(activeUsers, user => user.Id == userId);



        var deactivatedUsers = await Read<List<UserDTO>>(await admin.GetAsync("/api/Account/deactivated"));



        Assert.Contains(deactivatedUsers, user => user.Id == userId);



        // It is deliberately invisible as an id lookup too - an inactive account cannot be read as if it were live.  
        Assert.Equal(HttpStatusCode.NotFound, (await admin.GetAsync($"/api/Account/{userId}")).StatusCode);



        Assert.Equal(HttpStatusCode.OK, (await admin.PutAsync($"/api/Account/{userId}/activate", null)).StatusCode);



        activeUsers = await Read<List<UserDTO>>(await admin.GetAsync("/api/Account"));



        Assert.Contains(activeUsers, user => user.Id == userId);



        // Reactivated means usable again: the same credentials sign in.  
        Assert.Equal(HttpStatusCode.Accepted, await TryLogin(userName, StrongPassword));
    }


    [Fact]
    public async Task RegistrationIssuesATemporaryPasswordAndTheFirstLoginClearsWhenTheOwnerSetsOne()

    {

        var admin = CreateAuthenticatedClient(RoleNames.Admin.ToString(), "register-admin");



        var userName = $"temp-password-{Guid.NewGuid():N}"[..26];



        // No password sent: the server generates one and returns it exactly once.  
        var registerResponse = await admin.PostAsync("/api/Account/Register", Json(new CreateUserRequest { UserName = userName }));



        Assert.Equal(HttpStatusCode.OK, registerResponse.StatusCode);



        var created = await Read<CreateUserResult>(registerResponse);



        Assert.True(created.IsFirstLogin);



        Assert.False(string.IsNullOrEmpty(created.TempPassword));


        
 var takenAgain = await admin.PostAsync("/api/Account/Register", Json(new CreateUserRequest { UserName = userName }));


        
 Assert.Equal(HttpStatusCode.Conflict, takenAgain.StatusCode);


        
 var takenBody = await takenAgain.Content.ReadAsStringAsync();


        
 Assert.Contains(nameof(FailureReasonsProbe.UsernameAlreadyTaken), takenBody);


        
 var theirToken = await IssueToken(userName, created.TempPassword!);


        
 Assert.NotNull(theirToken);


        
 var theirClient = ClientWithToken(theirToken!);


        
 // While the account is on its temporary password the owner cannot be asked to prove it.  
 var firstLoginReset = await theirClient.PostAsync(
            "/api/Account/reset-password",
            Json(new UpdatePasswordRequest { NewPassword = StrongPassword }));



        Assert.Equal(HttpStatusCode.OK, firstLoginReset.StatusCode);



        // Afterwards the old password is required, so a stolen token alone cannot change it again.  
        var secondResetWithoutCurrent = await theirClient.PostAsync(
            "/api/Account/reset-password",
            Json(new UpdatePasswordRequest { NewPassword = OtherStrongPassword }));



        Assert.Equal(HttpStatusCode.Forbidden, secondResetWithoutCurrent.StatusCode);



        Assert.Equal(HttpStatusCode.OK, (await theirClient.PostAsync(
            "/api/Account/reset-password",
            Json(new UpdatePasswordRequest { CurrentPassword = StrongPassword, NewPassword = OtherStrongPassword }))).StatusCode);



        Assert.Equal(HttpStatusCode.Accepted, await TryLogin(userName, OtherStrongPassword));
    }


    [Fact]
    public async Task AnInsufficientRoleIsRefusedWithForbiddenRatherThanUnauthenticated()

    {

        var limited = CreateAuthenticatedClient(RoleNames.DataManager.ToString(), "limited-role");



        var accountsResponse = await limited.GetAsync("/api/Account");



        Assert.Equal(HttpStatusCode.Forbidden, accountsResponse.StatusCode);



        var body = await accountsResponse.Content.ReadAsStringAsync();



        Assert.Contains("any", body);



        Assert.Contains(RoleNames.Admin.ToString(), body);



        Assert.Equal(HttpStatusCode.Forbidden, (await limited.GetAsync("/api/RoleSettings")).StatusCode);



        // The same endpoint is open to an account that does hold the role.  
        var admin = CreateAuthenticatedClient(RoleNames.Admin.ToString(), "allowed-role");



        Assert.Equal(HttpStatusCode.OK, (await admin.GetAsync("/api/Account")).StatusCode);
    }


    [Fact]
    public async Task RoleAdministrationReadsCountsAndWritesTheExactPermissionSetOfARole()

    {

        var admin = CreateAuthenticatedClient(RoleNames.Admin.ToString(), "role-admin");



        var roles = await Read<List<RoleUserCountDto>>(await admin.GetAsync("/api/RoleSettings"));



        Assert.Contains(roles, role => role.Name == RoleNames.Admin.ToString());



        Assert.True(roles.Single(role => role.Name == RoleNames.Admin.ToString()).UsersCount >= 1);



        var createdName = $"Auditor-{Guid.NewGuid():N}"[..24];



        Assert.Equal(HttpStatusCode.OK, (await admin.PostAsync("/api/RoleSettings", Json(new CreateRoleRequest { Name = createdName }))).StatusCode);



        roles = await Read<List<RoleUserCountDto>>(await admin.GetAsync("/api/RoleSettings"));



        var createdRole = roles.Single(role => role.Name == createdName);



        Assert.Equal(0, createdRole.UsersCount);



        // Renaming keeps the same role id, so account and permission assignments survive it.  
        Assert.Equal(HttpStatusCode.OK, (await admin.PutAsync($"/api/RoleSettings/{createdRole.Id}", Json(new UpdateRoleRequest { Name = $"{createdName}-renamed" }))).StatusCode);



        roles = await Read<List<RoleUserCountDto>>(await admin.GetAsync("/api/RoleSettings"));



        var renamed = roles.Single(role => role.Name == $"{createdName}-renamed");



        Assert.Equal(createdRole.Id, renamed.Id);



        // A second role cannot take a name that already exists.  
        Assert.Equal(HttpStatusCode.BadRequest, (await admin.PostAsync("/api/RoleSettings", Json(new CreateRoleRequest { Name = renamed.Name }))).StatusCode);



        var matrix = await Read<RolesAndPermissionsDto>(await admin.GetAsync("/api/RoleSettings/permissions-with-roles"));



        Assert.NotEmpty(matrix.Permissions);



        var grantedIds = matrix.Permissions.Select(permission => permission.PermissionId).Take(2).ToList();


        
 // Writing the set for a brand new role: exactly these permissions, nothing else.  
 Assert.Equal(HttpStatusCode.OK, (await admin.PutAsync(
            "/api/RoleSettings/permissions-with-roles",
            Json(new UpdateRolePermissionsRequest { RoleId = renamed.Id, PermissionIds = grantedIds }))).StatusCode);


        
 matrix = await Read<RolesAndPermissionsDto>(await admin.GetAsync("/api/RoleSettings/permissions-with-roles"));


        
 foreach (var permission in matrix.Permissions)
        {

            if (grantedIds.Contains(permission.PermissionId))

                Assert.Contains(permission.Roles, role => role.Name == renamed.Name);

            else

                Assert.DoesNotContain(permission.Roles, role => role.Name == renamed.Name);

        }


        
 // An unknown permission id is rejected before anything is written, and an unknown role id never reaches the manager.  
 var unknownPermissionWrite = await admin.PutAsync(
            "/api/RoleSettings/permissions-with-roles",
            Json(new UpdateRolePermissionsRequest { RoleId = renamed.Id, PermissionIds = [999999] }));


        
 Assert.Equal(HttpStatusCode.BadRequest, unknownPermissionWrite.StatusCode);


        
 Assert.Equal(HttpStatusCode.BadRequest, (await admin.PutAsync(
            "/api/RoleSettings/permissions-with-roles",
            Json(new UpdateRolePermissionsRequest { RoleId = 987654, PermissionIds = grantedIds }))).StatusCode);
    }


    /// <summary>Only used to assert the machine-readable code travels in the response body.</summary>  
    private enum FailureReasonsProbe

    {

        UsernameAlreadyTaken

    }

}
