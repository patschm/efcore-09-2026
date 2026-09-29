
using ACME.Frontend.WPF.UserControls.ViewModels;
using System.Windows;
using System.Windows.Controls;

namespace ACME.Frontend.WPF;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow(MainViewModel model)
    {
        InitializeComponent();
        DataContext = model;
    }

}
