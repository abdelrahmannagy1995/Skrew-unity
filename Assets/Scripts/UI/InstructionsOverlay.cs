using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ScrewGame.UI
{
    /// <summary>
    /// "كيفية اللعب" (How to Play) overlay for the Menu scene.
    /// Shows paginated instructions with Prev / Next / Close buttons.
    /// </summary>
    public class InstructionsOverlay : MonoBehaviour
    {
        public static InstructionsOverlay Instance { get; private set; }

        [Header("Panel")]
        [SerializeField] private CanvasGroup _panelGroup;
        [SerializeField] private RectTransform _panelRect;

        [Header("Content")]
        [SerializeField] private TextMeshProUGUI _titleText;
        [SerializeField] private TextMeshProUGUI _bodyText;
        [SerializeField] private TextMeshProUGUI _pageIndicator;

        [Header("Buttons")]
        [SerializeField] private Button _prevBtn;
        [SerializeField] private Button _nextBtn;
        [SerializeField] private Button _closeBtn;

        // ─── Instructions pages ───────────────────────────────────
        private static readonly string[] _titles = new[]
        {
            "🎴 هدف اللعبة",
            "🃏 أنواع الأوراق",
            "⚡ أوراق الأوامر",
            "🌟 الأوراق الخاصة",
            "🔄 كيفية اللعب",
            "🏆 الفوز",
        };

        private static readonly string[] _bodies = new[]
        {
            // Objective
            "هدفك هو تجميع أقل مجموع نقاط ممكن في أوراقك الأربع.\n\n" +
            "كل لاعب يبدأ بأربع أوراق مقلوبة، ويمكنه رؤية ورقتين منها فقط.",

            // Card types
            "📌 الأوراق الرقمية (0-10): قيمة الورقة تساوي رقمها.\n\n" +
            "📌 أوراق الأوامر: أوراق خاصة بتأثيرات فورية.\n\n" +
            "📌 الأوراق الخاصة: أوراق نادرة بقوى استثنائية.",

            // Command cards
            "👁 تلحق نفسك — انظر إلى إحدى أوراقك\n" +
            "🔍 تلحق خصمك — انظر إلى ورقة خصمك\n" +
            "💥 بصرة — ضع ورقة الخصم في منتصف الطاولة\n" +
            "↔ خذ وحط — استبدل ورقة بأخرى\n" +
            "→ خذ بس — خذ ورقة الخصم\n" +
            "🎲 كعب دائر — اقلب ورقة عشوائية\n" +
            "⟳ عجب ما عجب — تبادل يدك مع خصمك\n" +
            "✦ على كيفك — اختر أي تأثير تريده",

            // Special cards
            "سارق 🦹 — اسرق ورقة من أي خصم\n" +
            "بينج 🔵 — ألغِ تأثير الورقة التالية\n" +
            "بونج 🟠 — حوّل التأثير إلى خصمك\n" +
            "سكرو أخضر 🟢 — نقاط مضاعفة للفائز\n" +
            "سكرو أحمر 🔴 — نقاط مضاعفة للخاسر",

            // Gameplay
            "في دورك:\n" +
            "1️⃣ اسحب ورقة من المجموعة أو الكومة المكشوفة\n" +
            "2️⃣ استبدلها بواحدة من أوراقك الأربع، أو ارمِها\n" +
            "3️⃣ إذا سحبت ورقة أمر، نفّذ تأثيرها فوراً\n\n" +
            "عند قول «سكرو» ← تُكشف جميع الأوراق وتُحسب النقاط!",

            // Winning
            "أقل مجموع نقاط = فوز!\n\n" +
            "⚠️ انتبه: إذا قلت «سكرو» ولم تكن الأقل نقاطاً،\n" +
            "ستحصل على ضعف النقاط عقوبة!\n\n" +
            "🏅 اللاعب الذي يصل إلى 100 نقطة يخسر الجولة.",
        };

        private int _currentPage;

        private void Awake()
        {
            Instance = this;
            gameObject.SetActive(false);
        }

        private void Start()
        {
            if (_prevBtn  != null) _prevBtn .onClick.AddListener(PrevPage);
            if (_nextBtn  != null) _nextBtn .onClick.AddListener(NextPage);
            if (_closeBtn != null) _closeBtn.onClick.AddListener(Hide);
        }

        // ─── Public API ───────────────────────────────────────────
        public void Show(int startPage = 0)
        {
            _currentPage = Mathf.Clamp(startPage, 0, _titles.Length - 1);
            gameObject.SetActive(true);
            RefreshPage();

            if (_panelGroup != null)
            {
                _panelGroup.alpha = 0f;
                _panelGroup.DOFade(1f, 0.3f);
            }
            if (_panelRect != null)
            {
                _panelRect.localScale = Vector3.one * 0.85f;
                _panelRect.DOScale(1f, 0.3f).SetEase(Ease.OutBack);
            }
        }

        public void Hide()
        {
            if (_panelGroup != null)
                _panelGroup.DOFade(0f, 0.2f).OnComplete(() => gameObject.SetActive(false));
            else
                gameObject.SetActive(false);
        }

        // ─── Pagination ───────────────────────────────────────────
        private void PrevPage()
        {
            if (_currentPage > 0) { _currentPage--; RefreshPage(); }
        }

        private void NextPage()
        {
            if (_currentPage < _titles.Length - 1) { _currentPage++; RefreshPage(); }
        }

        private void RefreshPage()
        {
            if (_titleText     != null) _titleText.text     = _titles[_currentPage];
            if (_bodyText      != null) _bodyText.text      = _bodies[_currentPage];
            if (_pageIndicator != null)
                _pageIndicator.text = $"{_currentPage + 1} / {_titles.Length}";

            if (_prevBtn != null) _prevBtn.interactable = _currentPage > 0;
            if (_nextBtn != null) _nextBtn.interactable = _currentPage < _titles.Length - 1;

            // Animate page transition
            if (_bodyText != null)
            {
                _bodyText.transform.DOKill();
                _bodyText.transform.localScale = new Vector3(0.95f, 0.95f, 1f);
                _bodyText.transform.DOScale(1f, 0.2f).SetEase(Ease.OutQuad);
            }
        }
    }
}
