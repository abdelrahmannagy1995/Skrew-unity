using System;
using UnityEngine;

namespace ScrewGame.Entities
{
    // =========================================================================
    // Card type enumerations
    // =========================================================================

    public enum CardType
    {
        Numerical,
        Command,
        Special,
    }

    public enum CommandCardId
    {
        PeekSelf,       // 7/8  – peek one of your own cards
        PeekOpponent,   // 9/10 – peek one of an opponent's cards
        Basra,          // البصرة – discard one of your own cards
        KhodWHat,       // خذ وهات – blind swap with an opponent
        KhodBas,        // خذ بس – give one of your cards to an opponent
        KaabDayer,      // كعب داير – see one card from every player OR two of your own
        AgabMaAgab,     // عجب ما عجب – see opponent's card and optionally swap
        AlaKefak,       // على كيفك – emulate any other command card
    }

    public enum SpecialCardId
    {
        None,
        Thief,       // الحرامي
        Ping,        // بينج
        Pong,        // بونج
        GreenScrew,  // Screw Driver – 0 points
        RedScrew,    // Red Screw    – 25 points
    }

    // =========================================================================
    // Card data (plain data class – serialised to/from JSON via Newtonsoft)
    // =========================================================================

    [Serializable]
    public class CardData
    {
        public string       CardKey;
        public CardType     CardType;
        public int          Value;
        public CommandCardId CommandId;
        public SpecialCardId SpecialId;

        /// <summary>Whether this card is currently revealed (face-up) to the local player.</summary>
        [NonSerialized] public bool IsRevealed;

        /// <summary>True if this card occupies a slot in the local player's grid.</summary>
        [NonSerialized] public bool InLocalGrid;
    }

    // =========================================================================
    // CardObject – MonoBehaviour that drives a single card prefab in the scene
    // =========================================================================

    /// <summary>
    /// Base class for all card visual representations.
    /// Attach to the "Card Token" prefab. Specialised command-card sub-prefabs
    /// inherit from this via <see cref="CommandCardObject"/> and <see cref="SpecialCardObject"/>.
    /// </summary>
    public class CardObject : MonoBehaviour
    {
        // -----------------------------------------------------------------------
        // Inspector references
        // -----------------------------------------------------------------------
        [Header("Visuals")]
        [SerializeField] protected SpriteRenderer _frontSprite;
        [SerializeField] protected SpriteRenderer _backSprite;

        // -----------------------------------------------------------------------
        // State
        // -----------------------------------------------------------------------
        public CardData Data { get; private set; }

        public int GridIndex { get; private set; }  // 0-3 position in owner's hand grid

        private bool _isFaceUp;

        // -----------------------------------------------------------------------
        // Initialisation
        // -----------------------------------------------------------------------
        public virtual void Initialise(CardData data, int gridIndex)
        {
            Data      = data;
            GridIndex = gridIndex;
            SetFaceDown();
        }

        // -----------------------------------------------------------------------
        // Flip methods
        // -----------------------------------------------------------------------

        public void SetFaceUp()
        {
            _isFaceUp = true;
            _frontSprite.enabled = true;
            _backSprite.enabled  = false;
            Data.IsRevealed      = true;
        }

        public void SetFaceDown()
        {
            _isFaceUp = false;
            _frontSprite.enabled = false;
            _backSprite.enabled  = true;
            Data.IsRevealed      = false;
        }

        public void ToggleFace()
        {
            if (_isFaceUp) SetFaceDown();
            else           SetFaceUp();
        }

        public bool IsFaceUp => _isFaceUp;

        // -----------------------------------------------------------------------
        // Override in specialised cards
        // -----------------------------------------------------------------------

        /// <summary>
        /// Called when this card is played as a command. Subclasses implement
        /// the specific UI workflow for their effect.
        /// </summary>
        public virtual void OnCommandActivated() { }
    }
}
