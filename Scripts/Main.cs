using System.Net.Sockets;
using Godot;

namespace PeerToPeerChatApp;

public partial class Main : Control
{

    private Button _sendButton;
    private LineEdit _messageInput;
    private TextEdit _chatOutput;
    private NetworkManager _networkManager;
    
    public override void _Ready()
    {
        _networkManager = new NetworkManager();
        AddChild(_networkManager);
        _networkManager.OnMessageReceived += HandleIncomingMessage;
        _sendButton = GetNode<Button>("BottomContainer/SendButton");
        _messageInput = GetNode<LineEdit>("BottomContainer/MessageInput");
        _chatOutput = GetNode<TextEdit>("ChatOutput");
        
        DisplayServer.WindowSetTitle($"PeerToPeerChatApp {ProjectSettings.GetSetting("application/config/version").ToString()}");
    }

    private void HandleIncomingMessage(string message)
    {
        _chatOutput.Text += message + "\n";
    }
    
    private void _on_send_button_pressed()
    {
        if (_messageInput.Text.Length is > 0 and < 2048)
        {
            _networkManager.SendMessage(_messageInput.Text);
            _messageInput.Text = "";
        }
    }
    
    public override void _Process(double delta)
    {
        if (Input.IsActionJustPressed("Enter"))
        {
            _on_send_button_pressed();
        }
    }
}