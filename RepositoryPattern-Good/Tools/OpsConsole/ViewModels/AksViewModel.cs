using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WebShop.Tools.OpsConsole.Models;
using WebShop.Tools.OpsConsole.Services;

namespace WebShop.Tools.OpsConsole.ViewModels;

public sealed partial class AksViewModel(
    AksOpsService service, OperationLog log, Func<bool> isSignedIn, ObservableCollection<string> resourceGroups) : ObservableObject
{
    public IReadOnlyList<AzureRegion> EuropeanRegions => AzureRegions.Europe;
    public ObservableCollection<string> ResourceGroups => resourceGroups;

    [ObservableProperty]
    private string _resourceGroup = "webshop-rg";

    [ObservableProperty]
    private string _location = "westeurope";

    [ObservableProperty]
    private string _clusterName = "webshop-aks";

    [ObservableProperty]
    private string _acrName = "psrepo";

    [ObservableProperty]
    private string _acrResourceGroup = "Dapr";

    [ObservableProperty]
    private string _imageTag = "v7-cosmos";

    [ObservableProperty]
    private string _embeddingImageTag = "v2";

    [ObservableProperty]
    private bool _buildAndPushImages = true;

    [ObservableProperty]
    private string _cosmosAccountName = "pscosmosdb";

    [ObservableProperty]
    private string _cosmosResourceGroup = "AI-200";

    [ObservableProperty]
    private string _postgresPassword = "";

    [ObservableProperty]
    private string _jwtSigningKey = "";

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private string? _resultUrl;

    public bool HasResultUrl => !string.IsNullOrEmpty(ResultUrl);

    partial void OnResultUrlChanged(string? value) => OnPropertyChanged(nameof(HasResultUrl));

    [RelayCommand]
    private void GeneratePostgresPassword() => PostgresPassword = RandomSecrets.GenerateAdminPassword();

    [RelayCommand]
    private void GenerateJwtSigningKey() => JwtSigningKey = RandomSecrets.GenerateJwtSigningKey();

    [RelayCommand]
    private async Task SetupAndDeployAsync()
    {
        if (!isSignedIn())
        {
            log.Append("Sign in first (see the Account panel above) - every action here shells out to `az`/`kubectl`/`docker`.");
            return;
        }

        if (string.IsNullOrWhiteSpace(PostgresPassword) || string.IsNullOrWhiteSpace(JwtSigningKey))
        {
            log.Append("Set (or generate) both the Postgres password and the JWT signing key before deploying.");
            return;
        }

        IsBusy = true;
        ResultUrl = null;
        try
        {
            log.Append($"--- Setting up AKS cluster '{ClusterName}' and deploying WebShop ---");
            var url = await service.SetupAndDeployAsync(
                ResourceGroup, Location, ClusterName, AcrName, AcrResourceGroup,
                ImageTag, EmbeddingImageTag, BuildAndPushImages,
                CosmosAccountName, CosmosResourceGroup, PostgresPassword, JwtSigningKey, log.Append);

            if (url is not null)
            {
                ResultUrl = url;
                log.Append($"AKS deploy finished: {url}");
            }
            else
            {
                log.Append("AKS deploy did not complete successfully - see the log above for where it stopped.");
            }
        }
        finally { IsBusy = false; }
    }
}
