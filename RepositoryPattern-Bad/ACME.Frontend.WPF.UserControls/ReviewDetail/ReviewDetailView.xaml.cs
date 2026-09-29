using ACME.Frontend.WPF.Core.Interfaces;
using System.Windows;
using System.Windows.Controls;


namespace ACME.Frontend.WPF.UserControls.ReviewDetail
{
    /// <summary>
    /// Interaction logic for ReviewDetailView.xaml
    /// </summary>
    public partial class ReviewDetailView : UserControl, IPageView
    {
        private ReviewDetailViewModel _model;
        public ReviewDetailView(ReviewDetailViewModel model)
        {
            _model = model;
            InitializeComponent();
            DataContext = _model;
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            _model.LoadData();
        }
    }
}
