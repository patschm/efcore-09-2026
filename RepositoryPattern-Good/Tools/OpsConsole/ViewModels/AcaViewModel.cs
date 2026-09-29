using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WebShop.Tools.OpsConsole.Models;
using WebShop.Tools.OpsConsole.Services;

namespace WebShop.Tools.OpsConsole.ViewModels;

public sealed partial class AcaViewModel(
    AcaOpsService service, OperationLog log, Func<bool> isSignedIn, ObservableCollection<string> resourceGroups) : ObservableObject
{
    public IReadOnlyList<AzureRegion> EuropeanRegions => AzureRegions.Europe;
    public ObservableCollection<string> ResourceGroups => resourceGroups;

    [ObservableProperty]
    private string _resourceGroup = "webshop-aca-rg";

    [ObservableProperty]
    private string _location = "westeurope";

    [ObservableProperty]
    private string _environmentName = "webshop-aca-env";

    [ObservableProperty]
    private string _appImageTag = "v7-cosmos";

    [ObservableProperty]
    private string _embeddingImageTag = "v2";

    [ObservableProperty]
    private string _cosmosAccountName = "pscosmosdb";

    [ObservableProperty]
    private string _cosmosResourceGroup = "AI-200";

    [ObservableProperty]
    private string _postgresAdminPassword = "";

    [ObservableProperty]
    private string _jwtSigningKey = "";

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private string? _resultUrl;

    public bool HasResultUrl => !string.IsNullOrEmpty(ResultUrl);

    partial void OnResultUrlChanged(string? value) => OnPropertyChanged(nameof(HasResultUrl));

    [RelayCommand]
    private void GeneratePostgresPassword() => PostgresAdminPassword = RandomSecrets.GenerateAdminPassword();

    [RelayCommand]
    private void GenerateJwtSigningKey() => JwtSigningKey = RandomSecrets.GenerateJwtSigningKey();

    [RelayCommand]
    private async Task SetupAndDeployAsync()
    {
        if (!isSignedIn())
        {
            log.Append("Sign in first (see the Account panel above) - every action here shells out to `az`.");
            return;
        }

        if (string.IsNullOrWhiteSpace(PostgresAdminPassword) || string.IsNullOrWhiteSpace(JwtSigningKey))
        {
            log.Append("Set (or generate) both the Postgres admin password and the JWT signing key before deploying.");
            return;
        }

        IsBusy = true;
        ResultUrl = null;
        try
        {
            log.Append($"--- Setting up ACA environment '{EnvironmentName}' and deploying WebShop ---");
            var url = await service.SetupAndDeployAsync(
                ResourceGroup, Location, EnvironmentName, AppImageTag, EmbeddingImageTag,
                PostgresAdminPassword, CosmosAccountName, CosmosResourceGroup, JwtSigningKey, log.Append);

            if (url is not null)
            {
                ResultUrl = url;
                log.Append($"ACA deploy finished: {url}");
            }
            else
            {
                log.Append("ACA deploy did not complete successfully - see the log above for where it stopped.");
            }
        }
        finally { IsBusy = false; }
    }
}
