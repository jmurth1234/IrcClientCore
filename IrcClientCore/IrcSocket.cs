using System;
using System.IO;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

namespace IrcClientCore
{
    public class IrcSocket : ISocket
    {
        private TcpClient _conn;
        private Stream _stream;
        private StreamReader _clientStreamReader;
        private StreamWriter _clientStreamWriter;
        private Irc parent;

        private IrcServer Server => parent.Server;

        public IrcSocket(Irc parent)
        {
            this.parent = parent;
        }

        public async Task Connect()
        {

            if (Server == null)
                return;

            parent.IsConnecting = true;
            parent.IsAuthed = false;
            parent.ReadOrWriteFailed = false;
            _conn = new TcpClient {NoDelay = true};
            try
            {
                await _conn.ConnectAsync(Server.Hostname, Server.Port);

                if (!_conn.Connected) return;
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

                parent.IsConnecting = false;
                parent.IsConnected = true;
                parent.AttemptAuth();

                while (true)
                {
                    var line = await _clientStreamReader.ReadLineAsync();
                    await parent.RecieveLine(line);
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

        public async Task Disconnect(string msg = "Powered by WinIRC", bool attemptReconnect = false)
        {
            WriteLine("QUIT :" + msg);
            parent.IsConnected = false;

            if (Server.ShouldReconnect && attemptReconnect)
            {
                parent.IsConnecting = true;
                parent.ReconnectionAttempts++;

                await Task.Run(async () =>
                {
                    if (parent.ReconnectionAttempts < 3)
                        await Task.Delay(1000);
                    else
                        await Task.Delay(60000);

                    if (parent.IsConnecting)
                        Connect();
                });
            }
            else
            {
                parent.IsConnecting = false;
                parent.HandleDisconnect?.Invoke(parent);
            }
        }

        public async Task WriteLine(string str)
        {
            try
            {
                await _clientStreamWriter.WriteLineAsync(str);
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