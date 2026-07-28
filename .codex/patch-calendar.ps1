$ErrorActionPreference = 'Stop'
$project = 'D:\Dev\Unity\Farm\Farm'
$cli = 'C:\Users\Lucas\AppData\Roaming\npm\unity-mcp-cli.cmd'
function Replace-Once([string]$content,[string]$old,[string]$new,[string]$label){if(-not $content.Contains($old)){throw "Trecho nao encontrado: $label"};$content.Replace($old,$new)}
function Submit([string]$path,[string]$content,[string]$id){$payload=@{filePath=$path;content=$content;requestId=$id}|ConvertTo-Json -Compress;$result=$payload|& $cli run-tool script-update-or-create $project --input-file -;if($LASTEXITCODE-ne 0){throw "Falha: $path"};$result|Select-Object -Last 12;Start-Sleep -Seconds 2}

$dayPath=Join-Path $project 'Assets\_Project\Scripts\Farming\FarmDayClock.cs'
$day=[IO.File]::ReadAllText($dayPath)
if(-not $day.Contains('DaysPerSeason')){
  $day=Replace-Once $day @'
    public enum FarmDayPhase { Night, Dawn, Morning, Afternoon, Dusk }
'@ @'
    public enum FarmDayPhase { Night, Dawn, Morning, Afternoon, Dusk }
    public enum FarmSeason { Spring, Summer, Autumn, Winter }
'@ 'season enum'
  $day=Replace-Once $day @'
    public sealed class FarmDayClock : MonoBehaviour
    {
'@ @'
    public sealed class FarmDayClock : MonoBehaviour
    {
        public const int DaysPerSeason = 7;
        public const int SeasonsPerYear = 4;
        public const int DaysPerYear = DaysPerSeason * SeasonsPerYear;
'@ 'calendar constants'
  $day=Replace-Once $day @'
        public FarmDayPhase Phase => ResolvePhase(MinutesOfDay);
'@ @'
        public FarmDayPhase Phase => ResolvePhase(MinutesOfDay);
        public int YearNumber => YearForDay(DayNumber);
        public FarmSeason CurrentSeason => SeasonForDay(DayNumber);
        public int DayOfSeason => DayInSeason(DayNumber);
        public string SeasonDisplayText => $"{SeasonName(CurrentSeason)} {DayOfSeason}/{DaysPerSeason}";
        public string CalendarText => $"ANO {YearNumber}  \u2022  {SeasonDisplayText}";
'@ 'calendar properties'
  $day=Replace-Once $day @'
        public static FarmDayPhase ResolvePhase(float minute)
'@ @'
        public static int YearForDay(int day) => ((Mathf.Max(1, day) - 1) / DaysPerYear) + 1;

        public static int DayInSeason(int day) => ((Mathf.Max(1, day) - 1) % DaysPerSeason) + 1;

        public static FarmSeason SeasonForDay(int day)
        {
            var seasonIndex = ((Mathf.Max(1, day) - 1) / DaysPerSeason) % SeasonsPerYear;
            return (FarmSeason)seasonIndex;
        }

        public static string SeasonName(FarmSeason season) => season switch
        {
            FarmSeason.Spring => "Primavera",
            FarmSeason.Summer => "Ver\u00E3o",
            FarmSeason.Autumn => "Outono",
            FarmSeason.Winter => "Inverno",
            _ => string.Empty
        };

        public static FarmDayPhase ResolvePhase(float minute)
'@ 'calendar static API'
  Submit 'Assets/_Project/Scripts/Farming/FarmDayClock.cs' $day 'calendar-day-clock-v10'
}

$hudPath=Join-Path $project 'Assets\_Project\Scripts\Farming\FarmHudController.cs'
$hud=[IO.File]::ReadAllText($hudPath)
if(-not $hud.Contains('calendarText')){
  $hud=Replace-Once $hud @'
        private Text clockText;
        private Text weatherText;
'@ @'
        private Text clockText;
        private Text calendarText;
        private Text weatherText;
'@ 'calendar field'
  $hud=Replace-Once $hud @'
                clockText.text = plot.DayClock.DisplayText;
                clockText.color = plot.DayClock.Phase is FarmDayPhase.Night or FarmDayPhase.Dawn ? new Color(0.62f, 0.76f, 1f) : AccentColor;
'@ @'
                clockText.text = plot.DayClock.DisplayText;
                clockText.color = plot.DayClock.Phase is FarmDayPhase.Night or FarmDayPhase.Dawn ? new Color(0.62f, 0.76f, 1f) : AccentColor;
                calendarText.text = plot.DayClock.CalendarText;
                calendarText.color = plot.DayClock.CurrentSeason switch
                {
                    FarmSeason.Spring => new Color(0.58f, 0.88f, 0.48f),
                    FarmSeason.Summer => new Color(1f, 0.78f, 0.28f),
                    FarmSeason.Autumn => new Color(0.94f, 0.48f, 0.20f),
                    FarmSeason.Winter => new Color(0.58f, 0.78f, 1f),
                    _ => Color.white
                };
'@ 'calendar HUD update'
  $hud=Replace-Once $hud 'clockText == null || weatherText == null' 'clockText == null || calendarText == null || weatherText == null' 'interface ready calendar'
  $hud=Replace-Once $hud @'
            var clockPanel = CreatePanel("FarmClock", root.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -18f), new Vector2(390f, 108f), new Vector2(0.5f, 1f), PanelColor);
            CreateText("ClockTitle", clockPanel.transform, "TEMPO DA FAZENDA", 13, FontStyle.Bold, new Color(0.72f, 0.76f, 0.68f), new Vector2(15f, -8f), new Vector2(360f, 20f), TextAnchor.MiddleCenter);
            clockText = CreateText("ClockValue", clockPanel.transform, "DIA 1   08:00   Manh\u00E3", 21, FontStyle.Bold, AccentColor, new Vector2(15f, -31f), new Vector2(360f, 34f), TextAnchor.MiddleCenter);
            weatherText = CreateText("WeatherValue", clockPanel.transform, "Ensolarado   \u2022   Amanh\u00E3: Nublado", 14, FontStyle.Bold, new Color(1f, 0.82f, 0.34f), new Vector2(15f, -70f), new Vector2(360f, 24f), TextAnchor.MiddleCenter);
'@ @'
            var clockPanel = CreatePanel("FarmClock", root.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -18f), new Vector2(440f, 142f), new Vector2(0.5f, 1f), PanelColor);
            CreateText("ClockTitle", clockPanel.transform, "TEMPO DA FAZENDA", 13, FontStyle.Bold, new Color(0.72f, 0.76f, 0.68f), new Vector2(15f, -7f), new Vector2(410f, 20f), TextAnchor.MiddleCenter);
            clockText = CreateText("ClockValue", clockPanel.transform, "DIA 1   08:00   Manh\u00E3", 21, FontStyle.Bold, AccentColor, new Vector2(15f, -28f), new Vector2(410f, 32f), TextAnchor.MiddleCenter);
            calendarText = CreateText("CalendarValue", clockPanel.transform, "ANO 1  \u2022  Primavera 1/7", 14, FontStyle.Bold, new Color(0.58f, 0.88f, 0.48f), new Vector2(15f, -63f), new Vector2(410f, 24f), TextAnchor.MiddleCenter);
            weatherText = CreateText("WeatherValue", clockPanel.transform, "Ensolarado   \u2022   Amanh\u00E3: Nublado", 14, FontStyle.Bold, new Color(1f, 0.82f, 0.34f), new Vector2(15f, -98f), new Vector2(410f, 24f), TextAnchor.MiddleCenter);
'@ 'calendar clock panel'
  Submit 'Assets/_Project/Scripts/Farming/FarmHudController.cs' $hud 'calendar-hud-v10'
}

$plotPath=Join-Path $project 'Assets\_Project\Scripts\Farming\FarmTestPlot.cs'
$plot=[IO.File]::ReadAllText($plotPath)
if(-not $plot.Contains('dayClock.SeasonDisplayText')){
  $plot=Replace-Once $plot @'
            hud.ShowDayTransition(nextDay, weatherSystem != null ? FarmWeatherSystem.WeatherName(weatherSystem.CurrentWeather) : string.Empty);
'@ @'
            var morningContext = dayClock.SeasonDisplayText;
            if (weatherSystem != null) morningContext += $"  \u2022  {FarmWeatherSystem.WeatherName(weatherSystem.CurrentWeather)}";
            hud.ShowDayTransition(nextDay, morningContext);
'@ 'calendar morning transition'
  Submit 'Assets/_Project/Scripts/Farming/FarmTestPlot.cs' $plot 'calendar-sleep-transition-v10'
}
Write-Output 'Calendar submitted.'
