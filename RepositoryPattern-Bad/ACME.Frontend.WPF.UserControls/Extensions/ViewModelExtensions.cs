using ACME.Frontend.WPF.UserControls.PriceList;
using ACME.Frontend.WPF.UserControls.ProductDetail;
using ACME.Frontend.WPF.UserControls.ProductGroupList;
using ACME.Frontend.WPF.UserControls.ProductList;
using ACME.Frontend.WPF.UserControls.ReviewDetail;
using ACME.Frontend.WPF.UserControls.ReviewList;
using ACME.Frontend.WPF.UserControls.SpecificationList;
using Microsoft.Extensions.DependencyInjection;

namespace ACME.Frontend.WPF.UserControls;

public static class ViewModelExtensions
{
    public static void AddViewModels(this ServiceCollection services)
    {
        services.AddTransient<ProductGroupListViewModel>();
        services.AddTransient<ProductListViewModel>();
        services.AddTransient<ProductDetailViewModel>();
        services.AddTransient<PriceListViewModel>();
        services.AddTransient<ReviewListViewModel>();
        services.AddTransient<SpecificationListViewModel>();
        services.AddTransient<ReviewDetailViewModel>();
    }
}
