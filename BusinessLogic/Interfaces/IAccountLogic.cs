using Services.DTOs;

namespace BusinessLogic.Interfaces
{
    public interface IAccountLogic
    {
        bool Register(UserAuthenticationRequest request, string Role);
    }
}
