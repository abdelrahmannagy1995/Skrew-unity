using System.Collections.Generic;
using ScrewGame.Entities;
using UnityEngine;

namespace ScrewGame.Core
{
    /// <summary>
    /// Responsible for building and filtering the deck based on the selected game mode.
    /// Deck composition:
    ///   General  – all cards (66+ cards)
    ///   Classic  – no Thief, no Ping/Pong
    ///   Thief    – no Ping/Pong, includes Thief
    ///   Doubles  – no Thief, includes Ping/Pong
    /// </summary>
    public static class DeckManager
    {
        // -----------------------------------------------------------------------
        // Deck building
        // -----------------------------------------------------------------------

        /// <summary>
        /// Returns a freshly built (unshuffled) deck for the given mode.
        /// The server performs the actual CSPRNG shuffle; this is used client-side
        /// for local simulation and AI determinization.
        /// </summary>
        public static List<CardData> BuildDeck(GameMode mode)
        {
            var deck = BuildBaseDeck();
            FilterByMode(deck, mode);
            return deck;
        }

        private static List<CardData> BuildBaseDeck()
        {
            var deck = new List<CardData>();

            // ---- Numerical cards: 1-9, 4 copies each (36 cards) ----
            for (int v = 1; v <= 9; v++)
            {
                for (int copy = 0; copy < 4; copy++)
                {
                    deck.Add(new CardData
                    {
                        CardKey  = $"num_{v}_{copy}",
                        CardType = CardType.Numerical,
                        Value    = v,
                    });
                }
            }

            // ---- Special numerical values ----
            // -1 x2
            for (int copy = 0; copy < 2; copy++)
                deck.Add(new CardData { CardKey = $"num_neg1_{copy}", CardType = CardType.Numerical, Value = -1 });

            // +20 x2
            for (int copy = 0; copy < 2; copy++)
                deck.Add(new CardData { CardKey = $"num_plus20_{copy}", CardType = CardType.Numerical, Value = 20 });

            // Green Screw = 0 points x2
            for (int copy = 0; copy < 2; copy++)
                deck.Add(new CardData { CardKey = $"green_screw_{copy}", CardType = CardType.Special, Value = 0, SpecialId = SpecialCardId.GreenScrew });

            // Red Screw = 25 points x2
            for (int copy = 0; copy < 2; copy++)
                deck.Add(new CardData { CardKey = $"red_screw_{copy}", CardType = CardType.Special, Value = 25, SpecialId = SpecialCardId.RedScrew });

            // ---- Command cards: 8 types x 2 copies = 16 cards ----
            var commandTypes = System.Enum.GetValues(typeof(CommandCardId)) as CommandCardId[];
            foreach (var cmdId in commandTypes)
            {
                for (int copy = 0; copy < 2; copy++)
                {
                    deck.Add(new CardData
                    {
                        CardKey   = $"cmd_{cmdId}_{copy}",
                        CardType  = CardType.Command,
                        Value     = 10, // endgame penalty
                        CommandId = cmdId,
                    });
                }
            }

            // ---- Special entity cards ----
            deck.Add(new CardData { CardKey = "special_thief", CardType = CardType.Special, Value = 0, SpecialId = SpecialCardId.Thief });
            deck.Add(new CardData { CardKey = "special_ping",  CardType = CardType.Special, Value = 0, SpecialId = SpecialCardId.Ping });
            deck.Add(new CardData { CardKey = "special_pong",  CardType = CardType.Special, Value = 0, SpecialId = SpecialCardId.Pong });

            // Extra numerical cards to reach 66 total (matching server-side deck-definitions)
            // 36 + 2 + 2 + 2 + 2 + 16 + 3 + 3 extra = 66
            int[] extraValues = { 0, 1, 2 };
            for (int i = 0; i < extraValues.Length; i++)
                deck.Add(new CardData { CardKey = $"num_extra_{extraValues[i]}_{i}", CardType = CardType.Numerical, Value = extraValues[i] });

            return deck; // 66 base cards (General mode)
        }

        private static void FilterByMode(List<CardData> deck, GameMode mode)
        {
            switch (mode)
            {
                case GameMode.Classic:
                    deck.RemoveAll(c =>
                        c.SpecialId == SpecialCardId.Thief ||
                        c.SpecialId == SpecialCardId.Ping  ||
                        c.SpecialId == SpecialCardId.Pong);
                    break;

                case GameMode.Thief:
                    deck.RemoveAll(c =>
                        c.SpecialId == SpecialCardId.Ping ||
                        c.SpecialId == SpecialCardId.Pong);
                    break;

                case GameMode.Doubles:
                    deck.RemoveAll(c => c.SpecialId == SpecialCardId.Thief);
                    break;

                case GameMode.General:
                default:
                    // All cards included
                    break;
            }
        }
    }

    /// <summary>The four supported game modes.</summary>
    public enum GameMode
    {
        General,  // سكرو العامة
        Classic,  // سكرو كلاسيك
        Thief,    // سكرو الحرامي
        Doubles,  // سكرو الثنائيات
    }
}
