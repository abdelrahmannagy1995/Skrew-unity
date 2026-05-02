using UnityEngine;

namespace ScrewGame.UI
{
    public class AllGridsRevealController : MonoBehaviour
    {
        public static AllGridsRevealController Instance { get; private set; }
        private void Awake() { Instance = this; }
        public void RevealAll() { /* flip all card objects to face-up */ }
    }
}
