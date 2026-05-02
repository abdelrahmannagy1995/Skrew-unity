using System.Collections.Generic;
using TMPro;
using UnityEngine;
using ScrewGame.Entities;

namespace ScrewGame.UI
{
    /// <summary>
    /// Companion component for CardObject.
    /// Reads CardData after Initialise() and updates the card's face
    /// text (value or command/special name) and tint colour on the
    /// face SpriteRenderer.
    /// </summary>
    [RequireComponent(typeof(CardObject))]
    public class CardVisualController : MonoBehaviour
    {
        // ─── Inspector ───────────────────────────────────────────
        [Header("Face Text (World-Space Canvas children)")]
        [SerializeField] private TextMeshPro _valueText;      // Large centre number
        [SerializeField] private TextMeshPro _labelText;      // Arabic card name
        [SerializeField] private TextMeshPro _cornerValueTL;  // Top-left mini value
        [SerializeField] private TextMeshPro _cornerValueBR;  // Bottom-right mini value (rotated 180°)

        [Header("Sprites – assigned per card type")]
        [SerializeField] private Sprite _numericalSprite;
        [SerializeField] private Sprite _cmdPeekSelfSprite;
        [SerializeField] private Sprite _cmdPeekOpponentSprite;
        [SerializeField] private Sprite _cmdBasraSprite;
        [SerializeField] private Sprite _cmdKhodWHatSprite;
        [SerializeField] private Sprite _cmdKhodBasSprite;
        [SerializeField] private Sprite _cmdKaabDayerSprite;
        [SerializeField] private Sprite _cmdAgabMaAgabSprite;
        [SerializeField] private Sprite _cmdAlaKefakSprite;
        [SerializeField] private Sprite _spcThiefSprite;
        [SerializeField] private Sprite _spcPingSprite;
        [SerializeField] private Sprite _spcPongSprite;
        [SerializeField] private Sprite _spcGreenScrewSprite;
        [SerializeField] private Sprite _spcRedScrewSprite;

        // ─── Arabic label tables ──────────────────────────────────
        private static readonly Dictionary<CommandCardId, string> _cmdLabels = new()
        {
            { CommandCardId.PeekSelf,      "تلحق نفسك"   },
            { CommandCardId.PeekOpponent,  "تلحق خصمك"   },
            { CommandCardId.Basra,         "بصرة"         },
            { CommandCardId.KhodWHat,      "خذ وحط"       },
            { CommandCardId.KhodBas,       "خذ بس"        },
            { CommandCardId.KaabDayer,     "كعب دائر"     },
            { CommandCardId.AgabMaAgab,    "عجب ما عجب"   },
            { CommandCardId.AlaKefak,      "على كيفك"     },
        };

        private static readonly Dictionary<SpecialCardId, string> _spcLabels = new()
        {
            { SpecialCardId.Thief,       "سارق"        },
            { SpecialCardId.Ping,        "بينج"         },
            { SpecialCardId.Pong,        "بونج"         },
            { SpecialCardId.GreenScrew,  "سكرو أخضر"   },
            { SpecialCardId.RedScrew,    "سكرو أحمر"   },
        };

        // ─── Runtime ─────────────────────────────────────────────
        private CardObject _card;
        private SpriteRenderer _frontRenderer;

        private void Awake()
        {
            _card = GetComponent<CardObject>();
            // _frontSprite is protected in CardObject; find the child named "Front"
            Transform frontT = transform.Find("Front");
            if (frontT != null) _frontRenderer = frontT.GetComponent<SpriteRenderer>();
        }

        /// <summary>Call this right after CardObject.Initialise().</summary>
        public void Refresh()
        {
            if (_card == null || _card.Data == null) return;
            CardData d = _card.Data;

            switch (d.CardType)
            {
                case CardType.Numerical:
                    ApplyNumerical(d.Value);
                    break;
                case CardType.Command:
                    ApplyCommand(d.CommandId);
                    break;
                case CardType.Special:
                    ApplySpecial(d.SpecialId);
                    break;
            }
        }

        // ─── Private helpers ─────────────────────────────────────
        private void ApplyNumerical(int value)
        {
            SetFrontSprite(_numericalSprite);
            string v = value.ToString();
            SetTexts(v, "", v, v);
        }

        private void ApplyCommand(CommandCardId id)
        {
            SetFrontSprite(CommandSprite(id));
            string label = _cmdLabels.TryGetValue(id, out string l) ? l : id.ToString();
            string sym   = CommandSymbol(id);
            SetTexts(sym, label, sym, sym);
        }

        private void ApplySpecial(SpecialCardId id)
        {
            SetFrontSprite(SpecialSprite(id));
            string label = _spcLabels.TryGetValue(id, out string l) ? l : id.ToString();
            SetTexts("★", label, "★", "★");
        }

        private void SetFrontSprite(Sprite s)
        {
            if (_frontRenderer != null && s != null) _frontRenderer.sprite = s;
        }

        private void SetTexts(string centre, string name, string tl, string br)
        {
            if (_valueText     != null) { _valueText.text     = centre; }
            if (_labelText     != null) { _labelText.text     = name;   }
            if (_cornerValueTL != null) { _cornerValueTL.text = tl;     }
            if (_cornerValueBR != null) { _cornerValueBR.text = br;     }
        }

        private static string CommandSymbol(CommandCardId id)
        {
            switch (id)
            {
                case CommandCardId.PeekSelf:     return "👁";
                case CommandCardId.PeekOpponent: return "🔍";
                case CommandCardId.Basra:        return "💥";
                case CommandCardId.KhodWHat:     return "↔";
                case CommandCardId.KhodBas:      return "→";
                case CommandCardId.KaabDayer:    return "🎲";
                case CommandCardId.AgabMaAgab:   return "⟳";
                case CommandCardId.AlaKefak:     return "✦";
                default: return "?";
            }
        }

        // ─── Sprite lookup helpers ────────────────────────────────
        private Sprite CommandSprite(CommandCardId id)
        {
            switch (id)
            {
                case CommandCardId.PeekSelf:     return _cmdPeekSelfSprite;
                case CommandCardId.PeekOpponent: return _cmdPeekOpponentSprite;
                case CommandCardId.Basra:        return _cmdBasraSprite;
                case CommandCardId.KhodWHat:     return _cmdKhodWHatSprite;
                case CommandCardId.KhodBas:      return _cmdKhodBasSprite;
                case CommandCardId.KaabDayer:    return _cmdKaabDayerSprite;
                case CommandCardId.AgabMaAgab:   return _cmdAgabMaAgabSprite;
                case CommandCardId.AlaKefak:     return _cmdAlaKefakSprite;
                default: return null;
            }
        }

        private Sprite SpecialSprite(SpecialCardId id)
        {
            switch (id)
            {
                case SpecialCardId.Thief:      return _spcThiefSprite;
                case SpecialCardId.Ping:       return _spcPingSprite;
                case SpecialCardId.Pong:       return _spcPongSprite;
                case SpecialCardId.GreenScrew: return _spcGreenScrewSprite;
                case SpecialCardId.RedScrew:   return _spcRedScrewSprite;
                default: return null;
            }
        }
    }
}
