using System;
using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using ScrewGame.Core;
using ScrewGame.UI;
using UnityEngine;

namespace ScrewGame.Entities
{
    /// <summary>
    /// Specialised MonoBehaviour for command-card prefabs.
    /// Each CommandCardId variant overrides <see cref="OnCommandActivated"/> to
    /// trigger the appropriate UI workflow and Edge Function call.
    /// </summary>
    public class CommandCardObject : CardObject
    {
        public override void OnCommandActivated()
        {
            if (Data?.CommandId == null) return;

            switch (Data.CommandId)
            {
                case CommandCardId.PeekSelf:
                    StartCoroutine(PeekSelfRoutine());
                    break;

                case CommandCardId.PeekOpponent:
                    CommandUI.Instance.RequestOpponentCardSelection(OnOpponentCardSelected);
                    break;

                case CommandCardId.Basra:
                    CommandUI.Instance.RequestOwnCardSelection(OnBasraSelfCardSelected);
                    break;

                case CommandCardId.KhodWHat:
                    CommandUI.Instance.RequestOwnAndOpponentCardSelection(OnKhodWHatSelected);
                    break;

                case CommandCardId.KhodBas:
                    CommandUI.Instance.RequestOwnCardAndOpponentSelection(OnKhodBasSelected);
                    break;

                case CommandCardId.KaabDayer:
                    CommandUI.Instance.ShowKaabDayerChoice(OnKaabDayerChoiceMade);
                    break;

                case CommandCardId.AgabMaAgab:
                    CommandUI.Instance.RequestOpponentCardSelection(OnAgabMaAgabOpponentSelected);
                    break;

                case CommandCardId.AlaKefak:
                    CommandUI.Instance.ShowCommandCardChoice(OnAlaKefakCommandChosen);
                    break;
            }
        }

        // -----------------------------------------------------------------------
        // PeekSelf – reveal one of your own cards for 3 seconds
        // -----------------------------------------------------------------------
        private IEnumerator PeekSelfRoutine()
        {
            CommandUI.Instance.RequestOwnCardSelection(cardIndex =>
            {
                StartCoroutine(RevealTemporarilyRoutine(cardIndex, 3f));
            });
            yield break;
        }

        private IEnumerator RevealTemporarilyRoutine(int cardIndex, float duration)
        {
            var grid = PlayerGrid.LocalInstance;
            grid.RevealCard(cardIndex);
            yield return new WaitForSeconds(duration);
            grid.HideCard(cardIndex);
        }

        // -----------------------------------------------------------------------
        // PeekOpponent – reveal one card belonging to a chosen opponent for 3 seconds
        // -----------------------------------------------------------------------
        private void OnOpponentCardSelected(string opponentId, int cardIndex)
        {
            StartCoroutine(RevealOpponentCardRoutine(opponentId, cardIndex, 3f));
        }

        private IEnumerator RevealOpponentCardRoutine(string opponentId, int cardIndex, float duration)
        {
            // Request the server to push the card value privately to this client
            GameLoop.Instance.PeekOpponentCardAsync(opponentId, cardIndex).Forget();

            // The broadcast event will trigger the reveal on OpponentGrid; hide after duration
            yield return new WaitForSeconds(duration);
            OpponentGridManager.Instance.HideCard(opponentId, cardIndex);
        }

        // -----------------------------------------------------------------------
        // Basra (Discard Self) – permanently discard one of your own cards
        // -----------------------------------------------------------------------
        private void OnBasraSelfCardSelected(int cardIndex)
        {
            GameLoop.Instance.DiscardOwnCardAsync(cardIndex).Forget();
        }

        // -----------------------------------------------------------------------
        // Khod w Hat (Blind Swap) – swap one of your cards with an opponent's
        // -----------------------------------------------------------------------
        private void OnKhodWHatSelected(int ownIndex, string opponentId, int oppIndex)
        {
            GameLoop.Instance.BlindSwapAsync(ownIndex, opponentId, oppIndex).Forget();
        }

        // -----------------------------------------------------------------------
        // Khod Bas (Give Only) – force one of your cards into an opponent's grid
        // -----------------------------------------------------------------------
        private void OnKhodBasSelected(int ownIndex, string targetOpponentId)
        {
            GameLoop.Instance.GiveCardToOpponentAsync(ownIndex, targetOpponentId).Forget();
        }

        // -----------------------------------------------------------------------
        // Kaab Dayer (Spin) – see one card from every player OR two of your own
        // -----------------------------------------------------------------------
        private void OnKaabDayerChoiceMade(bool seeAllPlayers)
        {
            if (seeAllPlayers)
                GameLoop.Instance.KaabDayerAllPlayersAsync().Forget();
            else
                StartCoroutine(KaabDayerTwoOwnCardsRoutine());
        }

        private IEnumerator KaabDayerTwoOwnCardsRoutine()
        {
            var grid = PlayerGrid.LocalInstance;
            // First card selection
            CommandUI.Instance.RequestOwnCardSelection(first =>
            {
                grid.RevealCard(first);
                // Second card selection
                CommandUI.Instance.RequestOwnCardSelection(second =>
                {
                    grid.RevealCard(second);
                    StartCoroutine(HideTwoCardsRoutine(grid, first, second, 3f));
                });
            });
            yield break;
        }

        private IEnumerator HideTwoCardsRoutine(PlayerGrid grid, int a, int b, float delay)
        {
            yield return new WaitForSeconds(delay);
            grid.HideCard(a);
            grid.HideCard(b);
        }

        // -----------------------------------------------------------------------
        // 3agab ma 3agab – see opponent's card; optionally swap
        // -----------------------------------------------------------------------
        private void OnAgabMaAgabOpponentSelected(string opponentId, int cardIndex)
        {
            GameLoop.Instance.AgabMaAgabAsync(opponentId, cardIndex).Forget();
        }

        // -----------------------------------------------------------------------
        // 3ala Kefak (Wildcard) – choose any command to emulate
        // -----------------------------------------------------------------------
        private void OnAlaKefakCommandChosen(CommandCardId chosenCommand)
        {
            // Update this card's data to the chosen command and activate directly.
            // Do NOT create a temporary object – its coroutines would be destroyed prematurely.
            var emulatedData = new CardData { CardType = CardType.Command, CommandId = chosenCommand };
            // Temporarily swap data and activate
            var original = Data;
            Initialise(emulatedData, GridIndex);
            OnCommandActivated();
            // Restore original data after activation (command effect is already in flight)
            Initialise(original, GridIndex);
        }
    }
}
