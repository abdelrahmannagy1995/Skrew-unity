using UnityEngine;

namespace ScrewGame.UI
{
    public class ScoreboardUI : MonoBehaviour
    {
        public static ScoreboardUI Instance { get; private set; }
        private void Awake() { Instance = this; }
        public void Show(object scores, string winnerId) { gameObject.SetActive(true); }
    }
}
