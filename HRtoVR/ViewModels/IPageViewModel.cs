using Material.Icons;

namespace HRtoVRChat.ViewModels;

using HRtoVRChat.Models;

public interface IPageViewModel
{
    string Title { get; }
    MaterialIconKind Icon { get; }
    ConnectionState? State { get; }
}
