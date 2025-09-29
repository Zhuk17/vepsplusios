using System.Threading.Tasks;

namespace VEPS_Plus.Services
{
    public interface IVpnService
    {
        Task<bool> Connect(string config);
        void Disconnect();
        bool IsVpnActive();
    }
}
