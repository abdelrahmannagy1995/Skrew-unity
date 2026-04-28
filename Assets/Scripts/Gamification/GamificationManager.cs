using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using Postgrest.Models;
using UnityEngine;

namespace ScrewGame.Gamification
{
    // =========================================================================
    // Models
    // =========================================================================

    [Serializable]
    public class UserProfile : BaseModel
    {
        [JsonProperty("id")]              public string Id            { get; set; }
        [JsonProperty("username")]        public string Username      { get; set; }
        [JsonProperty("display_name")]    public string DisplayName   { get; set; }
        [JsonProperty("coins")]           public int    Coins         { get; set; }
        [JsonProperty("elo_rating")]      public int    EloRating     { get; set; }
        [JsonProperty("streak_count")]    public int    StreakCount   { get; set; }
        [JsonProperty("last_login_date")] public string LastLoginDate { get; set; }
    }

    [Serializable]
    public class Mission : BaseModel
    {
        [JsonProperty("id")]              public string Id           { get; set; }
        [JsonProperty("title_en")]        public string TitleEn      { get; set; }
        [JsonProperty("title_ar")]        public string TitleAr      { get; set; }
        [JsonProperty("description_en")] public string DescEn       { get; set; }
        [JsonProperty("description_ar")] public string DescAr       { get; set; }
        [JsonProperty("period")]          public string Period       { get; set; }
        [JsonProperty("target_count")]    public int    TargetCount  { get; set; }
        [JsonProperty("coin_reward")]     public int    CoinReward   { get; set; }
    }

    [Serializable]
    public class UserMission : BaseModel
    {
        [JsonProperty("id")]           public string Id          { get; set; }
        [JsonProperty("mission_id")]   public string MissionId   { get; set; }
        [JsonProperty("progress")]     public int    Progress    { get; set; }
        [JsonProperty("completed")]    public bool   Completed   { get; set; }
    }

    [Serializable]
    public class LeaderboardEntry : BaseModel
    {
        [JsonProperty("id")]           public string Id          { get; set; }
        [JsonProperty("username")]     public string Username    { get; set; }
        [JsonProperty("elo_rating")]   public int    EloRating   { get; set; }
        [JsonProperty("total_wins")]   public int    TotalWins   { get; set; }
        [JsonProperty("rank")]         public long   Rank        { get; set; }
    }

    // =========================================================================
    // GamificationManager
    // =========================================================================

    /// <summary>
    /// Handles daily streaks, mission progress, badge unlocking, and leaderboard queries.
    /// </summary>
    public class GamificationManager : MonoBehaviour
    {
        public static GamificationManager Instance { get; private set; }

        private UserProfile _cachedProfile;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        // -----------------------------------------------------------------------
        // Streak update (call on login)
        // -----------------------------------------------------------------------

        public async UniTask UpdateStreakAsync(CancellationToken ct = default)
        {
            var profile = await GetProfileAsync(ct);
            if (profile == null) return;

            var today = DateTime.UtcNow.Date;
            DateTime? lastLogin = null;
            if (!string.IsNullOrEmpty(profile.LastLoginDate))
                lastLogin = DateTime.Parse(profile.LastLoginDate).Date;

            int newStreak = profile.StreakCount;

            if (lastLogin == null || lastLogin < today.AddDays(-1))
                newStreak = 1;                   // reset streak
            else if (lastLogin < today)
                newStreak = profile.StreakCount + 1;  // consecutive day
            // else: same day login, no change

            int coinBonus = CalculateStreakBonus(newStreak);

            var client = Core.SupabaseManager.Instance.Client;
            await client.From<UserProfile>()
                .Filter("auth_id", Postgrest.Constants.Operator.Equals, Core.SupabaseManager.Instance.CurrentUserId)
                .Update(new UserProfile
                {
                    StreakCount   = newStreak,
                    LastLoginDate = today.ToString("yyyy-MM-dd"),
                    Coins         = profile.Coins + coinBonus,
                });

            if (coinBonus > 0)
                Debug.Log($"[Gamification] Streak day {newStreak} – awarded {coinBonus} bonus coins.");
        }

        private static int CalculateStreakBonus(int streakDay)
        {
            // Increasing daily streak reward: 10, 20, 30, 50, 75, 100…
            return streakDay switch
            {
                1     => 10,
                2     => 20,
                3     => 30,
                4     => 50,
                5     => 75,
                var d when d >= 6 => 100,
                _     => 0,
            };
        }

        // -----------------------------------------------------------------------
        // Profile
        // -----------------------------------------------------------------------

        public async UniTask<UserProfile> GetProfileAsync(CancellationToken ct = default)
        {
            if (_cachedProfile != null) return _cachedProfile;

            var client = Core.SupabaseManager.Instance.Client;
            var result = await client.From<UserProfile>()
                .Filter("auth_id", Postgrest.Constants.Operator.Equals, Core.SupabaseManager.Instance.CurrentUserId)
                .Single();

            _cachedProfile = result;
            return result;
        }

        public void InvalidateProfileCache() => _cachedProfile = null;

        // -----------------------------------------------------------------------
        // Missions
        // -----------------------------------------------------------------------

        public async UniTask<List<UserMission>> GetActiveMissionsAsync(string period, CancellationToken ct = default)
        {
            var client = Core.SupabaseManager.Instance.Client;
            var result = await client.From<UserMission>()
                .Filter("user_id", Postgrest.Constants.Operator.Equals,
                    (await GetProfileAsync(ct))?.Id)
                .Get();
            return result?.Models ?? new List<UserMission>();
        }

        // -----------------------------------------------------------------------
        // Leaderboard (Classic mode, ELO-ranked)
        // -----------------------------------------------------------------------

        public async UniTask<List<LeaderboardEntry>> GetLeaderboardAsync(int limit = 50, CancellationToken ct = default)
        {
            var client = Core.SupabaseManager.Instance.Client;
            var result = await client
                .From<LeaderboardEntry>("leaderboard_classic")
                .Select("*")
                .Limit(limit)
                .Get();
            return result?.Models ?? new List<LeaderboardEntry>();
        }
    }
}
