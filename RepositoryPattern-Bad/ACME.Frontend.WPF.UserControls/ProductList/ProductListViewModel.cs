using ACME.DataLayer.Interfaces;
using ACME.Frontend.WPF.Core.Base;
using ACME.Frontend.WPF.Core.Interfaces;
using ACME.Frontend.WPF.UserControls.ProductDetail;
using ACME.Frontend.WPF.UserControls.ProductGroupList;
using ACME.Frontend.WPF.UserControls.ViewModels;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;

namespace ACME.Frontend.WPF.UserControls.ProductList;

public class ProductListViewModel : BaseViewModel
{
    private readonly IUnitOfWork _repositories;
    private readonly IViewMediator _viewMediator;
    private ProductGroupViewModel? _productGroup;

    public ProductListViewModel(IUnitOfWork repositories, IViewMediator viewMediator)
    {
        _repositories = repositories;
        _viewMediator = viewMediator;
        SelectCommand = new RelayCommand<ProductViewModel>(SelectItem);
        BackCommand = new RelayCommand(NavigateBack);
    }

    public ICommand BackCommand { get; }
    public ICommand SelectCommand { get; }
    public ObservableCollection<ProductViewModel> Products { get; private set; } = new ObservableCollection<ProductViewModel>();
    public int Page { get; set; } = 1;
    public int Count { get; set; } = 20;
    public ProductGroupViewModel? ProductGroup 
    { 
        get { return _productGroup; }
        set 
        {
            _productGroup = value;
            ChangeProperty();
        }
    }

    private void NavigateBack()
    {
        _viewMediator.Activate<ProductGroupListView>();
    }
    private void SelectItem(ProductViewModel? product)
    {
        if (product == null) return;
        _viewMediator.DataBag.Product = product;
        _viewMediator.Activate<ProductDetailView>();
    }
    public async Task<int> LoadProductsAsync()
    {
        var counter = 0;
        ProductGroup = _viewMediator.DataBag.ProductGroup;
        var repository = _repositories.ProductGroups;
        var reviews = await repository.GetProductsAsync(ProductGroup.Id, Page, Count);
        foreach (var item in reviews.Select(p => new ProductViewModel { Id = p.Id, Brand = p.Brand.Name, Image=p.Image, Name = p.Name}))
        {
            Products.Add(item);
            counter++;
        }
        return counter;
    }

    public async Task NextPageAsync()
    {
        if (Products.Count % Count > 0) return;
        Page++;
        var nr = await LoadProductsAsync();
    }

    internal async Task PreviousPageAsync()
    {
        if (Page == 1) return;
        Page--;
        var nr = await LoadProductsAsync();
    }
}
