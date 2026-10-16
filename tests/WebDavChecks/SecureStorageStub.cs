// Minimal storage contract for testing the real configuration service without MAUI.
namespace Microsoft.Maui.Storage;

public interface ISecureStorage
{
    Task<string?> GetAsync(string key);
    Task SetAsync(string key, string value);
}
