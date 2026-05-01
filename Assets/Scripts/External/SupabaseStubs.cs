// Compile-time stubs for supabase-csharp. Replace with the real NuGet
// package once NuGetForUnity is configured. Define SUPABASE_REAL to disable.
#if !SUPABASE_REAL
#pragma warning disable CS0067 // event never used
#pragma warning disable CS0414 // field assigned but never used
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Supabase
{
    public class SupabaseOptions
    {
        public bool AutoConnectRealtime;
        public bool AutoRefreshToken;
    }

    public class Client
    {
        public Client(string url, string anonKey, SupabaseOptions options = null) { }

        public Task InitializeAsync() => Task.CompletedTask;

        public Realtime.RealtimeClient Realtime { get; } = new Realtime.RealtimeClient();
        public Auth.AuthClient        Auth     { get; } = new Auth.AuthClient();
        public Functions.FunctionsClient Functions { get; } = new Functions.FunctionsClient();

        public Realtime.RealtimeChannel Channel(string topic) => new Realtime.RealtimeChannel(topic);

        public Postgrest.QueryBuilder<T> From<T>() where T : Postgrest.Models.BaseModel, new()
            => new Postgrest.QueryBuilder<T>();

        public Postgrest.QueryBuilder<T> From<T>(string table) where T : Postgrest.Models.BaseModel, new()
            => new Postgrest.QueryBuilder<T>();
    }
}

namespace Supabase.Auth
{
    public class User
    {
        public string Id { get; set; }
    }

    public class Session
    {
        public User User { get; set; }
    }

    public class AuthClient
    {
        public User CurrentUser { get; private set; }

        public Task<Session> SignUp(string email, string password)  => Task.FromResult<Session>(null);
        public Task<Session> SignIn(string email, string password)  => Task.FromResult<Session>(null);
        public Task          SignOut()                              => Task.CompletedTask;
    }
}

namespace Supabase.Functions
{
    public class FunctionsClient
    {
        public Task<object> Invoke(string functionName, string body) => Task.FromResult<object>(null);
    }
}

namespace Supabase.Realtime
{
    public class RealtimeClient
    {
        public event Action                                  OnOpen;
        public event EventHandler<EventArgs>                 OnClose;
        public event EventHandler<RealtimeErrorArgs>         OnError;
    }

    public class RealtimeErrorArgs : EventArgs
    {
        public string Message { get; set; }
    }

    public class PresenceEventArgs : EventArgs
    {
        public PresenceResponse Response { get; set; }
    }

    public class PresenceResponse
    {
        public Dictionary<string, object> Joins  { get; } = new Dictionary<string, object>();
        public Dictionary<string, object> Leaves { get; } = new Dictionary<string, object>();
    }

    public class PresenceCollection : Dictionary<string, object> { }

    public class PostgresChangeEventArgs : EventArgs
    {
        public PostgresChangePayload Payload { get; set; }
    }

    public class PostgresChangePayload
    {
        public PostgresChangeData Data { get; set; }
    }

    public class PostgresChangeData
    {
        public object Record { get; set; }
    }

    public class RealtimeChannel
    {
        public string Topic { get; }
        public PresenceCollection Presences { get; } = new PresenceCollection();

        public event EventHandler<Broadcast.BaseBroadcast>   OnBroadcast;
        public event EventHandler<EventArgs>                 OnPresenceSync;
        public event EventHandler<PresenceEventArgs>         OnPresenceJoin;
        public event EventHandler<PresenceEventArgs>         OnPresenceLeave;
        public event EventHandler<PostgresChangeEventArgs>   OnPostgresChange;

        public RealtimeChannel(string topic) { Topic = topic; }

        public Task Subscribe()                              => Task.CompletedTask;
        public Task Unsubscribe()                            => Task.CompletedTask;
        public Task Track(Dictionary<string, object> state)  => Task.CompletedTask;
        public Task Send(string type, string evt, object payload) => Task.CompletedTask;
    }
}

namespace Supabase.Realtime.Broadcast
{
    public class BaseBroadcast : EventArgs
    {
        public T Payload<T>() where T : class => null;
    }
}
#pragma warning restore CS0414
#pragma warning restore CS0067
#endif
