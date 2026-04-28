using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Supabase;
using Supabase.Realtime;
using UnityEngine;

namespace ScrewGame.Core
{
    /// <summary>
    /// Singleton that initialises and owns the Supabase client.
    /// All other systems obtain the client via SupabaseManager.Instance.Client.
    /// Uses UniTask to keep async operations off the main Unity thread.
    /// </summary>
    public class SupabaseManager : MonoBehaviour
    {
        // -----------------------------------------------------------------------
        // Inspector fields
        // -----------------------------------------------------------------------
        [Header("Supabase Configuration")]
        [SerializeField] private string _supabaseUrl;
        [SerializeField] private string _supabaseAnonKey;

        // -----------------------------------------------------------------------
        // Singleton
        // -----------------------------------------------------------------------
        public static SupabaseManager Instance { get; private set; }

        /// <summary>Fully initialised Supabase client. Null until InitialiseAsync completes.</summary>
        public Supabase.Client Client { get; private set; }

        public bool IsInitialised { get; private set; }

        // -----------------------------------------------------------------------
        // Unity lifecycle
        // -----------------------------------------------------------------------
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            InitialiseAsync(destroyCancellationToken).Forget();
        }

        // -----------------------------------------------------------------------
        // Initialisation
        // -----------------------------------------------------------------------
        private async UniTaskVoid InitialiseAsync(CancellationToken ct)
        {
            try
            {
                var resolved = SupabaseConfigLoader.Resolve(_supabaseUrl, _supabaseAnonKey);
                if (!resolved.IsValid)
                {
                    Debug.LogError("[SupabaseManager] Missing Supabase URL or anon key. " +
                                   "Set SUPABASE_URL/SUPABASE_ANON_KEY env vars, populate " +
                                   "StreamingAssets/supabase.json, or fill the inspector fields.");
                    return;
                }

                Debug.Log($"[SupabaseManager] Loading config from: {resolved.Source}");

                var options = new SupabaseOptions
                {
                    AutoConnectRealtime = true,
                    AutoRefreshToken    = true,
                };

                Client = new Supabase.Client(resolved.Url, resolved.AnonKey, options);
                await Client.InitializeAsync();

                IsInitialised = true;
                Debug.Log("[SupabaseManager] Supabase client initialised.");

                // Hook presence/disconnect callbacks
                Client.Realtime.OnOpen    += () => Debug.Log("[SupabaseManager] Realtime connected.");
                Client.Realtime.OnClose   += (sender, args) => Debug.LogWarning("[SupabaseManager] Realtime disconnected.");
                Client.Realtime.OnError   += (sender, args) => Debug.LogError($"[SupabaseManager] Realtime error: {args.Message}");
            }
            catch (OperationCanceledException)
            {
                Debug.Log("[SupabaseManager] Initialisation cancelled.");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SupabaseManager] Failed to initialise: {ex.Message}");
            }
        }

        // -----------------------------------------------------------------------
        // Authentication helpers
        // -----------------------------------------------------------------------

        /// <summary>Sign up a new player with email + password.</summary>
        public async UniTask<bool> SignUpAsync(string email, string password, CancellationToken ct = default)
        {
            try
            {
                var session = await Client.Auth.SignUp(email, password);
                return session?.User != null;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SupabaseManager] SignUp failed: {ex.Message}");
                return false;
            }
        }

        /// <summary>Sign in an existing player with email + password.</summary>
        public async UniTask<bool> SignInAsync(string email, string password, CancellationToken ct = default)
        {
            try
            {
                var session = await Client.Auth.SignIn(email, password);
                return session?.User != null;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SupabaseManager] SignIn failed: {ex.Message}");
                return false;
            }
        }

        /// <summary>Sign out the current user.</summary>
        public async UniTask SignOutAsync()
        {
            try
            {
                await Client.Auth.SignOut();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SupabaseManager] SignOut failed: {ex.Message}");
            }
        }

        /// <summary>Returns the currently authenticated user ID, or null.</summary>
        public string CurrentUserId => Client?.Auth?.CurrentUser?.Id;
    }
}
