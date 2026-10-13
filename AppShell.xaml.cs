namespace FavoritasBackUp
{
    using FavoritasBackUp.Services;

    public partial class AppShell : Shell
    {
        private readonly CloudConfigurationService _configurationService;
        private bool _initializationStarted;

        public AppShell(CloudConfigurationService configurationService)
        {
            ArgumentNullException.ThrowIfNull(configurationService);
            _configurationService = configurationService;
            InitializeComponent();
            Loaded += OnLoaded;
        }

        private async void OnLoaded(object? sender, EventArgs e)
        {
            if (_initializationStarted)
            {
                return;
            }

            _initializationStarted = true;
            Loaded -= OnLoaded;
            await InitializeAsync();
        }

        private async Task InitializeAsync()
        {
            IsEnabled = false;

            try
            {
                bool hasConfiguration = false;
                bool storageReadFailed = false;

                try
                {
                    hasConfiguration = await _configurationService.HasConfigurationAsync();
                }
                catch (Exception)
                {
                    storageReadFailed = true;
                }

                var loadingItem = CurrentItem;
                var initialRoute = hasConfiguration ? nameof(SyncPage) : nameof(ConfigPage);
                await GoToAsync($"//{initialRoute}");
                Items.Remove(loadingItem);

                if (storageReadFailed)
                {
                    await DisplayAlertAsync(
                        "Erro ao carregar",
                        "Não foi possível verificar as configurações salvas. Revise ou informe os dados na tela de configuração.",
                        "OK");
                }
            }
            finally
            {
                IsEnabled = true;
            }
        }
    }
}
