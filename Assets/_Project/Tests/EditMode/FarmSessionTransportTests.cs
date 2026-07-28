using NUnit.Framework;

namespace FarmPrototype.Farming.Tests
{
    public sealed class FarmSessionTransportTests
    {
        [Test]
        public void LoopbackIntent_IsSerializedBeforeHostDelivery()
        {
            var host = FarmLoopbackSessionTransport.CreateHost("host");
            var peer = host.AddPeer("peer-1");
            FarmSessionIntent received = null;
            host.ReceivedByHost += envelope => envelope.TryReadIntent(out received);

            var sent = FarmSessionIntent.Create(FarmSessionIntentKind.SleepReadiness, "peer-1", "ready=true");
            peer.SendIntentToHost(sent);

            Assert.That(received, Is.Not.Null);
            Assert.That(received.IntentId, Is.EqualTo(sent.IntentId));
            Assert.That(received, Is.Not.SameAs(sent));
            Assert.That(received.Payload, Is.EqualTo("ready=true"));
        }

        [Test]
        public void LoopbackHost_RejectsMoreThanThreePeers()
        {
            var host = FarmLoopbackSessionTransport.CreateHost();
            host.AddPeer("peer-1");
            host.AddPeer("peer-2");
            host.AddPeer("peer-3");

            Assert.Throws<System.InvalidOperationException>(() => host.AddPeer("peer-4"));
        }

        [Test]
        public void SessionEnvelope_RejectsProtocolMismatch()
        {
            var envelope = FarmSessionEnvelope.ForIntent("peer-1", FarmSessionIntent.Create(FarmSessionIntentKind.HotbarSelection, "peer-1", "2"));
            envelope.Protocol++;

            Assert.That(envelope.TryReadIntent(out _), Is.False);
        }
    }
}
