using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WebShop.Tools.OpsConsole.Models;
using WebShop.Tools.OpsConsole.Services;

namespace WebShop.Tools.OpsConsole.ViewModels;

public sealed partial class MainViewModel : ObservableObject
{
    private readonly AzureAuthService _authService;

    public OperationLog Log { get; } = new();

    [ObservableProperty]
    private AzureAccountInfo? _currentAccount;

    [ObservableProperty]
    private bool _isCheckingAuth = true;

    [ObservableProperty]
    private bool _isSigningIn;

    public bool IsSignedIn => CurrentAccount is not null;
    public bool ShowNotSignedInWarning => !IsCheckingAuth && !IsSignedIn;

    public ObservableCollection<AzureSubscriptionInfo> Subscriptions { get; } = [];

    [ObservableProperty]
    private AzureSubscriptionInfo? _selectedSubscription;

    // Shared across all four tabs (same ObservableCollection instance, passed by reference) so a
    // single refresh - on sign-in or on switching subscription - updates every tab's dropdown at
    // once. Editable rather than a locked-down picker: Create needs to accept a not-yet-existing
    // name too, this list is only ever a convenience for the common "pick something that already
    // exists" case (Restore/Delete/Setup & Deploy).
    public ObservableCollection<string> ResourceGroups { get; } = [];

    public CosmosViewModel Cosmos { get; }
    public PostgresViewModel Postgres { get; }
    public AksViewModel Aks { get; }
    public AcaViewModel Aca { get; }

    public MainViewModel()
    {
        _authService = new AzureAuthService();

        Cosmos = new CosmosViewModel(new CosmosOpsService(), Log, () => IsSignedIn, ResourceGroups);
        Postgres = new PostgresViewModel(new PostgresOpsService(), Log, () => IsSignedIn, ResourceGroups);
        Aks = new AksViewModel(new AksOpsService(), Log, () => IsSignedIn, ResourceGroups);
        Aca = new AcaViewModel(new AcaOpsService(), Log, () => IsSignedIn, ResourceGroups);

        // Fire-and-forget, but caught - an unobserved exception here (e.g. from a JSON parse
        // failure that somehow still got through) would otherwise vanish silently instead of
        // ever reaching the log, exactly the kind of "nothing visibly wrong, feature just doesn't
        // work" bug that hit the subscription dropdown (see ProcessRunner's own comment on the
        // root cause).
        _ = RefreshAuthStateAsync().ContinueWith(
            t => Log.Append($"Startup sign-in check failed: {t.Exception?.GetBaseException().Message}"),
            TaskContinuationOptions.OnlyOnFaulted);
    }

    partial void OnCurrentAccountChanged(AzureAccountInfo? value)
    {
        OnPropertyChanged(nameof(IsSignedIn));
        OnPropertyChanged(nameof(ShowNotSignedInWarning));
    }

    partial void OnIsCheckingAuthChanged(bool value) => OnPropertyChanged(nameof(ShowNotSignedInWarning));

    [RelayCommand]
    private async Task RefreshAuthStateAsync()
    {
        IsCheckingAuth = true;
        try
        {
            CurrentAccount = await _authService.GetCurrentAccountAsync(Log.Append);
            if (CurrentAccount is not null)
            {
                await RefreshSubscriptionsAsync();
                await RefreshResourceGroupsAsync();
            }
        }
        finally { IsCheckingAuth = false; }
    }

    private async Task RefreshResourceGroupsAsync()
    {
        ResourceGroups.Clear();
        foreach (var name in await AzureLookups.ListResourceGroupsAsync(Log.Append))
            ResourceGroups.Add(name);
    }

    [RelayCommand]
    private async Task LoginAsync()
    {
        IsSigningIn = true;
        Log.Append("--- Signing in (az login) - a browser window will open ---");
        try
        {
            var result = await _authService.LoginAsync(Log.Append);
            if (result.Succeeded)
            {
                Log.Append("Signed in.");
                await RefreshAuthStateAsync();
            }
            else
            {
                Log.Append($"Sign-in failed (exit code {result.ExitCode}).");
            }
        }
        finally { IsSigningIn = false; }
    }

    [RelayCommand]
    private async Task LogoutAsync()
    {
        Log.Append("--- Signing out ---");
        await _authService.LogoutAsync(Log.Append);
        CurrentAccount = null;
        Subscriptions.Clear();
        ResourceGroups.Clear();
        Log.Append("Signed out.");
    }

    private async Task RefreshSubscriptionsAsync()
    {
        Subscriptions.Clear();
        var subscriptions = await _authService.ListSubscriptionsAsync(Log.Append);
        if (subscriptions.Count == 0)
            Log.Append("No subscriptions came back from 'az account list' - see any message above, or run it yourself in a terminal to check.");

        foreach (var sub in subscriptions)
            Subscriptions.Add(sub);
        SelectedSubscription = Subscriptions.FirstOrDefault(s => s.IsDefault) ?? Subscriptions.FirstOrDefault();
    }

    [RelayCommand]
    private async Task UseSelectedSubscriptionAsync()
    {
        if (SelectedSubscription is null)
            return;

        Log.Append($"--- Switching to subscription '{SelectedSubscription.Name}' ---");
        var result = await _authService.SetActiveSubscriptionAsync(SelectedSubscription.Id, Log.Append);
        if (result.Succeeded)
            await RefreshAuthStateAsync();
    }
}
