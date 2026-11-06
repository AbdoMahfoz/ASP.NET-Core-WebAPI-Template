using System.ComponentModel.DataAnnotations;
using Services.Exceptions;

namespace Services.DTOs;

public class UserAuthenticationRequest
{
    [Required(AllowEmptyStrings = false, ErrorMessage = nameof(FailureReasons.UsernameRequired))]
    public string Username { get; set; }

    [Required(AllowEmptyStrings = false, ErrorMessage = nameof(FailureReasons.PasswordRequired))]
    [MinLength(8, ErrorMessage = nameof(FailureReasons.PasswordTooShort))]
    public string Password { get; set; }
}
public class UserAuthenticationResult
{
    public int UserId { get; set; }
    public string Token { get; set; }
    public int ExpiresIn { get; set; }
    public UserAuthenticationResult() { }
    public UserAuthenticationResult(int UserId, string Token, int ExpiresIn)
    {
            this.UserId = UserId;
            this.Token = Token;
            this.ExpiresIn = ExpiresIn;
        }
}