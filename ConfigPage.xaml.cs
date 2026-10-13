
using FavoritasBackUp.Models;
using FavoritasBackUp.Services;

namespace FavoritasBackUp;

public partial class ConfigPage : ContentPage
{
    private readonly CloudConfigurationService _configurationService;

    public ConfigPage(CloudConfigurationService configurationService)
    {
        ArgumentNullException.ThrowIfNull(configurationService);
        _configurationService = configurationService;
        InitializeComponent();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        SaveConfigurationButton.IsEnabled = false;

        try
        {
            var configuration = await _configurationService.GetAsync();

            CloudAddressEntry.Text = configuration?.Url ?? string.Empty;
            UsernameEntry.Text = configuration?.Username ?? string.Empty;
            PasswordEntry.Text = configuration?.Password ?? string.Empty;
        }
        catch (Exception)
        {
            await DisplayAlertAsync(
                "Erro ao carregar",
                "Não foi possível recuperar as configurações salvas. Tente novamente.",
                "OK");
        }
        finally
        {
            SaveConfigurationButton.IsEnabled = true;
        }
    }

    private async void OnSaveConfigurationClicked(
        object? sender,
        EventArgs e)
    {
        if (!SaveConfigurationButton.IsEnabled)
        {
            return;
        }

        string address = CloudAddressEntry.Text?.Trim() ?? string.Empty;
        string username = UsernameEntry.Text?.Trim() ?? string.Empty;
        string password = PasswordEntry.Text ?? string.Empty;

        if (string.IsNullOrWhiteSpace(address) ||
            string.IsNullOrWhiteSpace(username) ||
            string.IsNullOrWhiteSpace(password))
        {
            await DisplayAlertAsync(
                "Campos obrigatórios",
                "Preencha o endereço da nuvem, o usuário e a senha.",
                "OK");

            return;
        }

        if (!Uri.TryCreate(address, UriKind.Absolute, out Uri? uri) ||
            (uri.Scheme != Uri.UriSchemeHttps &&
             uri.Scheme != Uri.UriSchemeHttp))
        {
            await DisplayAlertAsync(
                "Endereço inválido",
                "Informe um endereço válido começando com HTTP ou HTTPS.",
                "OK");

            return;
        }
        var configuration = new CloudConfiguration
        {
            Url = address,
            Username = username,
            Password = password
        };

        SaveConfigurationButton.IsEnabled = false;

        try
        {
            await _configurationService.SaveAsync(configuration);
        }
        catch (Exception)
        {
            await DisplayAlertAsync(
                "Erro ao salvar",
                "Não foi possível salvar as configurações com segurança. Tente novamente.",
                "OK");

            return;
        }
        finally
        {
            SaveConfigurationButton.IsEnabled = true;
        }

        await Shell.Current.GoToAsync($"//{nameof(SyncPage)}");
    }
}
