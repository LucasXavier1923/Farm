using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEngine;

namespace FarmPrototype.Farming
{
    [Serializable]
    public sealed class FarmLocalizationEntry
    {
        public string Key;
        [TextArea] public string Value;
    }

    [Serializable]
    public sealed class FarmLocalizationDocument
    {
        public string LanguageCode = "en";
        public List<FarmLocalizationEntry> Entries = new();
    }

    /// <summary>
    /// Data-driven runtime text service. Add a JSON file under
    /// Resources/FarmLocalization with the same keys as en.json to add a language.
    /// Gameplay code only references keys and never embeds a language-specific translation.
    /// </summary>
    public static class FarmLocalization
    {
        public const string DefaultLanguageCode = "en";
        private const string ResourceFolder = "FarmLocalization";
        private const string LanguagePreferenceKey = "FarmPrototype.Language";

        private static readonly Dictionary<string, Dictionary<string, string>> Tables =
            new(StringComparer.OrdinalIgnoreCase);
        private static readonly List<string> LanguageCodes = new();
        private static bool loaded;
        private static string currentLanguageCode;

        public static event Action LanguageChanged;

        public static string CurrentLanguageCode
        {
            get
            {
                EnsureLoaded();
                return currentLanguageCode;
            }
        }

        public static IReadOnlyList<string> AvailableLanguageCodes
        {
            get
            {
                EnsureLoaded();
                return LanguageCodes;
            }
        }

        public static string Get(string key, string fallback = null)
        {
            EnsureLoaded();
            if (string.IsNullOrWhiteSpace(key)) return fallback ?? string.Empty;
            if (TryGet(currentLanguageCode, key, out var value)) return value;
            if (!string.Equals(currentLanguageCode, DefaultLanguageCode, StringComparison.OrdinalIgnoreCase) &&
                TryGet(DefaultLanguageCode, key, out value)) return value;
            return fallback ?? key;
        }

        public static string Format(string key, params object[] args) =>
            Format(key, null, args);

        public static string Format(string key, string fallback, params object[] args)
        {
            var format = Get(key, fallback);
            try { return string.Format(CultureInfo.InvariantCulture, format, args ?? Array.Empty<object>()); }
            catch (FormatException) { return format; }
        }

        public static bool SetLanguage(string languageCode)
        {
            EnsureLoaded();
            if (string.IsNullOrWhiteSpace(languageCode) || !Tables.ContainsKey(languageCode)) return false;
            if (string.Equals(currentLanguageCode, languageCode, StringComparison.OrdinalIgnoreCase)) return true;
            currentLanguageCode = languageCode;
            PlayerPrefs.SetString(LanguagePreferenceKey, currentLanguageCode);
            PlayerPrefs.Save();
            LanguageChanged?.Invoke();
            return true;
        }

        public static void ReloadForTesting()
        {
            ResetRuntimeState();
            EnsureLoaded();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetRuntimeState()
        {
            loaded = false;
            currentLanguageCode = null;
            Tables.Clear();
            LanguageCodes.Clear();
        }

        private static bool TryGet(string languageCode, string key, out string value)
        {
            value = null;
            return !string.IsNullOrWhiteSpace(languageCode) && Tables.TryGetValue(languageCode, out var table) &&
                table.TryGetValue(key, out value) && !string.IsNullOrEmpty(value);
        }

        private static void EnsureLoaded()
        {
            // Enter Play Mode can keep the managed domain alive. If an editor-time lookup
            // happened before Resources were ready, retry instead of preserving an empty table.
            if (loaded && Tables.Count > 0) return;
            loaded = true;
            Tables.Clear();
            LanguageCodes.Clear();

            foreach (var asset in Resources.LoadAll<TextAsset>(ResourceFolder))
            {
                if (asset == null || string.IsNullOrWhiteSpace(asset.text)) continue;
                FarmLocalizationDocument document;
                try { document = JsonUtility.FromJson<FarmLocalizationDocument>(asset.text); }
                catch { continue; }
                if (document == null || string.IsNullOrWhiteSpace(document.LanguageCode)) continue;

                var languageCode = document.LanguageCode.Trim();
                var table = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                foreach (var entry in document.Entries ?? new List<FarmLocalizationEntry>())
                    if (entry != null && !string.IsNullOrWhiteSpace(entry.Key) && entry.Value != null)
                        table[entry.Key.Trim()] = NormalizeSerializedText(entry.Value);

                Tables[languageCode] = table;
                LanguageCodes.Add(languageCode);
            }

            LanguageCodes.Sort(StringComparer.OrdinalIgnoreCase);
            var preferred = PlayerPrefs.GetString(LanguagePreferenceKey, DefaultLanguageCode);
            currentLanguageCode = Tables.ContainsKey(preferred)
                ? preferred
                : Tables.ContainsKey(DefaultLanguageCode) ? DefaultLanguageCode : LanguageCodes.Count > 0 ? LanguageCodes[0] : DefaultLanguageCode;
        }

        /// <summary>
        /// JSON exported by external localization tools can contain escaped sequences twice
        /// (for example <c>\\n</c> or <c>\\u2022</c>). JsonUtility correctly reads the outer
        /// JSON layer but leaves the inner escape visible to the player. Decode that final
        /// layer centrally so every UI surface and every future language file is protected.
        /// </summary>
        private static string NormalizeSerializedText(string value)
        {
            if (string.IsNullOrEmpty(value)) return value ?? string.Empty;

            var builder = new StringBuilder(value.Length);
            for (var index = 0; index < value.Length; index++)
            {
                var current = value[index];
                if (current != '\\' || index + 1 >= value.Length)
                {
                    builder.Append(current);
                    continue;
                }

                var escape = value[++index];
                switch (escape)
                {
                    case 'n': builder.Append('\n'); break;
                    case 'r': builder.Append('\r'); break;
                    case 't': builder.Append('\t'); break;
                    case '\\': builder.Append('\\'); break;
                    case 'u' when index + 4 < value.Length:
                    {
                        var hex = value.Substring(index + 1, 4);
                        if (ushort.TryParse(hex, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var codePoint))
                        {
                            builder.Append((char)codePoint);
                            index += 4;
                        }
                        else
                        {
                            builder.Append('\\').Append('u');
                        }
                        break;
                    }
                    default:
                        builder.Append('\\').Append(escape);
                        break;
                }
            }

            // Repair a few common UTF-8-as-Windows-1252 artifacts in legacy English data.
            return builder.ToString()
                .Replace("â€¢", "\u2022")
                .Replace("â€”", "\u2014")
                .Replace("â€“", "\u2013")
                .Replace("â€¦", "\u2026");
        }
    }
}
