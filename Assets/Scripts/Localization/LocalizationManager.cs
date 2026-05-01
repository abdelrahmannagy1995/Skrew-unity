using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace ScrewGame.Localization
{
    /// <summary>
    /// Simple i18n manager that loads JSON dictionaries from Resources/Localization/
    /// and provides RTL-safe string lookup.
    /// Supported locales: "en" (English), "ar" (Egyptian Arabic).
    /// </summary>
    public class LocalizationManager : MonoBehaviour
    {
        // -----------------------------------------------------------------------
        // Singleton
        // -----------------------------------------------------------------------
        public static LocalizationManager Instance { get; private set; }

        // -----------------------------------------------------------------------
        // State
        // -----------------------------------------------------------------------
        private Dictionary<string, string> _strings = new();
        private string _currentLocale = "en";

        public string CurrentLocale => _currentLocale;
        public bool IsRtl => _currentLocale == "ar";

        public event Action OnLanguageChanged;

        // -----------------------------------------------------------------------
        // Unity lifecycle
        // -----------------------------------------------------------------------
        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            // Load default locale from PlayerPrefs (or default to "en")
            string saved = PlayerPrefs.GetString("locale", "en");
            LoadLocale(saved);
        }

        // -----------------------------------------------------------------------
        // Public API
        // -----------------------------------------------------------------------

        /// <summary>Switch the active locale and reload all strings.</summary>
        public void SetLocale(string locale)
        {
            if (_currentLocale == locale) return;
            LoadLocale(locale);
            PlayerPrefs.SetString("locale", locale);
            OnLanguageChanged?.Invoke();
        }

        /// <summary>
        /// Retrieve a localised string by key.
        /// Applies RTL processing when locale is Arabic.
        /// </summary>
        public string Get(string key)
        {
            if (!_strings.TryGetValue(key, out var value))
            {
                Debug.LogWarning($"[Localization] Missing key: '{key}' for locale '{_currentLocale}'");
                return key;
            }

            return IsRtl ? RtlProcessor.Process(value) : value;
        }

        /// <summary>Convenience shorthand.</summary>
        public static string T(string key) => Instance?.Get(key) ?? key;

        // -----------------------------------------------------------------------
        // Loading
        // -----------------------------------------------------------------------
        private void LoadLocale(string locale)
        {
            var asset = Resources.Load<TextAsset>($"Localization/{locale}");
            if (asset == null)
            {
                Debug.LogError($"[Localization] Could not find locale file: Resources/Localization/{locale}.json");
                return;
            }

            try
            {
                _strings       = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, string>>(asset.text)
                                 ?? new Dictionary<string, string>();
                _currentLocale = locale;
                Debug.Log($"[Localization] Loaded {_strings.Count} strings for locale '{locale}'.");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Localization] Failed to parse locale '{locale}': {ex.Message}");
            }
        }
    }

    // =========================================================================
    // RTL Processor – wraps RTLTMPro / ArabicSupport plugin calls
    // =========================================================================

    /// <summary>
    /// Thin wrapper around the RTLTMPro / ArabicSupport plugin.
    /// Corrects Arabic letter connectivity and reading direction before the
    /// string is passed to TextMeshPro.
    /// Requires RTLTMPro to be installed in the project.
    /// </summary>
    public static class RtlProcessor
    {
        // When RTLTMPro is imported, it registers the RTLSupport namespace.
        // This method calls it via reflection so the Localization module compiles
        // even before the plugin is installed.
        public static string Process(string arabicText)
        {
            if (string.IsNullOrEmpty(arabicText)) return arabicText;

#if RTLTMPRO_IMPORTED
            // Direct call when RTLTMPro is present in the project
            return RTLTMPro.RTLSupport.FixRTL(arabicText);
#else
            // Fallback: attempt via reflection to avoid hard dependency
            var type = Type.GetType("RTLTMPro.RTLSupport, RTLTMPro");
            if (type != null)
            {
                var method = type.GetMethod("FixRTL", new[] { typeof(string) });
                if (method != null)
                    return method.Invoke(null, new object[] { arabicText }) as string ?? arabicText;
            }

            // Last resort: reverse the string so at minimum it reads RTL
            var chars = arabicText.ToCharArray();
            Array.Reverse(chars);
            return new string(chars);
#endif
        }
    }
}
