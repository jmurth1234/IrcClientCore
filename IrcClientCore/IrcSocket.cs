using System;
using System.IO;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

namespace IrcClientCore
{
    public class IrcSocket : Irc
    {
        private TcpClient _conn;
        private Stream _stream;
        private StreamReader _clientStreamReader;
        private StreamWriter _clientStreamWriter;

        public IrcSocket(IrcServer server) : base(server)
        {
        }

        public override async void Connect()
        {
            if (Server == null)
                return;

            IsConnecting = true;
            IsAuthed = false;
            ReadOrWriteFailed = false;
            _conn = new TcpClient();
            _conn.NoDelay = true;
            try
            {
                await _conn.ConnectAsync(Server.Hostname, Server.Port);

                if (_conn.Connected)
                {
                    if (Server.Ssl)
                    {
                        SslStream sslStream;
                        if (Server.IgnoreCertErrors)
                        {
                            sslStream = new SslStream(_conn.GetStream(), true, new RemoteCertificateValidationCallback(CheckCert));
                        }
                        else
                        {
                            sslStream = new SslStream(_conn.GetStream());
                        }

                        await sslStream.AuthenticateAsClientAsync(Server.Hostname);
                        _stream = sslStream;
                    }
                    else
                    {
                        _stream = _conn.GetStream();
                    }

                    _clientStreamReader = new StreamReader(_stream);
                    _clientStreamWriter = new StreamWriter(_stream);

                    IsConnecting = false;
                    IsConnected = true;
                    AttemptAuth();

                    while (true)
                    {
                        var line = await _clientStreamReader.ReadLineAsync();
                        await RecieveLine(line);
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                Console.WriteLine(e.StackTrace);
            }
        }

        private bool CheckCert(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            return true;
        }

        public override void DisconnectAsync(string msg = "Powered by WinIRC", bool attemptReconnect = false)
        {
            WriteLine("QUIT :" + msg);
            IsConnected = false;

            if (Server.ShouldReconnect && attemptReconnect)
            {
                IsConnecting = true;
                ReconnectionAttempts++;

                Task.Run(async () =>
                {
                    if (ReconnectionAttempts < 3)
                        await Task.Delay(1000);
                    else
                        await Task.Delay(60000);

                    if (IsConnecting)
                        Connect();
                });
            }
            else
            {
                IsConnecting = false;
                HandleDisconnect?.Invoke(this);
            }
        }

        public override void WriteLine(string str)
        {
            try
            {
                _clientStreamWriter.WriteLine(str);
                _clientStreamWriter.Flush();
            }
            catch (Exception e)
            {
                Console.WriteLine("Failed to send: " + str);
                Console.WriteLine(e);
            }
        }
    }
}