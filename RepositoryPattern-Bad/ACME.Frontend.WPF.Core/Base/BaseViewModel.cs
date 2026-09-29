using ACME.Frontend.WPF.Core.Interfaces;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ACME.Frontend.WPF.Core.Base;

public class BaseViewModel : INotifyPropertyChanged, IPageView
{
    public event PropertyChangedEventHandler? PropertyChanged;

    protected void ChangeProperty([CallerMemberName]string? propertyName=null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
