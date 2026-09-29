using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using WebShop.Tools.OpsConsole.Models;
using WebShop.Tools.OpsConsole.Services;

namespace WebShop.Tools.OpsConsole.ViewModels;

public sealed partial class CosmosViewModel(
    CosmosOpsService service, OperationLog log, Func<bool> isSignedIn, ObservableCollection<string> resourceGroups) : ObservableObject
{
    public IReadOnlyList<AzureRegion> EuropeanRegions => AzureRegions.Europe;
    public ObservableCollection<string> ResourceGroups => resourceGroups;

    [ObservableProperty]
    private string _resourceGroup = "webshop-cosmos-rg";

    [ObservableProperty]
    private string _location = "westeurope";

    [ObservableProperty]
    private string _accountName = "pscosmosdb";

    [ObservableProperty]
    private string _dumpFolder = "";

    [ObservableProperty]
    private bool _isBusy;

    private void LogNotSignedIn() => log.Append("Sign in first (see the Account panel above) - every action here shells out to `az`.");

    [RelayCommand]
    private void BrowseDumpFolder()
    {
        var dialog = new OpenFolderDialog { Title = "Select an NDJSON dump folder" };
        if (dialog.ShowDialog() == true)
            DumpFolder = dialog.FolderName;
    }

    [RelayCommand]
    private async Task CreateAsync()
    {
        if (!isSignedIn()) { LogNotSignedIn(); return; }
        IsBusy = true;
        try
        {
            log.Append($"--- Creating Cosmos account '{AccountName}' in resource group '{ResourceGroup}' ---");
            var result = await service.CreateAsync(ResourceGroup, Location, AccountName, log.Append);
            log.Append(result.Succeeded ? "Create finished." : $"Create failed (exit code {result.ExitCode}).");
        }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private async Task RestoreAsync()
    {
        if (!isSignedIn()) { LogNotSignedIn(); return; }
        if (string.IsNullOrWhiteSpace(DumpFolder))
        {
            log.Append("Pick an NDJSON dump folder (from a previous --export) before restoring.");
            return;
        }

        IsBusy = true;
        try
        {
            log.Append($"--- Restoring '{DumpFolder}' into Cosmos account '{AccountName}' ---");
            var result = await service.RestoreAsync(ResourceGroup, AccountName, DumpFolder, log.Append);
            log.Append(result.Succeeded ? "Restore finished." : $"Restore failed (exit code {result.ExitCode}).");
        }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private async Task DeleteAsync()
    {
        if (!isSignedIn()) { LogNotSignedIn(); return; }
        IsBusy = true;
        try
        {
            log.Append($"--- Deleting Cosmos account '{AccountName}' ---");
            var result = await service.DeleteAsync(ResourceGroup, AccountName, log.Append);
            log.Append(result.Succeeded ? "Delete finished." : $"Delete failed (exit code {result.ExitCode}).");
        }
        finally { IsBusy = false; }
    }
}
