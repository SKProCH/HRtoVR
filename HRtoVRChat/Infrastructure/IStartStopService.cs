using System.Threading.Tasks;
namespace HRtoVRChat.Infrastructure;

public interface IStartStopService {
    Task Start();
    Task Stop();
}