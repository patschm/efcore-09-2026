using ACME.Frontend.WPF.Core.Base;
using ACME.Frontend.WPF.UserControls.ViewModels;

namespace ACME.Frontend.WPF.UserControls.ProductList;

public class ProductViewModel: BaseViewModel
{
    private string? _image;
    private string? _name;
    private string? _brand;

    public long Id { get; internal set; }
    public string? Name 
    { 
        get { return _name; }
        set
        {
            _name = value;
            ChangeProperty();
        }
    }
    public string? Brand 
    { 
        get { return _brand; }
        set
        {
            _brand = value;
            ChangeProperty();
        }
    }
    public string FullName { get => $"{Brand} {Name}"; }
    public string? Image
    {
        get
        {
            return $"https://angular-training.azureedge.net/{_image}";
        }
        set
        {
            _image = value;
        }
    }
}
