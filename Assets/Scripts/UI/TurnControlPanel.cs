using UnityEngine;
using UnityEngine.UI;

namespace ScrewGame.UI
{
    public class TurnControlPanel : MonoBehaviour
    {
        public static TurnControlPanel Instance { get; private set; }

        [SerializeField] private Button _drawButton;
        [SerializeField] private Button _snatchButton;
        [SerializeField] private Button _screwButton;

        private void Awake()
        {
            Instance = this;
            SetInteractable(false);
        }

        public void SetInteractable(bool interactable)
        {
            if (_drawButton   != null) _drawButton.interactable   = interactable;
            if (_snatchButton != null) _snatchButton.interactable = interactable;
            if (_screwButton  != null) _screwButton.interactable  = interactable;
        }
    }
}
