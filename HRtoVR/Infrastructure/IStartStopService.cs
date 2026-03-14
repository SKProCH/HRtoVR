using System.Threading.Tasks;

namespace HRtoVR.Infrastructure;

public interface IStartStopService {
    Task Start();
    Task Stop();
}