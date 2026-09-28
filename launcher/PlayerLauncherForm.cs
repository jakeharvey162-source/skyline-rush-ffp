using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SkylineRush
{
    public sealed class SkylineLauncherForm : Form
    {
        readonly string root;
        readonly SkylineSupabaseClient api = new SkylineSupabaseClient();
        SkylineSession session;
        WindowsMatchHost host;
        WindowsMatchHost.Match hostedMatch;
        SkylineRoomInfo room;
        bool isHost;
        bool refreshing;

        readonly Label status = new Label();
        readonly Panel authPanel = new Panel();
        readonly Panel appPanel = new Panel();
        readonly TextBox email = new TextBox();
        readonly TextBox password = new TextBox();
        readonly TextBox displayName = new TextBox();
        readonly Label identity = new Label();
        readonly Label tailscale = new Label();
        readonly ComboBox mode = new ComboBox();
        readonly ComboBox district = new ComboBox();
        readonly TextBox roomCode = new TextBox();
        readonly Label lobbyTitle = new Label();
        readonly ListBox players = new ListBox();
        readonly Button readyButton = new Button();
        readonly Button connectButton = new Button();
        readonly Button leaveButton = new Button();
        readonly Timer lobbyTimer = new Timer();

        public SkylineLauncherForm(string gameRoot)
        {
            root = gameRoot;
            Text = "Skyline Rush";
            StartPosition = FormStartPosition.CenterScreen;
            MinimumSize = new Size(900, 600);
            Size = new Size(980, 650);
            BackColor = Color.FromArgb(7, 15, 20);
            ForeColor = Color.White;
            Font = new Font("Segoe UI", 10f);
            BuildUi();
            lobbyTimer.Interval = 2500;
            lobbyTimer.Tick += async (s, e) => await RefreshLobbyAsync();
            Shown += async (s, e) => await RestoreSessionAsync();
            FormClosing += (s, e) =>
            {
                lobbyTimer.Stop();
                try { if (hostedMatch != null && host != null) host.Stop(hostedMatch.Id); } catch { }
                if (host != null) host.Dispose();
            };
        }

        void BuildUi()
        {
            var title = new Label {
                Text = "SKYLINE RUSH",
                Font = new Font("Segoe UI Semibold", 25f, FontStyle.Bold),
                ForeColor = Color.FromArgb(95, 235, 226),
                AutoSize = true, Location = new Point(28, 20)
            };
            var subtitle = new Label {
                Text = "WINDOWS ALPHA • PRIVATE MULTIPLAYER",
                ForeColor = Color.FromArgb(242, 163, 58),
                AutoSize = true, Location = new Point(31, 61)
            };
            Controls.Add(title); Controls.Add(subtitle);

            status.AutoSize = false; status.Location = new Point(31, 92); status.Size = new Size(900, 44);
            status.ForeColor = Color.FromArgb(176, 195, 204); status.Text = "Starting Skyline Rush…";
            Controls.Add(status);

            authPanel.Location = new Point(28, 142); authPanel.Size = new Size(900, 390); authPanel.BackColor = Color.FromArgb(12, 26, 34);
            Controls.Add(authPanel);
            AddLabel(authPanel, "SIGN IN / CREATE ACCOUNT", 24, 22, 270, true);
            AddLabel(authPanel, "Email", 24, 72, 110, false);
            SetupText(email, 24, 94, 400); authPanel.Controls.Add(email);
            AddLabel(authPanel, "Password", 24, 138, 110, false);
            SetupText(password, 24, 160, 400); password.UseSystemPasswordChar = true; authPanel.Controls.Add(password);
            AddLabel(authPanel, "Display name (for new accounts)", 24, 204, 240, false);
            SetupText(displayName, 24, 226, 400); authPanel.Controls.Add(displayName);

            var signIn = Button("SIGN IN", 24, 284, 190);
            signIn.Click += async (s, e) => await SignInAsync();
            var signUp = Button("CREATE ACCOUNT", 234, 284, 190);
            signUp.Click += async (s, e) => await SignUpAsync();
            authPanel.Controls.Add(signIn); authPanel.Controls.Add(signUp);

            AddLabel(authPanel, "Your password is sent only to Supabase Auth. Skyline stores only an encrypted refresh token under your Windows user.", 470, 94, 380, false);

            appPanel.Location = new Point(28, 142); appPanel.Size = new Size(900, 440); appPanel.BackColor = Color.FromArgb(12, 26, 34); appPanel.Visible = false;
            Controls.Add(appPanel);

            identity.Location = new Point(24, 18); identity.Size = new Size(590, 28); identity.Font = new Font("Segoe UI Semibold", 12f);
            appPanel.Controls.Add(identity);
            var signOut = Button("SIGN OUT", 720, 14, 150); signOut.Click += async (s, e) => await SignOutAsync(); appPanel.Controls.Add(signOut);

            var offline = Button("PLAY OFFLINE", 24, 60, 180); offline.Click += (s, e) => LaunchGame(null); appPanel.Controls.Add(offline);
            var check = Button("CHECK TAILSCALE", 224, 60, 190); check.Click += async (s, e) => await CheckTailscaleAsync(); appPanel.Controls.Add(check);
            tailscale.Location = new Point(438, 64); tailscale.Size = new Size(430, 48); tailscale.ForeColor = Color.FromArgb(154, 180, 188);
            appPanel.Controls.Add(tailscale);

            var createBox = Group("CREATE PRIVATE ROOM", 24, 125, 400, 170);
            appPanel.Controls.Add(createBox);
            AddLabel(createBox, "Mode", 18, 32, 70, false);
            mode.Location = new Point(18, 55); mode.Size = new Size(165, 28); mode.DropDownStyle = ComboBoxStyle.DropDownList;
            mode.Items.AddRange(new object[] { "duel", "2v2", "4v4", "coop2", "coop4" }); mode.SelectedIndex = 0; createBox.Controls.Add(mode);
            AddLabel(createBox, "District", 205, 32, 80, false);
            district.Location = new Point(205, 55); district.Size = new Size(165, 28); district.DropDownStyle = ComboBoxStyle.DropDownList;
            district.Items.AddRange(new object[] { "heights", "lagoon", "rift" }); district.SelectedIndex = 0; createBox.Controls.Add(district);
            var create = Button("CREATE ROOM", 18, 103, 352); create.Click += async (s, e) => await CreateRoomAsync(); createBox.Controls.Add(create);

            var joinBox = Group("JOIN ROOM", 448, 125, 422, 170);
            appPanel.Controls.Add(joinBox);
            AddLabel(joinBox, "6-character room code", 18, 32, 200, false);
            SetupText(roomCode, 18, 56, 386); roomCode.MaxLength = 6; roomCode.CharacterCasing = CharacterCasing.Upper; joinBox.Controls.Add(roomCode);
            var join = Button("JOIN & OPEN LOBBY", 18, 103, 386); join.Click += async (s, e) => await JoinRoomAsync(); joinBox.Controls.Add(join);

            var lobbyBox = Group("LOBBY", 24, 312, 846, 108);
            appPanel.Controls.Add(lobbyBox);
            lobbyTitle.Location = new Point(18, 28); lobbyTitle.Size = new Size(280, 64); lobbyTitle.Text = "No active room.";
            lobbyBox.Controls.Add(lobbyTitle);
            players.Location = new Point(300, 24); players.Size = new Size(270, 72); players.BackColor = Color.FromArgb(5, 14, 18); players.ForeColor = Color.White; players.BorderStyle = BorderStyle.FixedSingle;
            lobbyBox.Controls.Add(players);
            readyButton = Button("READY", 585, 24, 115); readyButton.Enabled = false; readyButton.Click += async (s, e) => await ToggleReadyAsync(); lobbyBox.Controls.Add(readyButton);
            connectButton = Button("PLAY MATCH", 710, 24, 115); connectButton.Enabled = false; connectButton.Click += (s, e) => ConnectToRoom(); lobbyBox.Controls.Add(connectButton);
            leaveButton = Button("LEAVE", 585, 66, 240); leaveButton.Enabled = false; leaveButton.Click += async (s, e) => await LeaveRoomAsync(); lobbyBox.Controls.Add(leaveButton);
        }

        static void AddLabel(Control parent, string text, int x, int y, int width, bool strong)
        {
            var l = new Label { Text = text, Location = new Point(x, y), Size = new Size(width, strong ? 30 : 22),
                ForeColor = strong ? Color.FromArgb(95, 235, 226) : Color.FromArgb(170, 190, 198) };
            if (strong) l.Font = new Font("Segoe UI Semibold", 12f, FontStyle.Bold);
            parent.Controls.Add(l);
        }

        static void SetupText(TextBox box, int x, int y, int width)
        {
            box.Location = new Point(x, y); box.Size = new Size(width, 27);
            box.BackColor = Color.FromArgb(5, 14, 18); box.ForeColor = Color.White; box.BorderStyle = BorderStyle.FixedSingle;
        }

        static Button Button(string text, int x, int y, int width)
        {
            return new Button {
                Text = text, Location = new Point(x, y), Size = new Size(width, 34),
                FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(242, 163, 58), ForeColor = Color.FromArgb(12, 16, 18),
                Font = new Font("Segoe UI Semibold", 9f, FontStyle.Bold)
            };
        }

        static GroupBox Group(string text, int x, int y, int width, int height)
        {
            return new GroupBox { Text = text, Location = new Point(x, y), Size = new Size(width, height),
                ForeColor = Color.FromArgb(95, 235, 226), BackColor = Color.FromArgb(9, 21, 28) };
        }

        async Task RestoreSessionAsync()
        {
            string refresh = SkylineSessionStore.LoadRefreshToken();
            if (String.IsNullOrWhiteSpace(refresh)) { ShowSignedOut("Sign in to use private multiplayer, or use the Windows package directly for offline practice."); return; }
            SetStatus("Restoring your secure session…");
            try
            {
                session = await api.RefreshAsync(refresh);
                if (!session.IsUsable) { SkylineSessionStore.Clear(); ShowSignedOut("Your saved session expired. Sign in again."); return; }
                ShowSignedIn();
                await CheckTailscaleAsync();
            }
            catch (Exception ex) { SkylineSessionStore.Clear(); ShowSignedOut(Friendly(ex)); }
        }

        async Task SignInAsync()
        {
            if (!ValidateCredentials()) return;
            SetStatus("Signing in…");
            try
            {
                session = await api.SignInAsync(email.Text.Trim(), password.Text);
                if (!session.IsUsable) throw new InvalidOperationException("Sign in did not return a usable session.");
                password.Clear(); ShowSignedIn(); await CheckTailscaleAsync();
            }
            catch (Exception ex) { SetStatus(Friendly(ex)); }
        }

        async Task SignUpAsync()
        {
            if (!ValidateCredentials()) return;
            SetStatus("Creating your Skyline Rush account…");
            try
            {
                session = await api.SignUpAsync(email.Text.Trim(), password.Text);
                if (session.IsUsable)
                {
                    if (!String.IsNullOrWhiteSpace(displayName.Text)) await api.SaveDisplayNameAsync(session, displayName.Text);
                    password.Clear(); ShowSignedIn(); await CheckTailscaleAsync();
                }
                else
                {
                    SkylineSessionStore.Clear();
                    SetStatus("Account created. If email confirmation is enabled, confirm your email, then sign in.");
                }
            }
            catch (Exception ex) { SetStatus(Friendly(ex)); }
        }

        bool ValidateCredentials()
        {
            if (String.IsNullOrWhiteSpace(email.Text) || !email.Text.Contains("@")) { SetStatus("Enter a valid email address."); return false; }
            if (password.Text.Length < 6) { SetStatus("Password must be at least 6 characters."); return false; }
            return true;
        }

        void ShowSignedOut(string message)
        {
            session = null; authPanel.Visible = true; appPanel.Visible = false; SetStatus(message);
        }

        void ShowSignedIn()
        {
            authPanel.Visible = false; appPanel.Visible = true;
            identity.Text = "SIGNED IN • " + (String.IsNullOrWhiteSpace(session.Email) ? session.UserId : session.Email);
            SetStatus("Ready. Offline play works immediately; private rooms use the free Tailscale Alpha network.");
        }

        async Task SignOutAsync()
        {
            await LeaveRoomAsync(false);
            try { if (session != null && session.IsUsable) await api.SignOutAsync(session.AccessToken); }
            catch { SkylineSessionStore.Clear(); }
            ShowSignedOut("Signed out.");
        }

        async Task CheckTailscaleAsync()
        {
            tailscale.Text = "Checking…";
            NetworkStatus network = await TailscaleNetwork.CheckAsync();
            tailscale.ForeColor = network.Connected ? Color.FromArgb(95, 235, 160) : Color.FromArgb(242, 163, 58);
            tailscale.Text = network.Message + (network.Connected ? "  " + network.Address : "");
        }

        async Task CreateRoomAsync()
        {
            if (!RequireSession()) return;
            SetStatus("Checking Tailscale and starting a private dedicated server…");
            NetworkStatus network = await TailscaleNetwork.CheckAsync();
            tailscale.Text = network.Message + (network.Connected ? "  " + network.Address : "");
            if (!network.Connected) { SetStatus(network.Message); return; }
            try
            {
                if (room != null) await LeaveRoomAsync();
                if (host == null) host = new WindowsMatchHost(root, Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SkylineRush", "host"));
                hostedMatch = host.Start(Guid.NewGuid(), Convert.ToString(mode.SelectedItem), Convert.ToString(district.SelectedItem));
                bool ready = await Task.Factory.StartNew(() =>
                {
                    for (int i = 0; i < 120 && !hostedMatch.IsReady && !hostedMatch.HasExited; i++) Thread.Sleep(100);
                    return hostedMatch.IsReady;
                });
                if (!ready) throw new InvalidOperationException("The dedicated server did not become ready. Check your Windows security software and try again.");
                room = await api.CreateRoomAsync(session, Convert.ToString(mode.SelectedItem), Convert.ToString(district.SelectedItem),
                    network.Address, hostedMatch.Port, hostedMatch.Password);
                isHost = true;
                ActivateLobby("Room " + room.Code + " created. Share only this code with your friend.");
                await RefreshLobbyAsync();
            }
            catch (Exception ex)
            {
                if (hostedMatch != null && host != null) { try { host.Stop(hostedMatch.Id); } catch { } hostedMatch = null; }
                room = null; SetStatus(Friendly(ex));
            }
        }

        async Task JoinRoomAsync()
        {
            if (!RequireSession()) return;
            SetStatus("Joining room…");
            try
            {
                SkylineRoomInfo joined = await api.JoinRoomAsync(session, roomCode.Text);
                SkylineSupabaseClient.ValidateConnection(joined);
                room = joined; isHost = false;
                ActivateLobby("Joined room " + room.Code + ". Mark Ready, then Play Match when your host is ready.");
                await RefreshLobbyAsync();
            }
            catch (Exception ex) { SetStatus(Friendly(ex)); }
        }

        void ActivateLobby(string message)
        {
            lobbyTimer.Start(); readyButton.Enabled = true; connectButton.Enabled = true; leaveButton.Enabled = true;
            readyButton.Text = isHost ? "HOST READY" : "READY";
            SetStatus(message);
        }

        async Task RefreshLobbyAsync()
        {
            if (refreshing || room == null || session == null || !session.IsUsable) return;
            refreshing = true;
            try
            {
                SkylineLobbySnapshot lobby = await api.LobbyAsync(session, room.RoomId);
                lobbyTitle.Text = "ROOM " + lobby.Code + Environment.NewLine + lobby.Mode.ToUpperInvariant() + " • " + lobby.District.ToUpperInvariant() + " • " + lobby.Status.ToUpperInvariant();
                players.Items.Clear();
                bool meReady = false;
                foreach (SkylineLobbyPlayer p in lobby.Players)
                {
                    players.Items.Add(p);
                    if (p.UserId == session.UserId) meReady = p.Ready;
                }
                readyButton.Text = meReady ? "NOT READY" : "READY";
            }
            catch (Exception ex)
            {
                SetStatus(Friendly(ex));
                if (ex is SkylineApiException) lobbyTimer.Stop();
            }
            finally { refreshing = false; }
        }

        async Task ToggleReadyAsync()
        {
            if (room == null) return;
            bool next = readyButton.Text != "NOT READY";
            try
            {
                await api.SetReadyAsync(session, room.RoomId, next);
                await RefreshLobbyAsync();
            }
            catch (Exception ex) { SetStatus(Friendly(ex)); }
        }

        void ConnectToRoom()
        {
            if (room == null) return;
            try
            {
                string address = isHost ? "127.0.0.1" : room.HostAddress;
                if (!isHost) SkylineSupabaseClient.ValidateConnection(room);
                LaunchGame(new SkylineRoomInfo { RoomId = room.RoomId, HostAddress = address, ServerPort = room.ServerPort, ServerPassword = room.ServerPassword });
                SetStatus("Game launched. The launcher can stay open for lobby status and cleanup.");
            }
            catch (Exception ex) { SetStatus(Friendly(ex)); }
        }

        void LaunchGame(SkylineRoomInfo target)
        {
            string exe = Path.Combine(root, "bin", "amd64", "redeclipse_windows_amd64.exe");
            if (!File.Exists(exe)) throw new FileNotFoundException("The game executable is missing. Reinstall the full Skyline Rush package.");
            string home = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SkylineRush", "player");
            Directory.CreateDirectory(home);
            var start = new ProcessStartInfo(exe) { WorkingDirectory = root, UseShellExecute = false };
            start.Arguments = "-h\"" + home + "\"";
            if (target != null)
            {
                if (target.ServerPort < 1024 || target.ServerPort > 65535) throw new ArgumentException("Invalid room port.");
                start.Arguments += " -x\"connect " + target.HostAddress + " " + target.ServerPort + " " + target.ServerPassword + "\"";
            }
            Process.Start(start);
        }

        async Task LeaveRoomAsync() { await LeaveRoomAsync(true); }

        async Task LeaveRoomAsync(bool showStatus)
        {
            lobbyTimer.Stop();
            if (room != null && session != null && session.IsUsable)
            {
                try { await api.LeaveRoomAsync(session, room.RoomId); } catch { }
            }
            if (hostedMatch != null && host != null)
            {
                try { host.Stop(hostedMatch.Id); } catch { }
                hostedMatch = null;
            }
            room = null; isHost = false; players.Items.Clear(); lobbyTitle.Text = "No active room.";
            readyButton.Enabled = false; connectButton.Enabled = false; leaveButton.Enabled = false;
            if (showStatus) SetStatus("Left the room.");
        }

        bool RequireSession()
        {
            if (session != null && session.IsUsable) return true;
            SetStatus("Your session expired. Sign in again."); ShowSignedOut(status.Text); return false;
        }

        void SetStatus(string text) { status.Text = text; }

        static string Friendly(Exception ex)
        {
            string text = ex == null ? "Something went wrong." : ex.Message;
            if (String.IsNullOrWhiteSpace(text)) text = "Something went wrong.";
            if (text.IndexOf("401", StringComparison.OrdinalIgnoreCase) >= 0) return "Your session expired. Sign in again.";
            if (text.IndexOf("Room not found", StringComparison.OrdinalIgnoreCase) >= 0) return "That room code is invalid or expired.";
            if (text.IndexOf("Room is full", StringComparison.OrdinalIgnoreCase) >= 0) return "That room is already full.";
            if (text.IndexOf("name resolution", StringComparison.OrdinalIgnoreCase) >= 0 || text.IndexOf("connect", StringComparison.OrdinalIgnoreCase) >= 0 && text.IndexOf("unable", StringComparison.OrdinalIgnoreCase) >= 0)
                return "Skyline online services are unreachable. Check your internet connection and try again.";
            return text;
        }
    }
}
