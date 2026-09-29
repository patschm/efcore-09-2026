using ACME.Frontend.WPF.Core.Interfaces;
using System.Windows.Controls;

namespace ACME.Frontend.WPF.UserControls.ReviewList;

public partial class ReviewListView : UserControl, IPageView
{
    private readonly ReviewListViewModel _model;
    public ReviewListView(ReviewListViewModel model)
    {
        _model = model;
        InitializeComponent();
        DataContext = _model;
    }
}
