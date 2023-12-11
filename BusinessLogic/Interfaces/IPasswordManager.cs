namespace BusinessLogic.Interfaces;

public interface IPasswordManager
{
    string HashPassword(string Password);
    bool ComparePassword(string Vanilla, string Hashed);
}