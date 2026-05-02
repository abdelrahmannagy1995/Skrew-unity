using UnityEngine;

namespace ScrewGame.UI
{
    public class ThiefGuessModal : MonoBehaviour
    {
        public static ThiefGuessModal Instance { get; private set; }
        private void Awake() { Instance = this; }
        public void Show() { gameObject.SetActive(true); }
        public void Hide() { gameObject.SetActive(false); }
    }
}
