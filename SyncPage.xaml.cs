using FavoritasBackUp.Services;
#if ANDROID
using FavoritasBackUp.Platforms.Android.Services;
#endif

namespace FavoritasBackUp;

public partial class SyncPage : ContentPage
{
    private readonly INextcloudBackupService? _backupService;
    private CancellationTokenSource? _backupCancellation;
    private bool _isSyncing;

    public SyncPage(INextcloudBackupService? backupService = null)
    {
        _backupService = backupService;
        InitializeComponent();
    }

    protected override void OnDisappearing()
    {
        _backupCancellation?.Cancel();
        base.OnDisappearing();
    }

    private async void OnSettingsClicked(object? sender, EventArgs e)
    {
        await Shell.Current.GoToAsync($"//{nameof(ConfigPage)}");
    }

    private async void OnSyncClicked(object? sender, EventArgs e)
    {
        if (_isSyncing) return;
        _isSyncing = true;
        SyncButton.IsEnabled = false;
        ReadIndicator.IsVisible = ReadIndicator.IsRunning = true;
        using var cancellation = new CancellationTokenSource();
        _backupCancellation = cancellation;
        try
        {
            if (_backupService is null)
                throw new NotSupportedException("O backup de imagens está disponível somente no Android.");
#if ANDROID
            var access = PhotoLibraryPermission.GetAccess();
            if (access != PhotoLibraryAccess.Full)
            {
                await Permissions.RequestAsync<PhotoLibraryPermission>();
                cancellation.Token.ThrowIfCancellationRequested();
                access = PhotoLibraryPermission.GetAccess();
            }
            if (access == PhotoLibraryAccess.Denied)
                throw new UnauthorizedAccessException();
#endif
            SyncStatusLabel.Text = "Enviando imagens...";
            var result = await _backupService.BackupTodayAsync(cancellation.Token);
            cancellation.Token.ThrowIfCancellationRequested();
            var message = $"Fotos favoritas de hoje: {result.Total}\nEnviadas com sucesso: {result.Uploaded}\nJá existentes/ignoradas: {result.Skipped}\nFalhas: {result.Failed}";
            if (result.Error is not null) message += $"\n\n{result.Error}";
            SyncStatusLabel.Text = result.Failed > 0 ? "Backup concluído com falhas"
                : result.Total == 0 ? "Nenhuma favorita capturada hoje foi encontrada" : "Backup concluído";
#if ANDROID
            var finalAccess = PhotoLibraryPermission.GetAccess();
            if (access == PhotoLibraryAccess.Partial || finalAccess != PhotoLibraryAccess.Full)
                message += "\n\nAcesso limitado: o resultado abrange somente as imagens autorizadas e acessíveis. Permita acesso a todas as imagens para consultar todas as favoritas.";
#endif
            // No files and partial failures must not look like a fully successful backup.
            if (result.Total > 0 && result.Failed == 0)
                LastSyncLabel.Text = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss");
            await DisplayAlertAsync("Resultado do backup", message, "OK");
        }
        catch (OperationCanceledException)
        {
            SyncStatusLabel.Text = "Backup cancelado; arquivos já enviados foram preservados";
        }
        catch (UnauthorizedAccessException)
        {
            SyncStatusLabel.Text = "Acesso às imagens não permitido";
            await DisplayAlertAsync("Permissão necessária", "Permita a leitura das imagens nas configurações do Android para realizar o backup.", "OK");
        }
        catch (NotSupportedException exception)
        {
            SyncStatusLabel.Text = "Backup indisponível";
            await DisplayAlertAsync("Backup indisponível", exception.Message, "OK");
        }
        catch (Exception)
        {
            SyncStatusLabel.Text = "Não foi possível concluir o backup";
            await DisplayAlertAsync("Erro no backup", "Não foi possível consultar as imagens ou concluir o backup. Verifique as permissões e tente novamente.", "OK");
        }
        finally
        {
            _backupCancellation = null;
            _isSyncing = false;
            ReadIndicator.IsVisible = ReadIndicator.IsRunning = false;
            SyncButton.IsEnabled = true;
        }
    }
}
