using System;
using System.IO;
using UnityEngine;

namespace ScrewGame.Core
{
    /// <summary>
    /// Resolves Supabase URL + anon key at runtime.
    /// Resolution order (first non-empty wins):
    ///   1. Process environment variables SUPABASE_URL / SUPABASE_ANON_KEY.
    ///   2. <c>.env</c> file at the project root (Editor / desktop standalone only).
    ///   3. <c>Application.streamingAssetsPath</c>/supabase.json
    ///      (shape: { "url": "...", "anonKey": "..." }).
    ///   4. The values supplied by the caller (typically the Inspector defaults).
    ///
    /// Never check the real key into source control — see
    /// <c>.env.example</c> and <c>StreamingAssets/supabase.json.example</c>.
    /// </summary>
    public static class SupabaseConfigLoader
    {
        private const string EnvUrl     = "SUPABASE_URL";
        private const string EnvAnonKey = "SUPABASE_ANON_KEY";
        private const string FileName   = "supabase.json";
        private const string DotEnvName = ".env";

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

            // 2. .env at the project root (Editor / standalone desktop convenience).
            var fromDotEnv = TryLoadFromDotEnv();
            if (fromDotEnv.IsValid)
                return fromDotEnv;

            // 3. StreamingAssets JSON.
            var fromFile = TryLoadFromStreamingAssets();
            if (fromFile.IsValid)
                return fromFile;

            // 4. Inspector / caller defaults.
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

        /// <summary>
        /// Walks up from the working directory looking for a `.env` file (project root in the Editor)
        /// and parses simple KEY=VALUE lines. Quotes around the value are stripped.
        /// Only available on platforms with a real filesystem at the project root —
        /// silently returns <c>default</c> on iOS/Android players.
        /// </summary>
        private static Resolved TryLoadFromDotEnv()
        {
            try
            {
                var path = LocateDotEnv();
                if (path == null || !File.Exists(path))
                    return default;

                string url = null, anonKey = null;
                foreach (var rawLine in File.ReadAllLines(path))
                {
                    var line = rawLine.Trim();
                    if (line.Length == 0 || line[0] == '#') continue;

                    var eq = line.IndexOf('=');
                    if (eq <= 0) continue;

                    var key = line.Substring(0, eq).Trim();
                    var val = StripQuotes(line.Substring(eq + 1).Trim());

                    if (key == EnvUrl)     url     = val;
                    else if (key == EnvAnonKey) anonKey = val;
                }

                if (string.IsNullOrWhiteSpace(url) || string.IsNullOrWhiteSpace(anonKey))
                    return default;

                return new Resolved(url, anonKey, $"dotenv:{Path.GetFileName(path)}");
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[SupabaseConfigLoader] Failed to read {DotEnvName}: {ex.Message}");
                return default;
            }
        }

        private static string LocateDotEnv()
        {
            // In the Editor, Application.dataPath is "<project>/Assets" — its parent is the project root.
            // In standalone players, dataPath is inside the build; .env is unlikely to exist there, but
            // we still try the working directory as a last resort for desktop tooling.
            var candidates = new[]
            {
                Path.Combine(Directory.GetParent(Application.dataPath)?.FullName ?? string.Empty, DotEnvName),
                Path.Combine(Directory.GetCurrentDirectory(), DotEnvName),
            };

            foreach (var c in candidates)
            {
                if (!string.IsNullOrEmpty(c) && File.Exists(c))
                    return c;
            }
            return null;
        }

        private static string StripQuotes(string s)
        {
            if (s.Length >= 2 &&
                ((s[0] == '"'  && s[s.Length - 1] == '"') ||
                 (s[0] == '\'' && s[s.Length - 1] == '\'')))
            {
                return s.Substring(1, s.Length - 2);
            }
            return s;
        }

        [Serializable]
        private class JsonShape
        {
            public string url;
            public string anonKey;
        }
    }
}
