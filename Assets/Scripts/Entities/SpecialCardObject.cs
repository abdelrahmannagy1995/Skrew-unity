using System.Collections;
using ScrewGame.Core;
using UnityEngine;

namespace ScrewGame.Entities
{
    /// <summary>
    /// Specialised card object for the Thief and Ping/Pong cards.
    /// </summary>
    public class SpecialCardObject : CardObject
    {
        public override void OnCommandActivated()
        {
            switch (Data.SpecialId)
            {
                case SpecialCardId.Thief:
                    OnThiefDrawn();
                    break;

                case SpecialCardId.Ping:
                case SpecialCardId.Pong:
                    OnPingPongPlayed();
                    break;

                case SpecialCardId.RedScrew:
                    OnRedScrewDiscarded();
                    break;

                case SpecialCardId.GreenScrew:
                    // Green Screw is just a 0-point card; no active effect when discarded
                    break;
            }
        }

        // -----------------------------------------------------------------------
        // Thief
        // -----------------------------------------------------------------------
        private void OnThiefDrawn()
        {
            // Thief CANNOT be discarded – server enforces mandatory grid swap.
            // Display an informational popup to the local player.
            CommandUI.Instance.ShowThiefNotification();

            // Force swap into the player's grid (UI prompts slot selection)
            CommandUI.Instance.RequestOwnCardSelection(slotIndex =>
            {
                GameLoop.Instance.ForceSwapThiefAsync(slotIndex).Forget();
            });
        }

        // -----------------------------------------------------------------------
        // Ping / Pong
        // -----------------------------------------------------------------------
        private void OnPingPongPlayed()
        {
            // Playing Ping or Pong broadcasts a skip request for the opposing team.
            // Opponents can block via Basra mechanic if they hold a matching card.
            GameLoop.Instance.PlayPingPongAsync(Data.SpecialId).Forget();
        }

        // -----------------------------------------------------------------------
        // Red Screw discarded on top of Green Screw
        // -----------------------------------------------------------------------
        private void OnRedScrewDiscarded()
        {
            // The server checks whether the discard_top_card is a GreenScrew.
            // If so, it sets a flag that burns the GreenScrew and disables snatch for 1 turn.
            // The client only needs to trigger the visual burn effect.
            StartCoroutine(BurnEffectRoutine());
        }

        private IEnumerator BurnEffectRoutine()
        {
            // Particle effect placeholder – attach a ParticleSystem in the prefab
            var ps = GetComponentInChildren<ParticleSystem>();
            if (ps != null)
            {
                ps.Play();
                yield return new WaitForSeconds(ps.main.duration);
            }
        }
    }
}
