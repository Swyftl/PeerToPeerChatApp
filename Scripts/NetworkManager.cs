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
    private const int MainPort = 5001;
    private bool _isServer = false;
    private string _localId;

    public event Action<string> OnMessageReceived;

    public NetworkManager()
    {
        _localId = Guid.NewGuid().ToString();
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
            
            // Request a username from the user
            Broadcast("0x001", client);
        }
    }

    private async void HandleClient(TcpClient client)
    {
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
                    OnMessageReceived?.Invoke(msg);
                    Broadcast(msg, client);
                }
            }
            catch
            {
                _clients.Remove(client);
                break;
            }
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
        var stream = serverConn.GetStream();
        var buffer = new byte[1024];

        while (serverConn.Connected)
        {
            try
            {
                int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
                if (bytesRead > 0)
                {
                    string msg = Encoding.UTF8.GetString(buffer, 0, bytesRead).Trim();
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
        string username = GetNode<UserData>("/root/UserData").username;
        string formattedMessage = $"{username}|{message}";

        byte[] data = Encoding.UTF8.GetBytes(formattedMessage + "\n");

        if (_isServer)
        {
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