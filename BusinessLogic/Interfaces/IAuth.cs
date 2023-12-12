using Models.DataModels;
using Services.DTOs;
using System;

namespace BusinessLogic.Interfaces;

public interface IAuth
{
    (string token, int userId) GenerateToken(int UserId);
    (string token, int userId) Authenticate(UserAuthenticationRequest request);
    void Logout(int UserId);
    bool Validate(int UserId, DateTime TokenIssuedDate);
}