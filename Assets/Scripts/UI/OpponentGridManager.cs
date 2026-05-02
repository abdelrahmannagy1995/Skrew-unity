using UnityEngine;

namespace ScrewGame.UI
{
    public class OpponentGridManager : MonoBehaviour
    {
        public static OpponentGridManager Instance { get; private set; }
        private void Awake() { Instance = this; }
        public void HideCard(string opponentId, int cardIndex) { }
    }
}
