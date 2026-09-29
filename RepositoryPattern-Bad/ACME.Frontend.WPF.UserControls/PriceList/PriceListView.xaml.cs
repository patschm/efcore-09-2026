using ACME.Frontend.WPF.Core.Interfaces;
using System.Windows.Controls;

namespace ACME.Frontend.WPF.UserControls.PriceList
{
    public partial class PriceListView : UserControl, IPageView
    {
        private readonly PriceListViewModel _model;
        public PriceListView(PriceListViewModel model)
        {
            _model = model;
            InitializeComponent();
            DataContext = _model;
        }
    }
}
