using ACME.Frontend.WPF.Core.Interfaces;
using ACME.Frontend.WPF.UserControls.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace ACME.Frontend.WPF.UserControls.ProductDetail
{
    /// <summary>
    /// Interaction logic for ProductDetailView.xaml
    /// </summary>
    public partial class ProductDetailView : UserControl, IPageView
    {
        private readonly ProductDetailViewModel _model;
        public ProductDetailView(ProductDetailViewModel model)
        {
            _model = model;
            InitializeComponent();
            DataContext = _model;
        }

        private async void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            await _model.LoadDataAsync();
        }

        private async void Specifications_GotFocus(object sender, RoutedEventArgs e)
        {
            await _model.Specifications.LoadSpecificationsAsync();
        }
        private async void Reviews_GotFocus(object sender, RoutedEventArgs e)
        {
            await _model.Reviews.LoadReviewsAsync();
        }
        private async void Prices_GotFocus(object sender, RoutedEventArgs e)
        {
            await _model.Prices.LoadPricesAsync();
        }
    }
}
