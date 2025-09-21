using System;
using System.Collections.Generic;
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
    }

    private void HandleIncomingMessage(string message)
    {
        _chatOutput.Text += message + "\n";
    }
    
    private void _on_send_button_pressed()
    {
        _networkManager.SendMessage(_messageInput.Text);
        _messageInput.Text = "";
    }
}