
using FavoritasBackUp.Services;
#if ANDROID
using FavoritasBackUp.Platforms.Android.Services;
#endif

namespace FavoritasBackUp;

public partial class SyncPage : ContentPage
{
    private readonly IPhotoLibraryService? _photoLibraryService;
    private CancellationTokenSource? _readingCancellation;
    private bool _isReading;

    public SyncPage(IPhotoLibraryService? photoLibraryService = null)
    {
        _photoLibraryService = photoLibraryService;
        InitializeComponent();
    }

    protected override void OnDisappearing()
    {
        _readingCancellation?.Cancel();
        base.OnDisappearing();
    }

    private async void OnSettingsClicked(object? sender, EventArgs e)
    {
        await Shell.Current.GoToAsync($"//{nameof(ConfigPage)}");
    }

    private async void OnSyncClicked(object? sender, EventArgs e)
    {
        if (_isReading) return;
        _isReading = true;
        SyncButton.IsEnabled = false;
        ReadIndicator.IsVisible = ReadIndicator.IsRunning = true;
        using var cancellation = new CancellationTokenSource();
        _readingCancellation = cancellation;
        try
        {
            if (_photoLibraryService is null)
            {
                await DisplayAlertAsync("Leitura indisponível", "A leitura da biblioteca de imagens está disponível somente no Android.", "OK");
                return;
            }
#if ANDROID
            var access = PhotoLibraryPermission.GetAccess();
            if (access != PhotoLibraryAccess.Full)
            {
                await Permissions.RequestAsync<PhotoLibraryPermission>();
                cancellation.Token.ThrowIfCancellationRequested();
                access = PhotoLibraryPermission.GetAccess();
            }
            if (access == PhotoLibraryAccess.Denied)
            {
                SyncStatusLabel.Text = "Permissão de leitura não concedida";
                await DisplayAlertAsync("Permissão necessária",
                    "Permita o acesso às imagens para realizar a contagem. Você pode revisar essa permissão nas configurações do Android. Nenhuma imagem foi enviada à nuvem.", "OK");
                return;
            }
#endif
            SyncStatusLabel.Text = "Lendo metadados das imagens...";
            var total = await _photoLibraryService.GetTotalPhotosCountAsync(cancellation.Token);
            string favoriteResult;
            try
            {
                var favorites = await _photoLibraryService.GetFavoritePhotosAsync(cancellation.Token);
                favoriteResult = $"Fotos favoritas encontradas: {favorites.Count}";
                if (favorites.Count == 0)
                    favoriteResult += "\n\nNenhuma favorita foi exposta pelo MediaStore nas imagens acessíveis. A Galeria Samsung pode manter essa marcação sem disponibilizá-la no campo público IS_FAVORITE.";
            }
            catch (NotSupportedException exception)
            {
                favoriteResult = $"Contagem de favoritas indisponível: {exception.Message}";
            }
            cancellation.Token.ThrowIfCancellationRequested();
            var message = $"Leitura concluída!\n\nImagens acessíveis: {total}\n\n{favoriteResult}";
#if ANDROID
            var finalAccess = PhotoLibraryPermission.GetAccess();
            if (finalAccess == PhotoLibraryAccess.Denied)
                throw new UnauthorizedAccessException();
            if (access == PhotoLibraryAccess.Partial || finalAccess == PhotoLibraryAccess.Partial)
                message += "\n\nAcesso parcial: os números abrangem somente as imagens autorizadas. É necessário permitir acesso a todas as imagens nas configurações do Android para localizar automaticamente todas as favoritas expostas pelo MediaStore.";
#endif
            message += "\n\nNenhuma imagem foi enviada à nuvem.";
            SyncStatusLabel.Text = "Leitura concluída; nenhum backup realizado";
            await DisplayAlertAsync("Diagnóstico de imagens", message, "OK");
        }
        catch (OperationCanceledException)
        {
            SyncStatusLabel.Text = "Leitura cancelada";
        }
        catch (UnauthorizedAccessException)
        {
            SyncStatusLabel.Text = "Acesso às imagens não permitido";
            await DisplayAlertAsync("Permissão de leitura", "O acesso às imagens foi negado ou revogado. Revise a permissão nas configurações do Android. Nenhuma imagem foi enviada à nuvem.", "OK");
        }
        catch (Exception)
        {
            SyncStatusLabel.Text = "Não foi possível concluir a leitura";
            await DisplayAlertAsync("Erro na leitura", "Não foi possível consultar a biblioteca de imagens. Tente novamente e verifique as permissões. Nenhuma imagem foi enviada à nuvem.", "OK");
        }
        finally
        {
            _readingCancellation = null;
            _isReading = false;
            ReadIndicator.IsVisible = ReadIndicator.IsRunning = false;
            SyncButton.IsEnabled = true;
        }
    }
}
