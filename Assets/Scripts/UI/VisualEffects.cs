using System.Collections;
using Unity.Cinemachine;
using UnityEngine;

namespace ScrewGame.UI
{
    /// <summary>
    /// Handles Basra impact effects, screen shake, and particle bursts.
    /// Uses Cinemachine for screen shake and Unity ParticleSystem for burst FX.
    /// </summary>
    public class VisualEffects : MonoBehaviour
    {
        public static VisualEffects Instance { get; private set; }

        [SerializeField] private ParticleSystem        _basraParticleBurst;
        [SerializeField] private CinemachineCamera     _gameCamera;
        [SerializeField] private float                 _shakeAmplitude = 2f;
        [SerializeField] private float                 _shakeDuration  = 0.3f;

        private CinemachineBasicMultiChannelPerlin _noise;

        private void Awake()
        {
            Instance = this;
            if (_gameCamera != null)
                _noise = _gameCamera.GetComponent<CinemachineBasicMultiChannelPerlin>();
        }

        public void PlayBasraSuccess(string playerId)
        {
            if (_basraParticleBurst != null) _basraParticleBurst.Play();
            StartCoroutine(ShakeRoutine());
        }

        public void PlayBasraFailure(string playerId)
        {
            // Subtle red flash – handled by shader / UI overlay
        }

        private IEnumerator ShakeRoutine()
        {
            if (_noise == null) yield break;
            _noise.AmplitudeGain = _shakeAmplitude;
            yield return new WaitForSeconds(_shakeDuration);
            _noise.AmplitudeGain = 0f;
        }
    }
}
