using ACME.Frontend.WPF.UserControls.PriceList;
using ACME.Frontend.WPF.UserControls.ProductDetail;
using ACME.Frontend.WPF.UserControls.ProductGroupList;
using ACME.Frontend.WPF.UserControls.ProductList;
using ACME.Frontend.WPF.UserControls.ReviewDetail;
using ACME.Frontend.WPF.UserControls.ReviewList;
using ACME.Frontend.WPF.UserControls.SpecificationList;
using Microsoft.Extensions.DependencyInjection;

namespace ACME.Frontend.WPF.UserControls;

public static class ViewComponentExtensions
{
    public static void AddViewComponents(this ServiceCollection services)
    {
        services.AddTransient<ProductGroupListView>();
        services.AddTransient<ProductListView>();
        services.AddTransient<ProductDetailView>();
        services.AddTransient<PriceListView>();
        services.AddTransient<ReviewListView>();
        services.AddTransient<SpecificationListView>();
        services.AddTransient<ReviewDetailView>();
    }
}
