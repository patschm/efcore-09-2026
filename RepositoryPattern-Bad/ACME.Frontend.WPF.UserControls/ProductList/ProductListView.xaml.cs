using ACME.Frontend.WPF.Core.Interfaces;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;

namespace ACME.Frontend.WPF.UserControls.ProductList
{
    /// <summary>
    /// Interaction logic for ProductListView.xaml
    /// </summary>
    public partial class ProductListView : UserControl, IPageView
    {
        private readonly ProductListViewModel _model;

        public ProductListView(ProductListViewModel model)
        {
            _model = model; 
            InitializeComponent();
            DataContext = _model;
        }

        private async void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            await _model.LoadProductsAsync();
        }

        private async void ListView_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            if (e.VerticalOffset + e.ViewportHeight >= 20)
            {
                Debug.WriteLine(e.VerticalOffset + e.ViewportHeight);
                await _model.NextPageAsync();
            }
            if (e.VerticalOffset - e.ViewportHeight < 0)
            {
                Debug.WriteLine(e.VerticalOffset - e.ViewportHeight);
                await _model.PreviousPageAsync();
            }
        }
    }
}
