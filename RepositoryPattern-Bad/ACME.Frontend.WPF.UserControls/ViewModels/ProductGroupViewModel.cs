using ACME.Frontend.WPF.Core.Base;
using System;

namespace ACME.Frontend.WPF.UserControls.ViewModels;

public class ProductGroupViewModel : BaseViewModel
{
    private string? _name;
    private string? _image;
    public long Id { get; set; }
    public string? Name 
    { 
        get
        {
            return _name;
        }
        set
        {
            _name = value;
            ChangeProperty();
        }
    }
    public string? Image
    {
        get
        {
            return $"https://angular-training.azureedge.net/{_image}";
        }
        set
        {
            _image = value;
            ChangeProperty();
        }
    }
}
