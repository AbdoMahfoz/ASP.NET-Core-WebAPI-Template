using System;
using System.Collections.Generic;
using System.Text;

namespace BusinessLogic.Interfaces
{
    public interface IPasswordManager
    {
        string HashPassword(string Password);
        bool ComparePassword(string Vanilla, string Hashed);
    }
}
