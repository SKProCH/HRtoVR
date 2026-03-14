using HRtoVR.Models;
using Material.Icons;

namespace HRtoVR.ViewModels;

public interface IPageViewModel {
    string Title { get; }
    MaterialIconKind Icon { get; }
    ConnectionState? State { get; }
}