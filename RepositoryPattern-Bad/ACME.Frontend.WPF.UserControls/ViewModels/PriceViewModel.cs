using System;

namespace ACME.Frontend.WPF.UserControls.ViewModels;

public class PriceViewModel
{
    public double BasePrice { get; set; }
    public string? ShopName { get; set; }
    public DateTime PriceDate { get; set; }
}
