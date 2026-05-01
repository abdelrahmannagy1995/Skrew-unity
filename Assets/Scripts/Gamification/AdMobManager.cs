using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using GoogleMobileAds.Api;
using UnityEngine;

namespace ScrewGame.Gamification
{
    /// <summary>
    /// Manages Google AdMob rewarded video ads.
    /// Client-side: shows the ad and passes user_id in ServerSideVerificationOptions.
    /// Server-side: the admob-ssv Edge Function verifies the ECDSA signature and awards coins.
    /// </summary>
    public class AdMobManager : MonoBehaviour
    {
        // -----------------------------------------------------------------------
        // Inspector
        // -----------------------------------------------------------------------
        [Header("AdMob Configuration")]
        [SerializeField] private string _rewardedAdUnitId = "ca-app-pub-XXXXXXXX/YYYYYYYY";

        // -----------------------------------------------------------------------
        // Singleton
        // -----------------------------------------------------------------------
        public static AdMobManager Instance { get; private set; }

        // -----------------------------------------------------------------------
        // State
        // -----------------------------------------------------------------------
        private RewardedAd _rewardedAd;
        public bool IsAdLoaded => _rewardedAd != null;

        public event Action<int> OnRewardGranted;    // coin amount
        public event Action      OnAdFailedToLoad;

        // -----------------------------------------------------------------------
        // Unity lifecycle
        // -----------------------------------------------------------------------
        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            MobileAds.Initialize(_ =>
            {
                Debug.Log("[AdMob] Initialised.");
                LoadRewardedAd();
            });
        }

        // -----------------------------------------------------------------------
        // Load
        // -----------------------------------------------------------------------

        public void LoadRewardedAd()
        {
            _rewardedAd?.Destroy();
            _rewardedAd = null;

            var request = new AdRequest();
            RewardedAd.Load(_rewardedAdUnitId, request, OnAdLoaded);
        }

        private void OnAdLoaded(RewardedAd ad, LoadAdError error)
        {
            if (error != null || ad == null)
            {
                Debug.LogWarning($"[AdMob] Failed to load rewarded ad: {error?.GetMessage()}");
                OnAdFailedToLoad?.Invoke();
                return;
            }

            _rewardedAd = ad;
            Debug.Log("[AdMob] Rewarded ad loaded.");

            // Register callbacks
            _rewardedAd.OnAdPaid         += OnPaid;
            _rewardedAd.OnAdFailedToShow += OnFailedToShow;
            _rewardedAd.OnAdClosed       += OnAdClosed;
        }

        // -----------------------------------------------------------------------
        // Show
        // -----------------------------------------------------------------------

        public void ShowRewardedAd()
        {
            if (_rewardedAd == null || !_rewardedAd.CanShowAd())
            {
                Debug.LogWarning("[AdMob] No ad available.");
                return;
            }

            // Pass user_id in custom_data for SSV
            string userId = Core.SupabaseManager.Instance.CurrentUserId ?? "unknown";
            var ssvOptions = new ServerSideVerificationOptions
                .Builder()
                .SetUserId(userId)
                .SetCustomData($"{{\"user_id\":\"{userId}\"}}")
                .Build();

            _rewardedAd.SetServerSideVerificationOptions(ssvOptions);

            _rewardedAd.Show(reward =>
            {
                Debug.Log($"[AdMob] Reward granted: {reward.Amount} {reward.Type}");
                // Actual coin deposit is handled server-side by admob-ssv Edge Function
                OnRewardGranted?.Invoke((int)reward.Amount);
            });
        }

        // -----------------------------------------------------------------------
        // Callbacks
        // -----------------------------------------------------------------------

        private void OnPaid(AdValue adValue)
        {
            Debug.Log($"[AdMob] Ad paid: {adValue.Value} {adValue.CurrencyCode}");
        }

        private void OnFailedToShow(AdError error)
        {
            Debug.LogWarning($"[AdMob] Ad failed to show: {error.GetMessage()}");
        }

        private void OnAdClosed()
        {
            Debug.Log("[AdMob] Ad closed – loading next ad.");
            LoadRewardedAd();
        }
    }
}
