using Godot;
using Octokit;

namespace PeerToPeerChatApp;

public partial class Start : Control
{
    private LineEdit _usernameInput;
    private string latestClientVersion;
    
    public override void _Ready()
    {
        _usernameInput = GetNode<LineEdit>("LineEdit");
        // Check for updates on the client side

        if (CheckForUpdates())
        {
            var updatePopup = new AcceptDialog();

            updatePopup.OkButtonText = "I Understand";
            updatePopup.DialogText =
                $"And update to version {latestClientVersion} has been found, please update when you can.";
            updatePopup.Title = "Update Found";
            
            AddChild(updatePopup);
            updatePopup.Show();
        }
    }

    private bool CheckForUpdates()
    {
        var client = new GitHubClient(new ProductHeaderValue("PeerToPeerChatApp"));
        var releases = client.Repository.Release.GetLatest("Swyftl", "PeerToPeerChatApp");

        var latest = releases.Result;
        
        GD.Print($"The latest release was tagged at {latest.TagName} and is called {latest.Name}");
        GD.Print("Checking for updates...");

        var latestVersion = latest.TagName.Substring(1, latest.TagName.Length-1).Trim();
        
        latestClientVersion = latestVersion;

        if (latestVersion != ProjectSettings.GetSetting("application/config/version").ToString())
        {
            GD.Print("An update was found, prompting the user now!");
            return true;
        }
        else
        {
            GD.Print("The latest version is up to date!");
            return false;
        }
    }
    
    public void _on_button_pressed()
    {
        if (_usernameInput.Text.Length is > 2 and <= 20)
        {
            var userData = GetNode<UserData>("/root/UsernameStore");
            userData.username = _usernameInput.Text;
            // Change the scene to main

            var mainScene = GD.Load<PackedScene>("res://Scenes/main.tscn");

            GetTree().ChangeSceneToPacked(mainScene);
        }
    }

    public override void _Process(double delta)
    {
        if (Input.IsActionJustPressed("Enter"))
        {
            _on_button_pressed();
        }
    }
}