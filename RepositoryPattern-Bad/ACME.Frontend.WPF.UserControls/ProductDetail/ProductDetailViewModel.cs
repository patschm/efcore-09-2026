using ACME.DataLayer.Interfaces;
using ACME.Frontend.WPF.Core.Base;
using ACME.Frontend.WPF.Core.Interfaces;
using ACME.Frontend.WPF.UserControls.PriceList;
using ACME.Frontend.WPF.UserControls.ProductList;
using ACME.Frontend.WPF.UserControls.ReviewList;
using ACME.Frontend.WPF.UserControls.SpecificationList;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Threading.Tasks;
using System.Windows.Input;

namespace ACME.Frontend.WPF.UserControls.ProductDetail;

public class ProductDetailViewModel : BaseViewModel
{
	private ProductViewModel? _product;
	private readonly ReviewListViewModel _reviews;
	private readonly PriceListViewModel _prices;
	private readonly SpecificationListViewModel _specifications;
	private readonly IViewMediator _mediator;
	private readonly IUnitOfWork _uow;

	public ProductDetailViewModel(IViewMediator mediator, IUnitOfWork uow, ReviewListViewModel reviews, PriceListViewModel prices, SpecificationListViewModel specifications)
	{
		_mediator = mediator;
		_uow = uow;
		_reviews = reviews;
		_prices = prices;
		_specifications = specifications;
        BackCommand = new RelayCommand(NavigateBack);
    }

	public ICommand BackCommand { get; }

    public SpecificationListViewModel Specifications { get => _specifications; }
	public PriceListViewModel	Prices { 	get => _prices; }
	public ReviewListViewModel Reviews {	get => _reviews; }
	public ProductViewModel? Product
	{
		get { return _product; }
		set 
		{ 
			_product = value;
			ChangeProperty();
		}
	}

    public SpecificationListView? SpecificationView { get => new SpecificationListView(Specifications!); }
    public PriceListView? PriceView { get => new PriceListView(Prices!); }
    public ReviewListView? ReviewView { get =>new ReviewListView(Reviews!); }

    private void NavigateBack()
    {
		_mediator.Activate<ProductListView>();
    }
	public async Task LoadDataAsync()
	{
		Product = _mediator.DataBag.Product;
		await Specifications.LoadSpecificationsAsync();
	}

}
