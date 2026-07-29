using NUnit.Framework;
using UnityEngine;

namespace FarmPrototype.Farming.Tests
{
    public sealed class FarmHotbarLocalStateTests
    {
        [Test]
        public void LocalInventoryItem_AssignsSwapsAndClearsFromHotbar()
        {
            var owner = new GameObject("FarmHotbarLocalStateTests");
            try
            {
                var state = owner.AddComponent<FarmGameState>();
                Assert.That(state.AddItem("pumpkin", 2), Is.True);
                Assert.That(state.AssignHotbarSlot(6, FarmGameState.ItemPrefix + "pumpkin"), Is.True);
                Assert.That(state.GetHotbarEntry(6), Is.EqualTo(FarmGameState.ItemPrefix + "pumpkin"));

                Assert.That(state.SwapHotbarSlots(6, 7), Is.True);
                Assert.That(state.GetHotbarEntry(7), Is.EqualTo(FarmGameState.ItemPrefix + "pumpkin"));

                Assert.That(state.TryRemoveItem("pumpkin", 2), Is.True);
                Assert.That(state.GetHotbarEntry(7), Is.Empty);
            }
            finally
            {
                Object.DestroyImmediate(owner);
            }
        }
    }
}
