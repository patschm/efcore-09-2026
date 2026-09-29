using ACME.DataLayer.Interfaces;
using ACME.Frontend.WPF.Core.Base;
using ACME.Frontend.WPF.Core.Interfaces;
using ACME.Frontend.WPF.UserControls.ProductList;
using ACME.Frontend.WPF.UserControls.ViewModels;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace ACME.Frontend.WPF.UserControls.PriceList;

public class PriceListViewModel : BaseViewModel
{
    private readonly IUnitOfWork _repositories;
    private readonly IViewMediator _viewMediator;

    public PriceListViewModel(IUnitOfWork repositories, IViewMediator viewMediator)
    {
        _repositories = repositories;
        _viewMediator = viewMediator;
    }
    public ObservableCollection<PriceViewModel> Prices { get; private set; } = new ObservableCollection<PriceViewModel>();
    public async Task LoadPricesAsync()
    {
        var repository = _repositories.Products;
        ProductViewModel product = _viewMediator.DataBag.Product;
        Prices.Clear();
        var prices = await repository.GetPricesAsync(product.Id);
        foreach (var item in prices.Select(pr => new PriceViewModel { BasePrice = pr.BasePrice, PriceDate=pr.PriceDate, ShopName=pr.ShopName }))
        {
            Prices.Add(item);
        }
    }
}
