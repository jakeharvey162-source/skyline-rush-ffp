using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web.Script.Serialization;

namespace SkylineRush
{
    public sealed class SkylineSession
    {
        public string AccessToken;
        public string RefreshToken;
        public string UserId;
        public string Email;
        public bool IsUsable { get { return !String.IsNullOrWhiteSpace(AccessToken) && !String.IsNullOrWhiteSpace(UserId); } }
    }

    public sealed class SkylineRoomInfo
    {
        public Guid RoomId;
        public string Code;
        public string Mode;
        public string District;
        public int MaxPlayers;
        public string HostAddress;
        public int ServerPort;
        public string ServerPassword;
        public DateTime ExpiresAt;
    }

    public sealed class SkylineLobbyPlayer
    {
        public string UserId;
        public string DisplayName;
        public string Team;
        public bool Ready;
        public bool IsHost;
        public override string ToString()
        {
            string host = IsHost ? "HOST • " : "";
            string ready = Ready ? "READY" : "NOT READY";
            string team = String.IsNullOrWhiteSpace(Team) ? "" : " • " + Team;
            return host + DisplayName + team + " • " + ready;
        }
    }

    public sealed class SkylineLobbySnapshot
    {
        public Guid RoomId;
        public string Code;
        public string Mode;
        public string District;
        public string Status;
        public int MaxPlayers;
        public readonly List<SkylineLobbyPlayer> Players = new List<SkylineLobbyPlayer>();
    }

    public sealed class SkylineApiException : Exception
    {
        public int StatusCode { get; private set; }
        public SkylineApiException(string message, int statusCode) : base(message) { StatusCode = statusCode; }
    }

    public static class SkylineSessionStore
    {
        static string SessionPath
        {
            get
            {
                string dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SkylineRush", "auth");
                Directory.CreateDirectory(dir);
                return Path.Combine(dir, "refresh-token.dat");
            }
        }

        public static void SaveRefreshToken(string refreshToken)
        {
            if (String.IsNullOrWhiteSpace(refreshToken)) return;
            byte[] clear = Encoding.UTF8.GetBytes(refreshToken);
            byte[] encrypted = ProtectedData.Protect(clear, null, DataProtectionScope.CurrentUser);
            File.WriteAllBytes(SessionPath, encrypted);
            Array.Clear(clear, 0, clear.Length);
        }

        public static string LoadRefreshToken()
        {
            try
            {
                if (!File.Exists(SessionPath)) return null;
                byte[] encrypted = File.ReadAllBytes(SessionPath);
                byte[] clear = ProtectedData.Unprotect(encrypted, null, DataProtectionScope.CurrentUser);
                try { return Encoding.UTF8.GetString(clear); }
                finally { Array.Clear(clear, 0, clear.Length); }
            }
            catch (CryptographicException) { Clear(); return null; }
            catch (IOException) { return null; }
        }

        public static void Clear()
        {
            try { if (File.Exists(SessionPath)) File.Delete(SessionPath); }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
        }
    }

    public sealed class SkylineSupabaseClient
    {
        public const string BaseUrl = "https://ftsomveafuskrutqzsvs.supabase.co";
        public const string PublishableKey = "sb_publishable_x3SYM26ShPtf_JIYdvOkEg_kLXb2kIz";
        readonly JavaScriptSerializer json = new JavaScriptSerializer { MaxJsonLength = 1048576 };

        static SkylineSupabaseClient()
        {
            ServicePointManager.SecurityProtocol = (SecurityProtocolType)3072;
        }

        public Task<SkylineSession> SignInAsync(string email, string password)
        {
            return Task.Factory.StartNew(() =>
            {
                var body = new Dictionary<string, object> { { "email", email }, { "password", password } };
                return ParseSession(Request("POST", "/auth/v1/token?grant_type=password", body, null, null));
            });
        }

        public Task<SkylineSession> SignUpAsync(string email, string password)
        {
            return Task.Factory.StartNew(() =>
            {
                var body = new Dictionary<string, object> { { "email", email }, { "password", password } };
                return ParseSession(Request("POST", "/auth/v1/signup", body, null, null));
            });
        }

        public Task<SkylineSession> RefreshAsync(string refreshToken)
        {
            return Task.Factory.StartNew(() =>
            {
                var body = new Dictionary<string, object> { { "refresh_token", refreshToken } };
                return ParseSession(Request("POST", "/auth/v1/token?grant_type=refresh_token", body, null, null));
            });
        }

        public Task SignOutAsync(string accessToken)
        {
            return Task.Factory.StartNew(() =>
            {
                try { Request("POST", "/auth/v1/logout", new Dictionary<string, object>(), accessToken, null); }
                finally { SkylineSessionStore.Clear(); }
            });
        }

        public Task SaveDisplayNameAsync(SkylineSession session, string displayName)
        {
            return Task.Factory.StartNew(() =>
            {
                if (session == null || !session.IsUsable) throw new InvalidOperationException("Sign in first.");
                displayName = (displayName ?? "").Trim();
                if (displayName.Length < 2 || displayName.Length > 32) throw new ArgumentException("Display name must be 2–32 characters.");
                var body = new Dictionary<string, object> { { "user_id", session.UserId }, { "display_name", displayName } };
                Request("POST", "/rest/v1/skyline_profiles?on_conflict=user_id", body, session.AccessToken,
                    new Dictionary<string, string> { { "Prefer", "resolution=merge-duplicates,return=minimal" } });
            });
        }

        public Task<SkylineRoomInfo> CreateRoomAsync(SkylineSession session, string mode, string district, string hostAddress, int port, string password)
        {
            return Task.Factory.StartNew(() =>
            {
                RequireSession(session);
                var body = new Dictionary<string, object> {
                    { "p_mode", mode }, { "p_district", district }, { "p_host_address", hostAddress },
                    { "p_server_port", port }, { "p_server_password", password }
                };
                return ParseRoom(Request("POST", "/rest/v1/rpc/skyline_create_room", body, session.AccessToken, null));
            });
        }

        public Task<SkylineRoomInfo> JoinRoomAsync(SkylineSession session, string code)
        {
            return Task.Factory.StartNew(() =>
            {
                RequireSession(session);
                code = NormalizeRoomCode(code);
                var body = new Dictionary<string, object> { { "p_room_code", code } };
                return ParseRoom(Request("POST", "/rest/v1/rpc/skyline_join_room", body, session.AccessToken, null));
            });
        }

        public Task<SkylineLobbySnapshot> LobbyAsync(SkylineSession session, Guid roomId)
        {
            return Task.Factory.StartNew(() =>
            {
                RequireSession(session);
                var body = new Dictionary<string, object> {
                    { "p_room_id", roomId.ToString() }, { "p_action", "view" }, { "p_ready", null }
                };
                return ParseLobby(Request("POST", "/rest/v1/rpc/skyline_lobby_action", body, session.AccessToken, null));
            });
        }

        public Task<SkylineLobbySnapshot> SetReadyAsync(SkylineSession session, Guid roomId, bool ready)
        {
            return Task.Factory.StartNew(() =>
            {
                RequireSession(session);
                var body = new Dictionary<string, object> {
                    { "p_room_id", roomId.ToString() }, { "p_action", "ready" }, { "p_ready", ready }
                };
                return ParseLobby(Request("POST", "/rest/v1/rpc/skyline_lobby_action", body, session.AccessToken, null));
            });
        }

        public Task<SkylineLobbySnapshot> StartMatchAsync(SkylineSession session, Guid roomId)
        {
            return Task.Factory.StartNew(() =>
            {
                RequireSession(session);
                var body = new Dictionary<string, object> {
                    { "p_room_id", roomId.ToString() }, { "p_action", "start" }, { "p_ready", null }
                };
                return ParseLobby(Request("POST", "/rest/v1/rpc/skyline_lobby_action", body, session.AccessToken, null));
            });
        }

        public Task LeaveRoomAsync(SkylineSession session, Guid roomId)
        {
            return Task.Factory.StartNew(() =>
            {
                RequireSession(session);
                var body = new Dictionary<string, object> {
                    { "p_room_id", roomId.ToString() }, { "p_action", "leave" }, { "p_ready", null }
                };
                Request("POST", "/rest/v1/rpc/skyline_lobby_action", body, session.AccessToken, null);
            });
        }

        public static string NormalizeRoomCode(string code)
        {
            code = (code ?? "").Trim().ToUpperInvariant();
            if (!Regex.IsMatch(code, "^[A-F0-9]{6}$")) throw new ArgumentException("Room code must be six letters/numbers.");
            return code;
        }

        public static bool IsTailscaleIPv4(string address)
        {
            IPAddress ip;
            if (!IPAddress.TryParse(address, out ip) || ip.AddressFamily != System.Net.Sockets.AddressFamily.InterNetwork) return false;
            byte[] b = ip.GetAddressBytes();
            return b[0] == 100 && b[1] >= 64 && b[1] <= 127;
        }

        public static void ValidateConnection(SkylineRoomInfo room)
        {
            if (room == null || room.RoomId == Guid.Empty) throw new ArgumentException("Room information is incomplete.");
            if (!IsTailscaleIPv4(room.HostAddress)) throw new ArgumentException("Room host is not on the Skyline Alpha private network.");
            if (room.ServerPort < 1024 || room.ServerPort > 65535) throw new ArgumentException("Room server port is invalid.");
            if (String.IsNullOrEmpty(room.ServerPassword) || !Regex.IsMatch(room.ServerPassword, "^[A-Za-z0-9_-]{6,32}$"))
                throw new ArgumentException("Room server password is invalid.");
        }

        void RequireSession(SkylineSession session)
        {
            if (session == null || !session.IsUsable) throw new InvalidOperationException("Your session expired. Sign in again.");
        }

        SkylineSession ParseSession(string text)
        {
            var root = AsObject(json.DeserializeObject(text));
            var result = new SkylineSession {
                AccessToken = StringValue(root, "access_token"),
                RefreshToken = StringValue(root, "refresh_token")
            };
            Dictionary<string, object> user = ObjectValue(root, "user");
            if (user != null)
            {
                result.UserId = StringValue(user, "id");
                result.Email = StringValue(user, "email");
            }
            if (!String.IsNullOrWhiteSpace(result.RefreshToken)) SkylineSessionStore.SaveRefreshToken(result.RefreshToken);
            return result;
        }

        SkylineRoomInfo ParseRoom(string text)
        {
            var root = AsObject(json.DeserializeObject(text));
            var room = new SkylineRoomInfo {
                RoomId = Guid.Parse(StringValue(root, "room_id")),
                Code = StringValue(root, "room_code"),
                Mode = StringValue(root, "mode"),
                District = StringValue(root, "district"),
                MaxPlayers = IntValue(root, "max_players"),
                HostAddress = StringValue(root, "host_address"),
                ServerPort = IntValue(root, "server_port"),
                ServerPassword = StringValue(root, "server_password")
            };
            DateTime expires;
            if (DateTime.TryParse(StringValue(root, "expires_at"), out expires)) room.ExpiresAt = expires;
            return room;
        }

        SkylineLobbySnapshot ParseLobby(string text)
        {
            var root = AsObject(json.DeserializeObject(text));
            var lobby = new SkylineLobbySnapshot {
                RoomId = Guid.Parse(StringValue(root, "room_id")),
                Code = StringValue(root, "room_code"),
                Mode = StringValue(root, "mode"),
                District = StringValue(root, "district"),
                Status = StringValue(root, "status"),
                MaxPlayers = IntValue(root, "max_players")
            };
            object rawPlayers;
            if (root.TryGetValue("players", out rawPlayers) && rawPlayers is object[])
            {
                foreach (object raw in (object[])rawPlayers)
                {
                    var p = AsObject(raw);
                    lobby.Players.Add(new SkylineLobbyPlayer {
                        UserId = StringValue(p, "user_id"),
                        DisplayName = StringValue(p, "display_name") ?? "Player",
                        Team = StringValue(p, "team"),
                        Ready = BoolValue(p, "ready"),
                        IsHost = BoolValue(p, "is_host")
                    });
                }
            }
            else if (rawPlayers is System.Collections.ArrayList)
            {
                foreach (object raw in (System.Collections.ArrayList)rawPlayers)
                {
                    var p = AsObject(raw);
                    lobby.Players.Add(new SkylineLobbyPlayer {
                        UserId = StringValue(p, "user_id"),
                        DisplayName = StringValue(p, "display_name") ?? "Player",
                        Team = StringValue(p, "team"),
                        Ready = BoolValue(p, "ready"),
                        IsHost = BoolValue(p, "is_host")
                    });
                }
            }
            return lobby;
        }

        string Request(string method, string path, object body, string accessToken, Dictionary<string, string> extraHeaders)
        {
            var request = (HttpWebRequest)WebRequest.Create(BaseUrl + path);
            request.Method = method;
            request.ContentType = "application/json";
            request.Accept = "application/json";
            request.Headers["apikey"] = PublishableKey;
            if (!String.IsNullOrWhiteSpace(accessToken)) request.Headers["Authorization"] = "Bearer " + accessToken;
            if (extraHeaders != null) foreach (var pair in extraHeaders) request.Headers[pair.Key] = pair.Value;
            if (body != null)
            {
                byte[] payload = Encoding.UTF8.GetBytes(json.Serialize(body));
                request.ContentLength = payload.Length;
                using (Stream stream = request.GetRequestStream()) stream.Write(payload, 0, payload.Length);
            }
            try
            {
                using (var response = (HttpWebResponse)request.GetResponse())
                using (var reader = new StreamReader(response.GetResponseStream()))
                    return reader.ReadToEnd();
            }
            catch (WebException error)
            {
                var response = error.Response as HttpWebResponse;
                int status = response == null ? 0 : (int)response.StatusCode;
                string detail = "";
                if (response != null)
                {
                    try { using (var reader = new StreamReader(response.GetResponseStream())) detail = reader.ReadToEnd(); }
                    catch { }
                }
                throw new SkylineApiException(FriendlyError(detail, error.Message), status);
            }
        }

        string FriendlyError(string detail, string fallback)
        {
            try
            {
                var obj = AsObject(json.DeserializeObject(detail));
                foreach (string key in new[] { "message", "msg", "error_description", "error" })
                {
                    string value = StringValue(obj, key);
                    if (!String.IsNullOrWhiteSpace(value)) return value;
                }
            }
            catch { }
            return String.IsNullOrWhiteSpace(fallback) ? "Skyline online service is unavailable." : fallback;
        }

        static Dictionary<string, object> AsObject(object value)
        {
            var dict = value as Dictionary<string, object>;
            if (dict == null) throw new InvalidDataException("Skyline online service returned an unexpected response.");
            return dict;
        }
        static Dictionary<string, object> ObjectValue(Dictionary<string, object> d, string key)
        {
            object value;
            return d != null && d.TryGetValue(key, out value) ? value as Dictionary<string, object> : null;
        }
        static string StringValue(Dictionary<string, object> d, string key)
        {
            object value;
            return d != null && d.TryGetValue(key, out value) && value != null ? Convert.ToString(value) : null;
        }
        static int IntValue(Dictionary<string, object> d, string key)
        {
            object value;
            return d != null && d.TryGetValue(key, out value) && value != null ? Convert.ToInt32(value) : 0;
        }
        static bool BoolValue(Dictionary<string, object> d, string key)
        {
            object value;
            return d != null && d.TryGetValue(key, out value) && value != null && Convert.ToBoolean(value);
        }
    }
}
