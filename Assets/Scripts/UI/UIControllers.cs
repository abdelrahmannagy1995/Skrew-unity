using DG.Tweening;
using ScrewGame.Entities;
using UnityEngine;

namespace ScrewGame.UI
{
    /// <summary>
    /// Manages the visual 4-card grid for the local player.
    /// Cards are rendered face-down initially; Peek phase reveals indices 2 &amp; 3.
    /// </summary>
    public class PlayerGrid : MonoBehaviour
    {
        public static PlayerGrid LocalInstance { get; private set; }

        [SerializeField] private CardObject[] _cardObjects = new CardObject[4];

        private void Awake() { LocalInstance = this; }

        public void RevealCard(int index)
        {
            if (index < 0 || index >= _cardObjects.Length) return;
            _cardObjects[index]?.SetFaceUp();
        }

        public void HideCard(int index)
        {
            if (index < 0 || index >= _cardObjects.Length) return;
            _cardObjects[index]?.SetFaceDown();
        }

        public void AnimateCardDeal(int index, Vector3 fromPosition)
        {
            if (index < 0 || index >= _cardObjects.Length) return;
            var card = _cardObjects[index];
            if (card == null) return;
            card.transform.position = fromPosition;
            card.transform.DOLocalMove(Vector3.zero, 0.4f).SetEase(Ease.OutBack);
        }

        public void AnimateSwap(int fromIndex, int toIndex)
        {
            if (fromIndex < 0 || fromIndex >= _cardObjects.Length) return;
            if (toIndex   < 0 || toIndex   >= _cardObjects.Length) return;
            var a = _cardObjects[fromIndex].transform;
            var b = _cardObjects[toIndex].transform;
            var aPos = a.position; var bPos = b.position;
            a.DOMove(bPos, 0.3f).SetEase(Ease.InOutSine);
            b.DOMove(aPos, 0.3f).SetEase(Ease.InOutSine);
        }
    }
}
