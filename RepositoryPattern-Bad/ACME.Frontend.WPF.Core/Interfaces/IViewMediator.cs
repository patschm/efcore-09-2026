namespace ACME.Frontend.WPF.Core.Interfaces;

public interface IViewMediator
{
    event EventHandler<IPageView>? Changed;
    dynamic DataBag { get; set; }
    void Activate<T>() where T: class, IPageView;
}
