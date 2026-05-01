// Compile-time stubs for Google Mobile Ads Unity Plugin.
// Define ADMOB_REAL to disable when the real plugin is imported.
#if !ADMOB_REAL
#pragma warning disable CS0067
using System;

namespace GoogleMobileAds.Api
{
    public static class MobileAds
    {
        public static void Initialize(Action<object> onComplete)
        {
            // No-op stub – pretend init succeeded immediately.
            onComplete?.Invoke(null);
        }
    }

    public class AdRequest { }

    public class LoadAdError
    {
        public string GetMessage() => "stub: AdMob not installed";
    }

    public class AdError
    {
        public string GetMessage() => "stub: AdMob not installed";
    }

    public class AdValue
    {
        public double Value         { get; set; }
        public string CurrencyCode  { get; set; }
    }

    public class Reward
    {
        public double Amount { get; set; }
        public string Type   { get; set; }
    }

    public class ServerSideVerificationOptions
    {
        public class Builder
        {
            public Builder SetUserId(string id)         => this;
            public Builder SetCustomData(string data)   => this;
            public ServerSideVerificationOptions Build() => new ServerSideVerificationOptions();
        }
    }

    public class RewardedAd
    {
        public event Action<AdValue> OnAdPaid;
        public event Action<AdError> OnAdFailedToShow;
        public event Action          OnAdClosed;

        public static void Load(string adUnitId, AdRequest request, Action<RewardedAd, LoadAdError> onLoaded)
        {
            // Stub: report failure so callers don't dereference a fake ad.
            onLoaded?.Invoke(null, new LoadAdError());
        }

        public bool CanShowAd() => false;
        public void Show(Action<Reward> onRewarded) { }
        public void SetServerSideVerificationOptions(ServerSideVerificationOptions opts) { }
        public void Destroy() { }
    }
}
#pragma warning restore CS0067
#endif
