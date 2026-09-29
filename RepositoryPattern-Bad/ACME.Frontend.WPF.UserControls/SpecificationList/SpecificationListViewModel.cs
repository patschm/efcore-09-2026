using ACME.DataLayer.Entities;
using ACME.DataLayer.Interfaces;
using ACME.Frontend.WPF.Core.Base;
using ACME.Frontend.WPF.Core.Interfaces;
using ACME.Frontend.WPF.UserControls.ProductList;
using ACME.Frontend.WPF.UserControls.ViewModels;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace ACME.Frontend.WPF.UserControls.SpecificationList;

public class SpecificationListViewModel : BaseViewModel
{
    private readonly IUnitOfWork _repositories;
    private readonly IViewMediator _viewMediator;

    public SpecificationListViewModel(IUnitOfWork repositories, IViewMediator viewMediator)
    {
        _repositories = repositories;
        _viewMediator = viewMediator;
    }
    public ObservableCollection<SpecificationViewModel> Specifications { get; private set; } = new ObservableCollection<SpecificationViewModel>();
    public async Task LoadSpecificationsAsync()
    {
        ProductViewModel product = _viewMediator.DataBag.Product;
        ProductGroupViewModel productGroup = _viewMediator.DataBag.ProductGroup;
        var defs = await _repositories.ProductGroups.GetSpecificationDefinitionsAsync(productGroup.Id);
        var specs = await _repositories.Products.GetSpecificationsAsync(product.Id);
        var specifications = specs.Join(defs, sp => sp.Key, sd => sd.Key, ConstructSpecification);

        foreach (var item in specifications)
        {
            Specifications.Add(item);
        }
    }

    private static SpecificationViewModel ConstructSpecification(Specification sp, SpecificationDefinition sd)
    {
        var spec = new SpecificationViewModel
        {
            Label = sd.Name,
            Info = sd.Description
        };
        if (sp.NumberValue != null)
        {
            spec.Value = $"{sp.NumberValue} {sd.Unit}";
        }
        if (sp.BoolValue != null)
        {
            spec.Value = $"{sp.BoolValue}";
        }
        if (sp.StringValue != null)
        {
            spec.Value = $"{sp.StringValue} {sd.Unit}";
        }
        return spec;
    }
}
