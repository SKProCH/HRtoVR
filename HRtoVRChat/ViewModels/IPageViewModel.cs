using Material.Icons;

namespace HRtoVRChat.ViewModels;

public interface IPageViewModel
{
    string Title { get; }
    MaterialIconKind Icon { get; }
}
