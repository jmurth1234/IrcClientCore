using System;
using System.Collections.Generic;
using System.Text;
using IrcClientCore;
using Terminal.Gui;

namespace ConsoleIrcClient
{
    public class ConnectView
    {
        private TextField _hostField;
        private TextField _portField;
        private CheckBox _sslBox;
        private TextField _usernameField;
        private TextField _passwordField;
        private TextField _channelsField;
        public IrcServer Server { get; set; }

        private void CreateUi()
        {
            var top = Application.Top;

            // Creates the top-level window to show
            var win = new Window("Connect to a Server")
            {
                X = 0,
                Y = 0,

                // By using Dim.Fill(), it will automatically resize without manual intervention
                Width = Dim.Fill(),
                Height = Dim.Fill()
            };
            top.Add(win);

            var hostLabel = new Label("Hostname: ")
            {
                X = 3,
                Y = 2,
                Width = 12
            };
            var portLabel = new Label("Port: ")
            {
                X = Pos.Left(hostLabel),
                Y = Pos.Top(hostLabel) + 2,
                Width = Dim.Width(hostLabel)
            };

            _hostField = new TextField("")
            {
                X = Pos.Right(portLabel),
                Y = Pos.Top(hostLabel),
                Width = 40
            };
            _portField = new TextField("")
            {
                X = Pos.Left(_hostField),
                Y = Pos.Top(portLabel),
                Width = 6
            };
            _sslBox = new CheckBox("Use SSL")
            {
                X = Pos.Right(_portField) + 2,
                Y = Pos.Top(_portField)
            };

            var usernameLabel = new Label("Username: ")
            {
                X = Pos.Left(portLabel),
                Y = Pos.Top(portLabel) + 2,
                Width = Dim.Width(hostLabel)
            };

            var passwordLabel = new Label("Password: ")
            {
                X = Pos.Left(usernameLabel),
                Y = Pos.Top(usernameLabel) + 2,
                Width = Dim.Width(hostLabel)
            };

            var channelsLabel = new Label("Channels: ")
            {
                X = Pos.Left(passwordLabel),
                Y = Pos.Top(passwordLabel) + 2,
                Width = Dim.Width(hostLabel)
            };

            _usernameField = new TextField("")
            {
                X = Pos.Left(_portField),
                Y = Pos.Top(usernameLabel),
                Width = 40
            };

            _passwordField = new TextField("")
            {
                X = Pos.Left(_usernameField),
                Y = Pos.Top(passwordLabel),
                Width = 40
            };

            _channelsField = new TextField("")
            {
                X = Pos.Left(_passwordField),
                Y = Pos.Top(channelsLabel),
                Width = 40
            };

            var okButton = new Button(3, 14, "Ok");

            okButton.Clicked += OkClicked;

            var cancelButton = new Button(10, 14, "Cancel");

            cancelButton.Clicked += CancelClicked;


            // Add some controls, 
            win.Add(
                hostLabel, portLabel, usernameLabel, passwordLabel, channelsLabel,
                _hostField, _portField, _sslBox, _usernameField, _passwordField, _channelsField,
                okButton, cancelButton
            );
        }

        public void Run()
        {
            Application.Init();

            CreateUi();
            Server = Serialize.DeSerializeObject<IrcServer>("server");

            if (Server == null)
            {
                Server = new IrcServer { Name = "Test Server", IgnoreCertErrors = true };
            }

            _hostField.Text = Server.Hostname;
            _portField.Text = Server.Port.ToString();
            _sslBox.Checked = Server.Ssl;
            _usernameField.Text = Server.Username;
            _passwordField.Text = Server.Password;
            _channelsField.Text = Server.Channels;

            Application.Run();
        }

        private void OkClicked()
        {
            Application.RequestStop();

            Server.Hostname = _hostField.Text.ToString();
            Server.Port = int.Parse(_portField.Text.ToString());
            Server.Ssl = _sslBox.Checked;
            Server.Username = _usernameField.Text.ToString();
            Server.Password = _passwordField.Text.ToString();
            Server.Channels = _channelsField.Text.ToString();

            Serialize.SerializeObject(Server,"server");
        }

        private void CancelClicked()
        {
            Environment.Exit(0);
        }
    }
}
