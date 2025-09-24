using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Godot;

namespace PeerToPeerChatApp;

public partial class NetworkManager : Node
{
    private TcpListener _server;
    public List<TcpClient> _clients = new();
    private TcpClient _serverConnection;
    private Dictionary<TcpClient, string> _usernames = new();
    private Dictionary<TcpClient, bool> _versionConfirmed = new();
    private const int MainPort = 5001;
    private bool _isServer = false;
    private string _localId;

    public event Action<string> OnMessageReceived;

    public NetworkManager()
    {
        _localId = Guid.NewGuid().ToString();
        Task.Yield();
        TryConnectOrHost();
    }

    private async void TryConnectOrHost()
    {
        try
        {
            _serverConnection = new TcpClient();
            await _serverConnection.ConnectAsync(IPAddress.Loopback, MainPort);
            GD.Print("[Network] Connected to existing server on port " + MainPort);
            _isServer = false;
            HandleServerConnection(_serverConnection);
            StartServerHealthCheck();
        }
        catch
        {
            GD.Print("[Network] No server found. Starting as host...");
            StartServer();
            _isServer = true;
        }
    }

    private async void StartServer()
    {
        _server = new TcpListener(IPAddress.Any, MainPort);
        _server.Start();
        GD.Print("[Network] Hosting server on port " + MainPort);

        while (true)
        {
            var client = await _server.AcceptTcpClientAsync();
            GD.Print("[Network] Client connected!");
            _clients.Add(client);
            HandleClient(client);
        }
    }

    private async void HandleClient(TcpClient client)
    {
        // The server handles the client messages here
        var stream = client.GetStream();
        var buffer = new byte[1024];

        while (client.Connected)
        {
            try
            {
                int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
                if (bytesRead > 0)
                {
                    
                    string msg = Encoding.UTF8.GetString(buffer, 0, bytesRead).Trim();
                    GD.Print("[Server] Received: " + msg);
                    if (msg.StartsWith("USERNAME:"))
                    {
                        if (_versionConfirmed.ContainsKey(client))
                        {
                            string username = msg.Substring("USERNAME:".Length).Trim();
                            _usernames[client] = username;
                            GD.Print($"[Server] Registered Username {username}");
                        }
                    } else if (msg.StartsWith('/'))
                    {
                        GD.Print("Command Sent");
                        await HandleCommand(client, msg);
                    } else if (msg.StartsWith("CLIENTVERSION:"))
                    {
                        // Manages making sure the client and the server are the same version
                        string version = msg.Substring("CLIENTVERSION:".Length).Trim();
                        string serverVersion = ProjectSettings.GetSetting("application/config/version").ToString();
                        
                        // Make sure the versions are correct

                        if (version == serverVersion)
                        {
                            _versionConfirmed[client] = true;
                            GD.Print($"[Server] Version confirmed as compatible");
                        }
                        else
                        {
                            GD.Print($"[Server] Server version {serverVersion} not compatible");
                            client.Close();
                        }
                    }
                    else
                    {
                        if (!_versionConfirmed.ContainsKey(client) || !_versionConfirmed[client])
                        {
                            Broadcast($"Your client version does not match the server, please update to version {ProjectSettings.GetSetting("application/config/version").ToString()} to connect to this server", client);
                            client.Close();
                        }
                        OnMessageReceived?.Invoke(msg);
                        Broadcast(msg, client);
                    }
                }
            }
            catch
            {
                _clients.Remove(client);
                break;
            }
        }
    }

    private async Task HandleCommand(TcpClient client, string msg)
    {
        var stream = client.GetStream();

        if (msg == "/users")
        {
            string userList = string.Join(", ", _usernames.Values);
            string responseMessage = $"Server|Connected users: {userList}";
            byte[] response = Encoding.UTF8.GetBytes(responseMessage + "\n");
            await stream.WriteAsync(response, 0, response.Length);
        }
        if (msg == "/ping")
        {
            var now = DateTime.UtcNow;

            var hour = now.Hour;
            var minute = now.Minute;
            var second = now.Second;
            var millisecond = now.Millisecond;
            
            GD.Print("Sending current time to the server");
            
            string currentTime = $"SERVERTIME:{hour}:{minute}:{second}:{millisecond}";
            byte[] response = Encoding.UTF8.GetBytes(currentTime + "\n");
            await stream.WriteAsync(response, 0, response.Length);
        }
        else
        {
            string responseMessage = $"Server|UnknownCommand: {msg}\n";
            byte[] response = Encoding.UTF8.GetBytes(responseMessage + "\n");
            await stream.WriteAsync(response, 0, response.Length);
        }
    }

    private async void Broadcast(string message, TcpClient sender)
    {
        byte[] data = Encoding.UTF8.GetBytes(message + "\n");
        foreach (var c in _clients)
        {
            if (c != sender && c.Connected)
            {
                try { await c.GetStream().WriteAsync(data, 0, data.Length); }
                catch { _clients.Remove(c); }
            }
        }
    }

    private async void HandleServerConnection(TcpClient serverConn)
    {
        // The client handles server connection and sending messages here
        
        var stream = serverConn.GetStream();
        
        // Get information used for the next step
        string username = GetNode<UserData>("/root/UsernameStore").username;
        string clientVersion = ProjectSettings.GetSetting("application/config/version").ToString();
        
        // Send the client version over
        byte[] intro2 = Encoding.UTF8.GetBytes($"CLIENTVERSION:{clientVersion}\n");
        await stream.WriteAsync(intro2, 0, intro2.Length);
        
        // Send over the username
        byte[] intro = Encoding.UTF8.GetBytes($"USERNAME:{username}\n");
        await stream.WriteAsync(intro, 0, intro.Length);
        
        var buffer = new byte[1024];

        while (serverConn.Connected)
        {
            try
            {
                int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
                if (bytesRead > 0)
                {
                    string msg = Encoding.UTF8.GetString(buffer, 0, bytesRead).Trim();

                    if (msg.StartsWith("SERVERTIME:"))
                    {
                        // Manage the ping message here
                        
                        string timeData = msg.Substring("SERVERTIME:".Length);
                        
                        string[] parts = timeData.Split(':');

                        if (parts.Length == 4 &&

                            int.TryParse(parts[0], out int hour) &&
                            int.TryParse(parts[1], out int minute) &&
                            int.TryParse(parts[2], out int second) &&
                            int.TryParse(parts[3], out int millisecond)
                           )
                        {
                            DateTime now = DateTime.UtcNow;
                            DateTime serverTime = new DateTime(
                                now.Year, now.Month, now.Day, hour , minute, second, millisecond, DateTimeKind.Utc);

                            TimeSpan latency = DateTime.UtcNow - serverTime;
                            OnMessageReceived?.Invoke($"Pong: Latency: {latency.TotalMilliseconds}ms");
                        }
                    }
                    
                    GD.Print("[Client] Received: " + msg);
                    OnMessageReceived?.Invoke(msg);
                }
            }
            catch
            {
                GD.Print("[Client] Lost connection to server.");
                _serverConnection.Close();
                _serverConnection = null;
                _isServer = true;
                GD.Print("[Client] Becoming server...");
                StartServer();
                break;
            }
        }
    }

    // --- UPDATED SendMessage to include username ---
    public async void SendMessage(string message)
    {
        string username = GetNode<UserData>("/root/UsernameStore").username;
        string formattedMessage = $"{message}";

        byte[] data = Encoding.UTF8.GetBytes(formattedMessage + "\n");

        if (_isServer)
        {
            formattedMessage = $"{username}: {message}";
            GD.Print("[Server] Sending: " + formattedMessage);
            Broadcast(formattedMessage, null);
            OnMessageReceived?.Invoke(formattedMessage);
        }
        else
        {
            if (_serverConnection != null && _serverConnection.Connected)
            {
                try
                {
                    await _serverConnection.GetStream().WriteAsync(data, 0, data.Length);
                    OnMessageReceived?.Invoke(formattedMessage);
                }
                catch
                {
                    GD.Print("[Client] Failed to send message. Server might be down.");
                }
            }
        }
    }

    private async void StartServerHealthCheck()
    {
        while (!_isServer)
        {
            if (_serverConnection == null || !_serverConnection.Connected)
            {
                GD.Print("[Client] Server is offline. Taking over as server...");
                _isServer = true;
                StartServer();
                break;
            }
            await Task.Delay(3000);
        }
    }
}