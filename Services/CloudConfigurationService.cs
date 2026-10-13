using FavoritasBackUp.Models;
using Microsoft.Maui.Storage;

namespace FavoritasBackUp.Services;

public class CloudConfigurationService
{
    private const string UrlKey = "CloudConfiguration.Url";
    private const string UsernameKey = "CloudConfiguration.Username";
    private const string PasswordKey = "CloudConfiguration.Password";

    private readonly ISecureStorage _secureStorage;

    public CloudConfigurationService(ISecureStorage secureStorage)
    {
        ArgumentNullException.ThrowIfNull(secureStorage);
        _secureStorage = secureStorage;
    }

    public async Task SaveAsync(CloudConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        await _secureStorage.SetAsync(UrlKey, configuration.Url);
        await _secureStorage.SetAsync(UsernameKey, configuration.Username);
        await _secureStorage.SetAsync(PasswordKey, configuration.Password);
    }

    public async Task<CloudConfiguration?> GetAsync()
    {
        var url = await _secureStorage.GetAsync(UrlKey);
        var username = await _secureStorage.GetAsync(UsernameKey);
        var password = await _secureStorage.GetAsync(PasswordKey);

        if (string.IsNullOrEmpty(url) ||
            string.IsNullOrEmpty(username) ||
            string.IsNullOrEmpty(password))
        {
            return null;
        }

        return new CloudConfiguration
        {
            Url = url,
            Username = username,
            Password = password
        };
    }

    public async Task<bool> HasConfigurationAsync()
    {
        return await GetAsync() is not null;
    }
}
