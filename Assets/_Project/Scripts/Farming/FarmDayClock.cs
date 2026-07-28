using UnityEngine;

namespace FarmPrototype.Farming
{
    public enum FarmDayPhase { Night, Dawn, Morning, Afternoon, Dusk }
    public enum FarmSeason { Spring, Summer, Autumn, Winter }

    public sealed class FarmDayClock : MonoBehaviour
    {
        public const int DaysPerSeason = 7;
        public const int SeasonsPerYear = 4;
        public const int DaysPerYear = DaysPerSeason * SeasonsPerYear;
        [SerializeField, Min(60f)] private float realSecondsPerGameDay = 600f;
        [SerializeField, Min(0f)] private float simulationSpeed = 1f;
        [SerializeField, Min(5f)] private float saveCheckpointRealSeconds = 30f;

        private FarmTestPlot plot;
        private FarmGameState state;
        private Light sun;
        private Quaternion originalSunRotation;
        private Color originalSunColor;
        private float originalSunIntensity;
        private float originalAmbientIntensity;
        private UnityEngine.Rendering.AmbientMode originalAmbientMode;
        private Color originalAmbientSkyColor;
        private Color originalAmbientEquatorColor;
        private Color originalAmbientGroundColor;
        private bool originalFog;
        private float checkpointElapsed;
        private float weatherLightMultiplier = 1f;
        private bool lightingCaptured;
        private FarmDayPhase lastPickupPhase;

        public int DayNumber => state != null ? state.DayNumber : 1;
        public float MinutesOfDay => state != null ? state.MinutesOfDay : 480f;
        public float SimulationSpeed => simulationSpeed;
        public float RealSecondsPerGameDay => realSecondsPerGameDay;
        public float GameMinutesPerRealSecond => 1440f / Mathf.Max(60f, realSecondsPerGameDay);
        public FarmDayPhase Phase => ResolvePhase(MinutesOfDay);
        public int YearNumber => YearForDay(DayNumber);
        public FarmSeason CurrentSeason => SeasonForDay(DayNumber);
        public int DayOfSeason => DayInSeason(DayNumber);
        public string SeasonDisplayText => $"{SeasonName(CurrentSeason)} {DayOfSeason}/{DaysPerSeason}";
        public string CalendarText => FarmLocalization.Format("clock.calendar", "YEAR {0}  \u2022  {1}", YearNumber, SeasonDisplayText);
        public float CurrentLightFactor { get; private set; }
        public float WeatherLightMultiplier => weatherLightMultiplier;
        public string DisplayText => FarmLocalization.Format("clock.display", "DAY {0}   {1}   {2}", DayNumber, FormatTime(MinutesOfDay), PhaseName(Phase));

        public void Initialize(FarmTestPlot owner, FarmGameState gameState)
        {
            plot = owner;
            state = gameState;
            CaptureLighting();
            lastPickupPhase = Phase;
            ApplyLighting();
        }

        private void Update()
        {
            if (state == null) return;
            if (FarmSessionTime.IsSimulationAuthority && simulationSpeed > 0f)
            {
                var previousDay = state.DayNumber;
                var previousMinute = state.MinutesOfDay;
                var gameMinutesPerSecond = 1440f / Mathf.Max(60f, realSecondsPerGameDay);
                var dayChanged = state.AdvanceClock(gameMinutesPerSecond * simulationSpeed * FarmSessionTime.DeltaTime);
                if (CrossedMorning(previousDay, previousMinute, state.DayNumber, state.MinutesOfDay))
                    plot?.NotifyMorningStarted();
                RefreshRouteWindow();
                checkpointElapsed += FarmSessionTime.DeltaTime;
                if (dayChanged || checkpointElapsed >= saveCheckpointRealSeconds)
                {
                    checkpointElapsed = 0f;
                    plot?.NotifyClockCheckpoint();
                }
            }
            ApplyLighting();
        }

        public float RealSecondsForGameMinutes(float gameMinutes) =>
            Mathf.Max(0f, gameMinutes) / GameMinutesPerRealSecond;

        public void SetClock(int day, float minute)
        {
            state?.SetClock(day, minute);
            RefreshRouteWindow();
            ApplyLighting();
        }

        public void SetClockForTesting(int day, float minute) => SetClock(day, minute);

        public void AdvanceMinutes(float minutes)
        {
            if (state == null) return;
            var previousDay = state.DayNumber;
            var previousMinute = state.MinutesOfDay;
            state.AdvanceClock(minutes);
            if (CrossedMorning(previousDay, previousMinute, state.DayNumber, state.MinutesOfDay))
                plot?.NotifyMorningStarted();
            RefreshRouteWindow();
            ApplyLighting();
            plot?.NotifyClockCheckpoint();
        }

        private static bool CrossedMorning(int previousDay, float previousMinute, int currentDay, float currentMinute)
        {
            const float morningMinute = 360f;
            if (currentDay == previousDay) return previousMinute < morningMinute && currentMinute >= morningMinute;
            return currentDay > previousDay && currentMinute >= morningMinute;
        }

        private void RefreshRouteWindow()
        {
            var phase = Phase;
            if (phase == lastPickupPhase) return;
            lastPickupPhase = phase;
            plot?.RebuildWorldPickups();
        }

        public void SetSimulationSpeed(float value) => simulationSpeed = Mathf.Max(0f, value);

        public void SetWeatherLightMultiplier(float value)
        {
            weatherLightMultiplier = Mathf.Clamp(value, 0.5f, 1f);
            ApplyLighting();
        }

        private void CaptureLighting()
        {
            sun = RenderSettings.sun;
            if (sun == null)
            {
                var lights = FindObjectsByType<Light>();
                foreach (var candidate in lights)
                {
                    if (candidate.type != LightType.Directional) continue;
                    if (sun == null || candidate.intensity > sun.intensity) sun = candidate;
                }
            }

            originalAmbientIntensity = RenderSettings.ambientIntensity;
            originalAmbientMode = RenderSettings.ambientMode;
            originalAmbientSkyColor = RenderSettings.ambientSkyColor;
            originalAmbientEquatorColor = RenderSettings.ambientEquatorColor;
            originalAmbientGroundColor = RenderSettings.ambientGroundColor;
            originalFog = RenderSettings.fog;
            if (sun != null)
            {
                originalSunRotation = sun.transform.rotation;
                originalSunColor = sun.color;
                originalSunIntensity = sun.intensity;
            }
            lightingCaptured = true;
        }

        private void ApplyLighting()
        {
            if (!lightingCaptured) return;
            var minutes = MinutesOfDay;
            CurrentLightFactor = EvaluateLightFactor(minutes);
            var ambientWeatherFactor = Mathf.Lerp(0.90f, 1f, weatherLightMultiplier);
            RenderSettings.fog = false;
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            var nightSky = new Color(0.34f, 0.40f, 0.58f);
            var daySky = new Color(0.62f, 0.76f, 0.88f);
            var nightEquator = new Color(0.34f, 0.36f, 0.46f);
            var dayEquator = new Color(0.66f, 0.56f, 0.38f);
            var nightGround = new Color(0.22f, 0.23f, 0.30f);
            var dayGround = new Color(0.38f, 0.32f, 0.22f);
            RenderSettings.ambientSkyColor = Color.Lerp(nightSky, daySky, CurrentLightFactor);
            RenderSettings.ambientEquatorColor = Color.Lerp(nightEquator, dayEquator, CurrentLightFactor);
            RenderSettings.ambientGroundColor = Color.Lerp(nightGround, dayGround, CurrentLightFactor);
            RenderSettings.ambientIntensity = Mathf.Lerp(Mathf.Max(0.80f, originalAmbientIntensity * 0.80f), Mathf.Max(1.08f, originalAmbientIntensity * 1.08f), CurrentLightFactor) * ambientWeatherFactor;

            if (sun == null) return;
            var yaw = originalSunRotation.eulerAngles.y;
            sun.transform.rotation = Quaternion.Euler((minutes / 4f) - 90f, yaw, 0f);
            sun.intensity = originalSunIntensity * Mathf.Lerp(0.40f, 1.05f, CurrentLightFactor) * weatherLightMultiplier;
            var nightColor = new Color(0.38f, 0.48f, 0.78f);
            sun.color = Color.Lerp(nightColor, originalSunColor, CurrentLightFactor);
        }

        private void OnDestroy()
        {
            if (!lightingCaptured) return;
            RenderSettings.ambientIntensity = originalAmbientIntensity;
            RenderSettings.ambientMode = originalAmbientMode;
            RenderSettings.ambientSkyColor = originalAmbientSkyColor;
            RenderSettings.ambientEquatorColor = originalAmbientEquatorColor;
            RenderSettings.ambientGroundColor = originalAmbientGroundColor;
            RenderSettings.fog = originalFog;
            if (sun == null) return;
            sun.transform.rotation = originalSunRotation;
            sun.color = originalSunColor;
            sun.intensity = originalSunIntensity;
        }

        public static int YearForDay(int day) => ((Mathf.Max(1, day) - 1) / DaysPerYear) + 1;

        public static int DayInSeason(int day) => ((Mathf.Max(1, day) - 1) % DaysPerSeason) + 1;

        public static FarmSeason SeasonForDay(int day)
        {
            var seasonIndex = ((Mathf.Max(1, day) - 1) / DaysPerSeason) % SeasonsPerYear;
            return (FarmSeason)seasonIndex;
        }

        public static string SeasonName(FarmSeason season) => season switch
        {
            FarmSeason.Spring => FarmLocalization.Get("season.spring", "Spring"),
            FarmSeason.Summer => FarmLocalization.Get("season.summer", "Summer"),
            FarmSeason.Autumn => FarmLocalization.Get("season.autumn", "Autumn"),
            FarmSeason.Winter => FarmLocalization.Get("season.winter", "Winter"),
            _ => string.Empty
        };

        public static FarmDayPhase ResolvePhase(float minute)
        {
            minute = Mathf.Repeat(minute, 1440f);
            if (minute < 300f || minute >= 1260f) return FarmDayPhase.Night;
            if (minute < 480f) return FarmDayPhase.Dawn;
            if (minute < 720f) return FarmDayPhase.Morning;
            if (minute < 1080f) return FarmDayPhase.Afternoon;
            return FarmDayPhase.Dusk;
        }

        public static float EvaluateLightFactor(float minute)
        {
            minute = Mathf.Repeat(minute, 1440f);
            if (minute < 300f) return 0.40f;
            if (minute < 480f) return Mathf.SmoothStep(0.40f, 0.85f, (minute - 300f) / 180f);
            if (minute < 720f) return Mathf.SmoothStep(0.85f, 1f, (minute - 480f) / 240f);
            if (minute < 1080f) return 1f;
            if (minute < 1260f) return Mathf.SmoothStep(1f, 0.40f, (minute - 1080f) / 180f);
            return 0.40f;
        }

        public static string FormatTime(float minute)
        {
            var total = Mathf.FloorToInt(Mathf.Repeat(minute, 1440f));
            return $"{total / 60:00}:{total % 60:00}";
        }

        private static string PhaseName(FarmDayPhase phase) => phase switch
        {
            FarmDayPhase.Night => FarmLocalization.Get("phase.night", "Night"),
            FarmDayPhase.Dawn => FarmLocalization.Get("phase.dawn", "Dawn"),
            FarmDayPhase.Morning => FarmLocalization.Get("phase.morning", "Morning"),
            FarmDayPhase.Afternoon => FarmLocalization.Get("phase.afternoon", "Afternoon"),
            FarmDayPhase.Dusk => FarmLocalization.Get("phase.dusk", "Dusk"),
            _ => string.Empty
        };
    }
}
