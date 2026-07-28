using System;
using System.Collections.Generic;
using UnityEngine;

namespace FarmPrototype.Farming
{
    [Serializable]
    public sealed class FarmSleepSessionSnapshot
    {
        public List<string> Participants = new();
        public List<string> ReadyParticipants = new();

        public FarmSleepSessionSnapshot Clone() => new()
        {
            Participants = new List<string>(Participants ?? new List<string>()),
            ReadyParticipants = new List<string>(ReadyParticipants ?? new List<string>())
        };
    }

    /// <summary>
    /// Keeps sleep readiness separate from the action that advances the day.
    /// A future Steam adapter only needs to update participants and votes.
    /// </summary>
    public sealed class FarmSleepSession : MonoBehaviour
    {
        [SerializeField] private string localParticipantId = "local";
        private readonly HashSet<string> participants = new(StringComparer.Ordinal);
        private readonly HashSet<string> readyParticipants = new(StringComparer.Ordinal);

        public event Action Changed;
        public string LocalParticipantId => localParticipantId;
        public int ParticipantCount => participants.Count;
        public int ReadyCount => readyParticipants.Count;
        public bool IsLocalReady => readyParticipants.Contains(localParticipantId);
        public bool CanAdvanceDay => ParticipantCount > 0 && ReadyCount >= ParticipantCount;
        public string StatusText => CanAdvanceDay
            ? FarmLocalization.Get("sleep.all_ready", "Everyone is ready. The farm will wake up.")
            : FarmLocalization.Format("sleep.ready_count", "{0}/{1} player(s) ready to sleep.", ReadyCount, ParticipantCount);

        public void Initialize(string localId)
        {
            localParticipantId = string.IsNullOrWhiteSpace(localId) ? "local" : localId.Trim();
            ConfigureParticipants(new[] { localParticipantId }, localParticipantId);
        }

        public void ConfigureParticipants(IEnumerable<string> participantIds, string localId)
        {
            localParticipantId = string.IsNullOrWhiteSpace(localId) ? "local" : localId.Trim();
            participants.Clear();
            readyParticipants.Clear();
            if (participantIds != null)
                foreach (var id in participantIds)
                    if (!string.IsNullOrWhiteSpace(id)) participants.Add(id.Trim());
            participants.Add(localParticipantId);
            Changed?.Invoke();
        }

        public bool SetParticipantReady(string participantId, bool ready)
        {
            if (string.IsNullOrWhiteSpace(participantId)) return false;
            participantId = participantId.Trim();
            if (!participants.Contains(participantId)) return false;
            var changed = ready ? readyParticipants.Add(participantId) : readyParticipants.Remove(participantId);
            if (changed) Changed?.Invoke();
            return changed;
        }

        /// <summary>
        /// Adds a connected session member without clearing existing sleep votes.
        /// The host calls this only after the transport has authenticated the peer.
        /// </summary>
        public bool EnsureParticipant(string participantId)
        {
            if (string.IsNullOrWhiteSpace(participantId)) return false;
            var added = participants.Add(participantId.Trim());
            if (added) Changed?.Invoke();
            return added;
        }

        public bool SetLocalReady(bool ready) => SetParticipantReady(localParticipantId, ready);

        public FarmSleepSessionSnapshot CaptureSnapshot()
        {
            var snapshot = new FarmSleepSessionSnapshot();
            snapshot.Participants.AddRange(participants);
            snapshot.Participants.Sort(StringComparer.Ordinal);
            foreach (var participant in readyParticipants)
                if (participants.Contains(participant)) snapshot.ReadyParticipants.Add(participant);
            snapshot.ReadyParticipants.Sort(StringComparer.Ordinal);
            return snapshot;
        }

        /// <summary>Applies votes supplied by the host while keeping this client's local identity.</summary>
        public bool ApplySnapshot(FarmSleepSessionSnapshot snapshot)
        {
            if (snapshot == null) return false;
            participants.Clear();
            readyParticipants.Clear();
            foreach (var id in snapshot.Participants ?? new List<string>())
                if (!string.IsNullOrWhiteSpace(id)) participants.Add(id.Trim());
            participants.Add(localParticipantId);
            foreach (var id in snapshot.ReadyParticipants ?? new List<string>())
                if (!string.IsNullOrWhiteSpace(id) && participants.Contains(id.Trim())) readyParticipants.Add(id.Trim());
            Changed?.Invoke();
            return true;
        }

        public void ClearReadiness()
        {
            if (readyParticipants.Count == 0) return;
            readyParticipants.Clear();
            Changed?.Invoke();
        }

        public bool IsParticipantReady(string participantId) =>
            !string.IsNullOrWhiteSpace(participantId) && readyParticipants.Contains(participantId.Trim());
    }
}
