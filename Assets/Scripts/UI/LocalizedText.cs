using TMPro;
using UnityEngine;
using ScrewGame.Localization;

namespace ScrewGame.UI
{
    /// <summary>
    /// Attach to any TextMeshProUGUI or TextMeshPro object.
    /// Reads the localization key and updates the text automatically
    /// when the locale changes. Also applies RTL alignment for Arabic.
    /// </summary>
    [RequireComponent(typeof(TMP_Text))]
    public class LocalizedText : MonoBehaviour
    {
        [SerializeField] private string _key;

        private TMP_Text _tmp;

        private void Awake() => _tmp = GetComponent<TMP_Text>();

        private void Start()
        {
            Apply();
            if (LocalizationManager.Instance != null)
                LocalizationManager.Instance.OnLanguageChanged += Apply;
        }

        private void OnDestroy()
        {
            if (LocalizationManager.Instance != null)
                LocalizationManager.Instance.OnLanguageChanged -= Apply;
        }

        public void SetKey(string key)
        {
            _key = key;
            Apply();
        }

        public void Apply()
        {
            if (_tmp == null) _tmp = GetComponent<TMP_Text>();
            if (_tmp == null || string.IsNullOrEmpty(_key)) return;

            string text = LocalizationManager.T(_key);
            _tmp.text = text;
            
            bool rtl = LocalizationManager.Instance != null && LocalizationManager.Instance.IsRtl;
            _tmp.alignment = rtl ? TextAlignmentOptions.Right : TextAlignmentOptions.Left;

            // Handle font switching based on locale
            if (rtl)
            {
                var arabicFont = Resources.Load<TMP_FontAsset>("Fonts/Skrew_Arabic_Font");
                if (arabicFont != null) _tmp.font = arabicFont;
            }
            else
            {
                // Revert to a standard font for English
                var defaultFont = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
                if (defaultFont != null) _tmp.font = defaultFont;
            }
        }
    }
}
