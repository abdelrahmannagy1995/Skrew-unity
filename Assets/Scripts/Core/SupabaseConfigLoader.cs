using System;
using System.IO;
using UnityEngine;

namespace ScrewGame.Core
{
    /// <summary>
    /// Resolves Supabase URL + anon key at runtime.
    /// Resolution order (first non-empty wins):
    ///   1. Process environment variables SUPABASE_URL / SUPABASE_ANON_KEY.
    ///   2. <c>Application.streamingAssetsPath</c>/supabase.json
    ///      (shape: { "url": "...", "anonKey": "..." }).
    ///   3. The values supplied by the caller (typically the Inspector defaults).
    ///
    /// Never check the real key into source control — see
    /// <c>StreamingAssets/supabase.json.example</c>.
    /// </summary>
    public static class SupabaseConfigLoader
    {
        private const string EnvUrl     = "SUPABASE_URL";
        private const string EnvAnonKey = "SUPABASE_ANON_KEY";
        private const string FileName   = "supabase.json";

        public readonly struct Resolved
        {
            public readonly string Url;
            public readonly string AnonKey;
            public readonly string Source;

            public Resolved(string url, string anonKey, string source)
            {
                Url     = url;
                AnonKey = anonKey;
                Source  = source;
            }

            public bool IsValid => !string.IsNullOrWhiteSpace(Url) && !string.IsNullOrWhiteSpace(AnonKey);
        }

        public static Resolved Resolve(string fallbackUrl = null, string fallbackAnonKey = null)
        {
            // 1. Environment variables (works on desktop / CI; ignored on most mobile platforms).
            var envUrl = SafeGetEnv(EnvUrl);
            var envKey = SafeGetEnv(EnvAnonKey);
            if (!string.IsNullOrWhiteSpace(envUrl) && !string.IsNullOrWhiteSpace(envKey))
                return new Resolved(envUrl, envKey, "environment");

            // 2. StreamingAssets JSON.
            var fromFile = TryLoadFromStreamingAssets();
            if (fromFile.IsValid)
                return fromFile;

            // 3. Inspector / caller defaults.
            return new Resolved(fallbackUrl, fallbackAnonKey, "inspector");
        }

        private static string SafeGetEnv(string key)
        {
            try { return Environment.GetEnvironmentVariable(key); }
            catch { return null; }
        }

        private static Resolved TryLoadFromStreamingAssets()
        {
            try
            {
                var path = Path.Combine(Application.streamingAssetsPath, FileName);
                if (!File.Exists(path))
                    return default;

                var json    = File.ReadAllText(path);
                var payload = JsonUtility.FromJson<JsonShape>(json);
                if (payload == null)
                    return default;

                return new Resolved(payload.url, payload.anonKey, $"streamingAssets:{FileName}");
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[SupabaseConfigLoader] Failed to read {FileName}: {ex.Message}");
                return default;
            }
        }

        [Serializable]
        private class JsonShape
        {
            public string url;
            public string anonKey;
        }
    }
}
