using ACME.Frontend.WPF.Core.Interfaces;
using System.Windows.Controls;


namespace ACME.Frontend.WPF.UserControls.ProductGroupList;

/// <summary>
/// Interaction logic for ProductGroupListView.xaml
/// </summary>
public partial class ProductGroupListView : UserControl, IPageView
{
    private readonly ProductGroupListViewModel _model;

    public ProductGroupListView(ProductGroupListViewModel viewModel)
    {
        _model = viewModel;
        InitializeComponent();
        DataContext = _model;
    }

    private async void UserControl_Loaded(object sender, System.Windows.RoutedEventArgs e)
    {
        await _model.LoadProductGroupsAsync();
        
    }
}
