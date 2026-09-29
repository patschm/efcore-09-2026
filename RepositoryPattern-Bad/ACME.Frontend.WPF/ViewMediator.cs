using ACME.Frontend.WPF.Core.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Dynamic;

namespace ACME.Frontend.WPF;

public class ViewMediator : IViewMediator
{
    private readonly IServiceProvider _serviceProvider;

    public ViewMediator(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public dynamic DataBag { get; set; } = new ExpandoObject();

    public event EventHandler<IPageView>? Changed;

    public void Activate<T>() where T : class, IPageView
    {
        var view = _serviceProvider.GetRequiredService<T>();
        Changed?.Invoke(this, view);
    }
    
}
