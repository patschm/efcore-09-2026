using System.Collections.Specialized;
using System.Windows;
using WebShop.Tools.OpsConsole.ViewModels;

namespace WebShop.Tools.OpsConsole;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        Loaded += (_, _) =>
        {
            if (DataContext is MainViewModel vm)
                vm.Log.Lines.CollectionChanged += OnLogLinesChanged;
        };
    }

    private void OnLogLinesChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (LogListBox.Items.Count > 0)
            LogListBox.ScrollIntoView(LogListBox.Items[^1]);
    }
}
