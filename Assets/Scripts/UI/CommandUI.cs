using System;
using ScrewGame.Entities;
using UnityEngine;

namespace ScrewGame.UI
{
    /// <summary>UI helper for command-card interaction flows.</summary>
    public class CommandUI : MonoBehaviour
    {
        public static CommandUI Instance { get; private set; }
        private void Awake() { Instance = this; }

        public void RequestOwnCardSelection(Action<int> callback) { }
        public void RequestOpponentCardSelection(Action<string, int> callback) { }
        public void RequestOwnAndOpponentCardSelection(Action<int, string, int> callback) { }
        public void RequestOwnCardAndOpponentSelection(Action<int, string> callback) { }
        public void ShowKaabDayerChoice(Action<bool> callback) { }
        public void ShowCommandCardChoice(Action<CommandCardId> callback) { }
        public void ShowThiefNotification() { }
    }
}
