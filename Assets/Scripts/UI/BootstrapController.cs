using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ScrewGame.UI
{
    /// <summary>
    /// Drives the Bootstrap/splash scene:
    ///   – Animates the progress bar
    ///   – Loads the Menu scene after a short delay
    ///   – Sets the locale from PlayerPrefs on startup
    /// </summary>
    public class BootstrapController : MonoBehaviour
    {
        [Header("UI")]
        [SerializeField] private Image            _progressFill;
        [SerializeField] private TextMeshProUGUI  _loadingText;
        [SerializeField] private CanvasGroup      _canvasGroup;

        [Header("Timing")]
        [SerializeField] private float _splashDuration = 2.5f;

        // Menu scene is always build index 1
        private const int MenuBuildIndex = 1;

        private static readonly string[] _dots = { ".", "..", "...", ".." };

        private void Start()
        {
            // Ensure locale is loaded
            ScrewGame.Localization.LocalizationManager.Instance?.SetLocale(
                PlayerPrefs.GetString("locale", "en"));

            StartCoroutine(SplashRoutine());
        }

        private IEnumerator SplashRoutine()
        {
            float elapsed = 0f;
            int   dotIdx  = 0;

            while (elapsed < _splashDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / _splashDuration);

                if (_progressFill != null)
                    _progressFill.fillAmount = t;

                // Animated loading dots
                if (_loadingText != null)
                {
                    dotIdx = Mathf.FloorToInt(elapsed * 2f) % _dots.Length;
                    _loadingText.text = ScrewGame.Localization.LocalizationManager.T("loading")
                                        .TrimEnd('.') + _dots[dotIdx];
                }

                yield return null;
            }

            // Fade out
            if (_canvasGroup != null)
            {
                float fadeTime = 0.4f, fadeElapsed = 0f;
                while (fadeElapsed < fadeTime)
                {
                    fadeElapsed += Time.deltaTime;
                    _canvasGroup.alpha = 1f - fadeElapsed / fadeTime;
                    yield return null;
                }
            }

            SceneManager.LoadScene(MenuBuildIndex);
        }
    }
}
