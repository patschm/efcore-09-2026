using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using WebShop.Tools.OpsConsole.Models;
using WebShop.Tools.OpsConsole.Services;

namespace WebShop.Tools.OpsConsole.ViewModels;

public sealed partial class PostgresViewModel(
    PostgresOpsService service, OperationLog log, Func<bool> isSignedIn, ObservableCollection<string> resourceGroups) : ObservableObject
{
    public IReadOnlyList<AzureRegion> EuropeanRegions => AzureRegions.Europe;
    public ObservableCollection<string> ResourceGroups => resourceGroups;

    // Which of the two ways this repo already runs Postgres to use - see PostgresHostingMode's
    // own comment. Only AzureFlexibleServer needs a signed-in `az` session; DockerContainer is
    // pure local Docker and works offline.
    [ObservableProperty]
    private PostgresHostingMode _hostingMode = PostgresHostingMode.AzureFlexibleServer;

    public bool IsAzureMode => HostingMode == PostgresHostingMode.AzureFlexibleServer;
    public bool IsContainerMode => HostingMode == PostgresHostingMode.DockerContainer;

    partial void OnHostingModeChanged(PostgresHostingMode value)
    {
        OnPropertyChanged(nameof(IsAzureMode));
        OnPropertyChanged(nameof(IsContainerMode));
    }

    // --- Azure Flexible Server fields ---

    [ObservableProperty]
    private string _resourceGroup = "webshop-postgres-rg";

    // northeurope, not westeurope (this app's other defaults) - this subscription is restricted
    // from provisioning Postgres Flexible Server in westeurope specifically, discovered the hard
    // way while building infra/aca.bicep (see that file's postgresLocation param and project
    // memory's aca-deployment note). Still a plain dropdown entry, not disabled - picking
    // westeurope here fails with a real `az` error, which is the correct/expected behavior to
    // surface, not something to silently prevent.
    [ObservableProperty]
    private string _location = "northeurope";

    [ObservableProperty]
    private string _serverName = "webshop-standalone-pg";

    // --- Docker container fields ---

    [ObservableProperty]
    private string _containerName = "webshop-postgres";

    [ObservableProperty]
    private string _containerPort = "5432";

    [ObservableProperty]
    private string _volumeName = "webshop-postgres-data";

    [ObservableProperty]
    private bool _removeVolumeOnDelete;

    // --- Shared fields ---

    [ObservableProperty]
    private string _adminLogin = "webshopadmin";

    // A plain (not masked) field, deliberately - this is a single-user local ops tool, not a
    // shared/multi-tenant app, and the value only ever flows into a subprocess environment
    // variable (see PostgresOpsService), never to disk. Same tradeoff as everywhere else in this
    // app: real security engineering (a masked PasswordBox needs its own binding workaround in
    // WPF) wasn't worth it for a tool only its own operator ever sees.
    [ObservableProperty]
    private string _adminPassword = "";

    [ObservableProperty]
    private string _databaseName = "webshop";

    [ObservableProperty]
    private string _dumpFilePath = "";

    [ObservableProperty]
    private bool _isBusy;

    private void LogNotSignedIn() => log.Append("Sign in first (see the Account panel above) - every action here shells out to `az`.");

    [RelayCommand]
    private void GeneratePassword() => AdminPassword = RandomSecrets.GenerateAdminPassword();

    [RelayCommand]
    private void BrowseDumpFile()
    {
        var dialog = new OpenFileDialog { Filter = "SQL dump (*.sql)|*.sql|All files (*.*)|*.*" };
        if (dialog.ShowDialog() == true)
            DumpFilePath = dialog.FileName;
    }

    [RelayCommand]
    private async Task CreateAsync()
    {
        if (IsAzureMode && !isSignedIn()) { LogNotSignedIn(); return; }
        if (string.IsNullOrWhiteSpace(AdminPassword))
        {
            log.Append("Set (or generate) an admin password before creating the server.");
            return;
        }

        IsBusy = true;
        try
        {
            if (IsAzureMode)
            {
                log.Append($"--- Creating Postgres Flexible Server '{ServerName}' in resource group '{ResourceGroup}' ---");
                var result = await service.CreateAsync(ResourceGroup, Location, ServerName, AdminLogin, AdminPassword, log.Append);
                log.Append(result.Succeeded ? "Create finished." : $"Create failed (exit code {result.ExitCode}).");
            }
            else
            {
                if (!int.TryParse(ContainerPort, out var port))
                {
                    log.Append($"'{ContainerPort}' isn't a valid port number.");
                    return;
                }

                log.Append($"--- Starting Postgres container '{ContainerName}' ---");
                var result = await service.CreateContainerAsync(ContainerName, port, VolumeName, AdminPassword, DatabaseName, log.Append);
                log.Append(result.Succeeded ? "Container started." : $"Failed to start the container (exit code {result.ExitCode}).");
            }
        }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private async Task RestoreAsync()
    {
        if (IsAzureMode && !isSignedIn()) { LogNotSignedIn(); return; }
        if (string.IsNullOrWhiteSpace(DumpFilePath))
        {
            log.Append("Pick a pg_dump .sql file before restoring.");
            return;
        }

        IsBusy = true;
        try
        {
            if (IsAzureMode)
            {
                log.Append($"--- Restoring '{DumpFilePath}' into '{ServerName}' ---");
                var result = await service.RestoreAsync(ServerName, AdminLogin, AdminPassword, DatabaseName, DumpFilePath, log.Append);
                log.Append(result.Succeeded ? "Restore finished." : $"Restore failed (exit code {result.ExitCode}).");
            }
            else
            {
                log.Append($"--- Restoring '{DumpFilePath}' into container '{ContainerName}' ---");
                var result = await service.RestoreContainerAsync(ContainerName, AdminLogin, DatabaseName, DumpFilePath, log.Append);
                log.Append(result.Succeeded ? "Restore finished." : $"Restore failed (exit code {result.ExitCode}).");
            }
        }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private async Task DeleteAsync()
    {
        if (IsAzureMode && !isSignedIn()) { LogNotSignedIn(); return; }
        IsBusy = true;
        try
        {
            if (IsAzureMode)
            {
                log.Append($"--- Deleting Postgres Flexible Server '{ServerName}' ---");
                var result = await service.DeleteAsync(ResourceGroup, ServerName, log.Append);
                log.Append(result.Succeeded ? "Delete finished." : $"Delete failed (exit code {result.ExitCode}).");
            }
            else
            {
                log.Append($"--- Removing container '{ContainerName}' ---");
                var result = await service.DeleteContainerAsync(ContainerName, RemoveVolumeOnDelete, VolumeName, log.Append);
                log.Append(result.Succeeded ? "Container removed." : $"Failed to remove the container (exit code {result.ExitCode}).");
            }
        }
        finally { IsBusy = false; }
    }
}
