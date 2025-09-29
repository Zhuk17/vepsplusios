using System.Threading.Tasks;

namespace VEPS_Plus.Services
{
    public class DummyVpnService : IVpnService
    {
        public Task<bool> Connect(string config)
        {
            System.Diagnostics.Debug.WriteLine("DummyVpnService: Connect called.");
            return Task.FromResult(false);
        }

        public void Disconnect()
        {
            System.Diagnostics.Debug.WriteLine("DummyVpnService: Disconnect called.");
        }

        public bool IsVpnActive()
        {
            System.Diagnostics.Debug.WriteLine("DummyVpnService: IsVpnActive called.");
            return false;
        }
    }
}
