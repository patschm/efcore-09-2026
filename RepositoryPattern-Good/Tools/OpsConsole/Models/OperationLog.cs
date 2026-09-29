using System.Collections.ObjectModel;
using System.Windows;

namespace WebShop.Tools.OpsConsole.Models;

// Shared across every tab - one running log for the whole app, since a user watching a long AKS
// deploy shouldn't have to guess which tab's own log panel has the output. Process output events
// fire on a thread-pool thread (see ProcessRunner), so every append marshals to the UI thread
// before touching the ObservableCollection WPF's binding system reads from.
public sealed class OperationLog
{
    public ObservableCollection<string> Lines { get; } = [];

    public void Append(string line)
    {
        var dispatcher = Application.Current?.Dispatcher;
        if (dispatcher is null || dispatcher.CheckAccess())
            Lines.Add(line);
        else
            dispatcher.Invoke(() => Lines.Add(line));
    }
}
