using System;
using UnityEngine;

namespace FarmPrototype.Player
{
    /// <summary>
    /// Local ownership seam for a future Steam player-spawn adapter.
    /// Remote avatars keep their visual components but never consume this
    /// client's keyboard, camera target, or locally-selected equipment state.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class FarmPlayerOwnership : MonoBehaviour
    {
        [SerializeField] private string participantId = "local";
        [SerializeField] private bool isLocallyControlled = true;

        public event Action Changed;
        public string ParticipantId => participantId;
        public bool IsLocallyControlled => isLocallyControlled;

        public void Configure(string id, bool isLocal)
        {
            participantId = string.IsNullOrWhiteSpace(id) ? "local" : id.Trim();
            if (isLocallyControlled == isLocal) return;
            isLocallyControlled = isLocal;
            Changed?.Invoke();
        }
    }
}
