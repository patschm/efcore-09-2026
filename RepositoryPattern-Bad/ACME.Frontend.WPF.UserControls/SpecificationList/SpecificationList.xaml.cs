using ACME.Frontend.WPF.Core.Interfaces;
using System.Windows.Controls;

namespace ACME.Frontend.WPF.UserControls.SpecificationList;

public partial class SpecificationListView : UserControl, IPageView
{
    private readonly SpecificationListViewModel _model;

    public SpecificationListView(SpecificationListViewModel model)
    {
        _model = model;
        InitializeComponent();
        DataContext = _model;
    }
}
