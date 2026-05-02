using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using ScrewGame.Core;
using ScrewGame.Localization;

namespace ScrewGame.UI
{
    /// <summary>
    /// Controls all Menu scene interactions:
    ///   – Mode buttons start a local game with the correct GameMode
    ///   – Language toggle switches AR / EN and refreshes all LocalizedText components
    ///   – How To Play opens InstructionsOverlay
    /// </summary>
    public class MenuController : MonoBehaviour
    {
        // ─── Inspector ────────────────────────────────────────────────────────
        [Header("Mode Buttons")]
        [SerializeField] private Button _btnGeneral;
        [SerializeField] private Button _btnClassic;
        [SerializeField] private Button _btnThief;
        [SerializeField] private Button _btnDoubles;

        [Header("Language Toggle")]
        [SerializeField] private Button _btnAR;
        [SerializeField] private Button _btnEN;

        [Header("How To Play")]
        [SerializeField] private Button _btnHowToPlay;

        [Header("Mode Labels (optional localised)")]
        [SerializeField] private TextMeshProUGUI _labelGeneral;
        [SerializeField] private TextMeshProUGUI _labelClassic;
        [SerializeField] private TextMeshProUGUI _labelThief;
        [SerializeField] private TextMeshProUGUI _labelDoubles;

        // Gameplay is always build index 2
        private const int GameplayBuildIndex = 2;

        // Key used to pass chosen mode across scenes
        public static GameMode ChosenMode { get; private set; } = GameMode.General;

        // ─── Lifecycle ────────────────────────────────────────────────────────
        private void Start()
        {
            // Mode buttons
            _btnGeneral?.onClick.AddListener(() => StartGame(GameMode.General));
            _btnClassic?.onClick.AddListener(() => StartGame(GameMode.Classic));
            _btnThief  ?.onClick.AddListener(() => StartGame(GameMode.Thief));
            _btnDoubles?.onClick.AddListener(() => StartGame(GameMode.Doubles));

            // Language
            _btnAR?.onClick.AddListener(() => SwitchLocale("ar"));
            _btnEN?.onClick.AddListener(() => SwitchLocale("en"));

            // How to Play
            _btnHowToPlay?.onClick.AddListener(() => InstructionsOverlay.Instance?.Show());

            // Subscribe to language changes so labels refresh
            if (LocalizationManager.Instance != null)
                LocalizationManager.Instance.OnLanguageChanged += RefreshLabels;

            RefreshLabels();
        }

        private void OnDestroy()
        {
            if (LocalizationManager.Instance != null)
                LocalizationManager.Instance.OnLanguageChanged -= RefreshLabels;
        }

        // ─── Actions ──────────────────────────────────────────────────────────
        private void StartGame(GameMode mode)
        {
            ChosenMode = mode;
            SceneManager.LoadScene(GameplayBuildIndex);
        }

        private void SwitchLocale(string locale)
        {
            LocalizationManager.Instance?.SetLocale(locale);
        }

        private void RefreshLabels()
        {
            SetLabel(_labelGeneral, "mode_general");
            SetLabel(_labelClassic, "mode_classic");
            SetLabel(_labelThief,   "mode_thief");
            SetLabel(_labelDoubles, "mode_doubles");
        }

        private static void SetLabel(TextMeshProUGUI tmp, string key)
        {
            if (tmp == null) return;
            tmp.text = LocalizationManager.T(key);
        }

        // ─── Auto-wire (called from scene at runtime if inspector refs are null) ─
        private void Awake()
        {
            AutoWireIfNeeded();
        }

        private void AutoWireIfNeeded()
        {
            if (_btnGeneral == null) _btnGeneral = FindBtnByName("Btn_General");
            if (_btnClassic == null) _btnClassic = FindBtnByName("Btn_Classic");
            if (_btnThief   == null) _btnThief   = FindBtnByName("Btn_Thief");
            if (_btnDoubles == null) _btnDoubles = FindBtnByName("Btn_Doubles");
            if (_btnAR      == null) _btnAR      = FindBtnByName("Lang_AR");
            if (_btnEN      == null) _btnEN      = FindBtnByName("Lang_EN");
            if (_btnHowToPlay == null) _btnHowToPlay = FindBtnByName("Btn_HowToPlay");

            _labelGeneral = GetChildTMP(_btnGeneral);
            _labelClassic = GetChildTMP(_btnClassic);
            _labelThief   = GetChildTMP(_btnThief);
            _labelDoubles = GetChildTMP(_btnDoubles);
        }

        private static Button FindBtnByName(string name)
        {
            var go = GameObject.Find(name);
            return go != null ? go.GetComponent<Button>() : null;
        }

        private static TextMeshProUGUI GetChildTMP(Button btn)
        {
            if (btn == null) return null;
            return btn.GetComponentInChildren<TextMeshProUGUI>();
        }
    }
}
