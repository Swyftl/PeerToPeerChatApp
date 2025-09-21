using Godot;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

public partial class NetworkManager : Node
{
    private TcpListener server;
    private List<TcpClient> clients = new();
    private TcpClient serverConnection;
    private const int mainPort = 5001;
    private bool isServer = false;
    private string localId;

    public event Action<string> OnMessageReceived;

    public NetworkManager()
    {
        localId = Guid.NewGuid().ToString();
        TryConnectOrHost();
    }

    private async void TryConnectOrHost()
    {
        try
        {
            serverConnection = new TcpClient();
            await serverConnection.ConnectAsync(IPAddress.Loopback, mainPort);
            GD.Print("[Network] Connected to existing server on port " + mainPort);
            isServer = false;
            HandleServerConnection(serverConnection);
            StartServerHealthCheck();
        }
        catch
        {
            GD.Print("[Network] No server found. Starting as host...");
            StartServer();
            isServer = true;
        }
    }

    private async void StartServer()
    {
        server = new TcpListener(IPAddress.Any, mainPort);
        server.Start();
        GD.Print("[Network] Hosting server on port " + mainPort);

        while (true)
        {
            var client = await server.AcceptTcpClientAsync();
            GD.Print("[Network] Client connected!");
            clients.Add(client);
            HandleClient(client);
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
                clients.Remove(client);
                break;
            }
        }
    }

    private async void Broadcast(string message, TcpClient sender)
    {
        byte[] data = Encoding.UTF8.GetBytes(message + "\n");
        foreach (var c in clients)
        {
            if (c != sender && c.Connected)
            {
                try { await c.GetStream().WriteAsync(data, 0, data.Length); }
                catch { clients.Remove(c); }
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
                serverConnection.Close();
                serverConnection = null;
                isServer = true;
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

        if (isServer)
        {
            GD.Print("[Server] Sending: " + formattedMessage);
            Broadcast(formattedMessage, null);
            OnMessageReceived?.Invoke(formattedMessage);
        }
        else
        {
            if (serverConnection != null && serverConnection.Connected)
            {
                try
                {
                    await serverConnection.GetStream().WriteAsync(data, 0, data.Length);
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
        while (!isServer)
        {
            if (serverConnection == null || !serverConnection.Connected)
            {
                GD.Print("[Client] Server is offline. Taking over as server...");
                isServer = true;
                StartServer();
                break;
            }
            await Task.Delay(3000);
        }
    }
}
