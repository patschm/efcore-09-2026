using ACME.Frontend.WPF.Core.Base;
using ACME.Frontend.WPF.Core.Interfaces;
using ACME.Frontend.WPF.UserControls.ProductGroupList;

namespace ACME.Frontend.WPF;

public class MainViewModel : BaseViewModel
{
	private IPageView? _currentView;
	private readonly IViewMediator _viewMediator;

	public MainViewModel(IViewMediator viewMediator)
	{
		_viewMediator = viewMediator;
		_viewMediator.Changed += (o, v) => CurrentView = v;
		_viewMediator.Activate<ProductGroupListView>();
	}

	public IPageView? CurrentView
	{
		get { return _currentView; }
		set 
		{ 
			_currentView = value;
			ChangeProperty();
		}
	}
}
