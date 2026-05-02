using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ScrewGame.UI
{
    /// <summary>
    /// World-space nameplate that floats above a player's card grid.
    /// Shows avatar initial, display name, and current score.
    /// Highlights when it is this player's turn.
    /// </summary>
    public class PlayerNameplate : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private TextMeshProUGUI _nameText;
        [SerializeField] private TextMeshProUGUI _scoreText;
        [SerializeField] private TextMeshProUGUI _avatarInitialText;
        [SerializeField] private Image _avatarBg;
        [SerializeField] private Image _turnHighlight;

        [Header("Colors")]
        [SerializeField] private Color _myTurnColor    = new Color(1f, 0.85f, 0.1f, 0.9f);
        [SerializeField] private Color _theirTurnColor = new Color(1f, 1f, 1f, 0.0f);

        private Tween _glowLoop;

        // ─── Public API ───────────────────────────────────────────
        public void Setup(string displayName, int avatarColorIndex)
        {
            if (_nameText != null) _nameText.text = displayName;
            if (_avatarInitialText != null)
            {
                string initial = displayName.Length > 0
                    ? displayName.Substring(0, 1).ToUpper()
                    : "?";
                _avatarInitialText.text = initial;
            }
            if (_avatarBg != null)
                _avatarBg.color = AvatarColor(avatarColorIndex);

            SetMyTurn(false);
        }

        public void UpdateScore(int score)
        {
            if (_scoreText == null) return;
            string prev = _scoreText.text;
            _scoreText.text = score.ToString();
            if (prev != score.ToString())
            {
                _scoreText.transform.DOKill();
                _scoreText.transform.DOPunchScale(Vector3.one * 0.4f, 0.35f, 4, 0.5f);
            }
        }

        public void SetMyTurn(bool active)
        {
            if (_turnHighlight == null) return;
            _glowLoop?.Kill();
            if (active)
            {
                _turnHighlight.color = _myTurnColor;
                _glowLoop = _turnHighlight
                    .DOFade(0.4f, 0.6f)
                    .SetLoops(-1, LoopType.Yoyo)
                    .SetEase(Ease.InOutSine);
            }
            else
            {
                _turnHighlight.color = _theirTurnColor;
            }
        }

        // ─── Helper ───────────────────────────────────────────────
        private static readonly Color[] _avatarPalette = new Color[]
        {
            new Color(0.20f, 0.50f, 0.90f),
            new Color(0.90f, 0.25f, 0.25f),
            new Color(0.15f, 0.70f, 0.30f),
            new Color(0.80f, 0.55f, 0.05f),
            new Color(0.55f, 0.15f, 0.80f),
            new Color(0.10f, 0.65f, 0.75f),
        };

        private static Color AvatarColor(int index)
            => _avatarPalette[index % _avatarPalette.Length];
    }
}
