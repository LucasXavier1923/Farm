using System;
using UnityEngine;

namespace FarmPrototype.Farming
{
    /// <summary>
    /// Transport-agnostic world payload for the future Steam host to send to peers.
    /// It deliberately contains only shared farm state; camera settings and other
    /// local presentation preferences never belong in this payload.
    /// </summary>
    [Serializable]
    public sealed class FarmWorldSessionSnapshot
    {
        public const int ProtocolVersion = 1;

        public int Protocol = ProtocolVersion;
        public int Revision;
        public float HostTimeSeconds;
        public FarmSaveData Farm;
        public FarmSleepSessionSnapshot Sleep;

        public bool IsValid => Protocol == ProtocolVersion && Revision > 0 && Farm != null;

        public static FarmWorldSessionSnapshot Create(
            int revision,
            float hostTimeSeconds,
            FarmSaveData farm,
            FarmSleepSessionSnapshot sleep = null)
        {
            return new FarmWorldSessionSnapshot
            {
                Protocol = ProtocolVersion,
                Revision = Mathf.Max(1, revision),
                HostTimeSeconds = Mathf.Max(0f, hostTimeSeconds),
                Farm = CloneFarmData(farm),
                Sleep = sleep?.Clone()
            };
        }

        public FarmSaveData CreateIndependentFarmCopy() => CloneFarmData(Farm);
        public FarmSleepSessionSnapshot CreateIndependentSleepCopy() => Sleep?.Clone();

        private static FarmSaveData CloneFarmData(FarmSaveData source)
        {
            if (source == null) return null;
            var json = JsonUtility.ToJson(source);
            return string.IsNullOrWhiteSpace(json) ? null : JsonUtility.FromJson<FarmSaveData>(json);
        }
    }
}
