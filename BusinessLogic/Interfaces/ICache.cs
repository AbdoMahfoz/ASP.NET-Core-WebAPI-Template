using System.Threading.Tasks;

namespace BusinessLogic.Interfaces;

public interface ICache
{
    Task SetValue(string key, object value);
    Task<T> GetValue<T>(string key);
    Task<object> GetValue(string key);
}