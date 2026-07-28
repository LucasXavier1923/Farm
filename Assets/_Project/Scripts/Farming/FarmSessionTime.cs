using System;
using UnityEngine;

namespace FarmPrototype.Farming
{
    public enum FarmSessionRole
    {
        Solo,
        Host,
        Peer
    }

    /// <summary>
    /// Session-world time abstraction. The local fallback is unscaled Unity time,
    /// so individual menus never pause the farm. A future Steam session adapter can
    /// install a host-synchronized clock and disable simulation authority on peers.
    /// </summary>
    public static class FarmSessionTime
    {
        private static Func<float> nowProvider;
        private static Func<float> deltaTimeProvider;

        /// <summary>Only the host (or solo session) may advance deterministic world simulation.</summary>
        public static bool IsSimulationAuthority { get; private set; } = true;
        public static bool UsesExternalClock => nowProvider != null;
        public static FarmSessionRole Role => !UsesExternalClock
            ? FarmSessionRole.Solo
            : IsSimulationAuthority ? FarmSessionRole.Host : FarmSessionRole.Peer;
        public static float Now => Read(nowProvider, Time.unscaledTime);
        public static float DeltaTime => Mathf.Max(0f, Read(deltaTimeProvider, Time.unscaledDeltaTime));

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetForNewRuntimeSession() => RestoreLocalSoloDefaults();

        /// <summary>
        /// Configures the time source supplied by a future network session. The provider
        /// must be monotonic and use seconds. Passing null restores the local fallback.
        /// </summary>
        public static void Configure(Func<float> sessionNow, Func<float> sessionDeltaTime, bool isSimulationAuthority)
        {
            if (sessionNow == null || sessionDeltaTime == null)
            {
                RestoreLocalSoloDefaults();
                return;
            }
            nowProvider = sessionNow;
            deltaTimeProvider = sessionDeltaTime;
            IsSimulationAuthority = isSimulationAuthority;
        }

        public static void RestoreLocalSoloDefaults()
        {
            nowProvider = null;
            deltaTimeProvider = null;
            IsSimulationAuthority = true;
        }

        private static float Read(Func<float> provider, float fallback)
        {
            if (provider == null) return fallback;
            try
            {
                var value = provider();
                return float.IsFinite(value) ? value : fallback;
            }
            catch
            {
                return fallback;
            }
        }
    }
}
