using ACME.DataLayer.Interfaces;
using ACME.Frontend.WPF.Core.Base;
using ACME.Frontend.WPF.Core.Interfaces;
using ACME.Frontend.WPF.UserControls.ProductList;
using ACME.Frontend.WPF.UserControls.ReviewDetail;
using ACME.Frontend.WPF.UserControls.ViewModels;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;

namespace ACME.Frontend.WPF.UserControls.ReviewList;

public class ReviewListViewModel : BaseViewModel
{
    private readonly IUnitOfWork _repositories;
    private readonly IViewMediator _viewMediator;

    public ReviewListViewModel(IUnitOfWork repositories, IViewMediator viewMediator)
    {
        _repositories = repositories;
        _viewMediator = viewMediator;
        NewReview = new RelayCommand(ReviewEditor);
    }

   public ObservableCollection<ReviewViewModel> Reviews { get; private set; } = new ObservableCollection<ReviewViewModel>();
   public ICommand NewReview { get; }
    private void ReviewEditor()
    {
        _viewMediator.Activate<ReviewDetailView>();
    }


    public async Task LoadReviewsAsync()
    {
        var repository = _repositories.Products;
        ProductViewModel product = _viewMediator.DataBag.Product;
        Reviews.Clear();
        var review = await repository.GetReviewsAsync(product.Id);
        foreach (var item in review.Select(pr => new ReviewViewModel { Author=pr.Reviewer?.Name, Email=pr.Reviewer?.Email, Score=pr.Score, Text=pr.Text }))
        {
            Reviews.Add(item);
        }
    }
}
