using UnityEngine;
using ScrewGame.Core;
using ScrewGame.Entities;

namespace ScrewGame.UI
{
    /// <summary>
    /// Attach to each CardObject in the local player's grid.
    /// Handles tap/click to swap the drawn card into this slot.
    /// Highlights when the slot is a valid swap target.
    /// </summary>
    [RequireComponent(typeof(CardObject))]
    public class CardClickHandler : MonoBehaviour
    {
        [SerializeField] private int _gridIndex;            // 0-3
        [SerializeField] private SpriteRenderer _highlight; // optional yellow ring

        private CardObject _card;
        private bool _selectable;

        private void Awake()  => _card = GetComponent<CardObject>();

        private void Start()
        {
            // Subscribe to simulator events
            if (LocalGameSimulator.Instance != null)
            {
                LocalGameSimulator.Instance.OnTurnChanged += OnTurnChanged;
            }
        }

        private void OnDestroy()
        {
            if (LocalGameSimulator.Instance != null)
                LocalGameSimulator.Instance.OnTurnChanged -= OnTurnChanged;
        }

        private void OnTurnChanged(int seat)
        {
            SetSelectable(false); // reset on each turn change
        }

        // Called by GameplayInputController after Draw / Snatch
        public void SetSelectable(bool on)
        {
            _selectable = on;
            if (_highlight != null) _highlight.enabled = on;
        }

        public int GridIndex => _gridIndex;

        // ─── Mouse / touch input ─────────────────────────────────────────────
        private void OnMouseDown()
        {
            if (!_selectable) return;
            var sim = LocalGameSimulator.Instance;
            if (sim == null || !sim.HasDrawnCard) return;

            bool swapped = sim.TrySwapIntoSlot(_gridIndex);
            if (swapped)
            {
                // Update visual
                CardData newData = sim.GetLocalCard(_gridIndex);
                _card.Initialise(newData, _gridIndex);
                GetComponent<CardVisualController>()?.Refresh();
                _card.SetFaceDown();

                // Disable all highlights
                GameplayInputController.Instance?.ClearCardHighlights();
            }
        }
    }
}
