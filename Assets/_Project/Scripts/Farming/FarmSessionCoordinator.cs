using System.Threading.Tasks;
using UnityEngine;

namespace FarmPrototype.Farming
{
    /// <summary>
    /// Binds gameplay to a transport without letting either side know whether the
    /// session is loopback, Steam P2P, or another future implementation.
    /// </summary>
    public sealed class FarmSessionCoordinator : MonoBehaviour
    {
        private FarmTestPlot plot;
        private FarmHostIntentRouter hostRouter;
        private IFarmSessionTransport transport;

        public bool IsBound => transport != null && transport.IsConnected;

        private void Awake()
        {
            plot = GetComponent<FarmTestPlot>();
            hostRouter = new FarmHostIntentRouter(plot);
        }

        private void OnDestroy() => Unbind();

        public void Bind(IFarmSessionTransport sessionTransport)
        {
            Unbind();
            transport = sessionTransport;
            if (transport == null || plot == null) return;
            if (FarmSessionTime.IsSimulationAuthority)
            {
                transport.ReceivedByHost += HandleIntentAtHost;
                plot.WorldSnapshotReady += transport.BroadcastSnapshot;
            }
            else
            {
                FarmSessionIntentBus.Requested += transport.SendIntentToHost;
                transport.ReceivedByPeer += HandleSnapshotAtPeer;
            }
        }

        public void Unbind()
        {
            if (transport == null || plot == null)
            {
                transport = null;
                return;
            }
            transport.ReceivedByHost -= HandleIntentAtHost;
            transport.ReceivedByPeer -= HandleSnapshotAtPeer;
            plot.WorldSnapshotReady -= transport.BroadcastSnapshot;
            FarmSessionIntentBus.Requested -= transport.SendIntentToHost;
            transport = null;
        }

        private async void HandleIntentAtHost(FarmSessionEnvelope envelope)
        {
            if (!envelope.TryReadIntent(out var intent)) return;
            if (string.IsNullOrWhiteSpace(envelope.SenderId) ||
                !string.Equals(intent.RequestedBy, envelope.SenderId, System.StringComparison.Ordinal))
            {
                Debug.LogWarning("Co-op intent rejected: sender identity did not match its payload.");
                return;
            }
            var result = await hostRouter.ExecuteAsync(intent);
            if (!result.Succeeded) Debug.LogWarning($"Co-op intent rejected: {result.Message}");
        }

        private void HandleSnapshotAtPeer(FarmSessionEnvelope envelope)
        {
            if (envelope.TryReadSnapshot(out var snapshot)) plot.ApplyWorldSessionSnapshot(snapshot);
        }
    }
}
