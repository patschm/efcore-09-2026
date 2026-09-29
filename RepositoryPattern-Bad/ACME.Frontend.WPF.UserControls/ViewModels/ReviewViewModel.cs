using ACME.Frontend.WPF.Core.Base;

namespace ACME.Frontend.WPF.UserControls.ViewModels;

public class ReviewViewModel : BaseViewModel
{
    private string? _author;
    private string? _text;
    private string? _email;
    private byte _score;

    public string? Author { get => _author; set { _author = value; ChangeProperty(); } }
    public string? Text { get => _text; set { _text = value; ChangeProperty(); } }
    public byte Score { get => _score; set { _score = value; ChangeProperty(); } }
    public string? Email { get => _email; set { _email = value; ChangeProperty(); } }
}
