using UnityEngine;

namespace FarmPrototype.Farming
{
    /// <summary>
    /// Lightweight day-flow guidance. It never pauses or blocks the player; it
    /// turns clock, energy, weather, and sleep into readable cozy-farm pacing.
    /// </summary>
    public sealed class FarmDailyRhythm : MonoBehaviour
    {
        private FarmTestPlot plot;
        private FarmGameState state;
        private FarmDayClock clock;
        private int lastReminderDay = -1;
        private int lastMorningDay = -1;
        private string prompt;

        public string CurrentGuidance => prompt ?? string.Empty;

        public void Initialize(FarmTestPlot owner, FarmGameState gameState, FarmDayClock dayClock)
        {
            plot = owner;
            state = gameState;
            clock = dayClock;
            RefreshGuidance(true);
        }

        private void Update() => RefreshGuidance(false);

        private void RefreshGuidance(bool immediate)
        {
            if (plot == null || state == null || clock == null) return;
            var minute = clock.MinutesOfDay;
            var day = clock.DayNumber;
            if (day != lastMorningDay && minute >= 360f)
            {
                lastMorningDay = day;
                lastReminderDay = -1;
            }

            var next = string.Empty;
            if (state.IsExhausted)
                next = FarmLocalization.Get("rhythm.tired", "You are tired. Slow down, organize the farm, or sleep when ready.");
            else if (minute >= 1260f || minute < 300f)
                next = FarmLocalization.Get("rhythm.night", "The farm is quiet. You can keep working or head to bed when ready.");
            else if (minute >= 1140f)
                next = FarmLocalization.Get("rhythm.evening", "Evening is here. Finish your tasks and plan tomorrow's crops.");
            else if (clock.Phase == FarmDayPhase.Morning && plot.WeatherSystem != null && plot.WeatherSystem.CurrentWeather == FarmWeather.Rain)
                next = FarmLocalization.Get("rhythm.rain", "Rain is watering the fields today. Use the time for harvesting, crafting, or exploring.");

            if (!string.Equals(prompt, next, System.StringComparison.Ordinal) || immediate)
            {
                prompt = next;
                plot.SetExternalPrompt(this, prompt);
            }

            if (minute >= 1140f && lastReminderDay != day)
                lastReminderDay = day;
        }

        private void OnDestroy() => plot?.SetExternalPrompt(this, string.Empty);
    }
}
