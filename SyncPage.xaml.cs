
namespace FavoritasBackUp;

public partial class SyncPage : ContentPage
{
    public SyncPage()
    {
        InitializeComponent();
    }

    private async void OnSettingsClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync($"//{nameof(ConfigPage)}");
    }

    private async void OnSyncClicked(object sender, EventArgs e)
    {
        await DisplayAlertAsync(
            "Sincronização",
            "A sincronização com a nuvem será implementada em breve.",
            "OK");
    }
}
