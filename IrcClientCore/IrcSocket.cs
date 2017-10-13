using System;
using System.IO;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace IrcClientCore
{
    public class IrcSocket : Irc
    {
        private TcpClient _conn;
        private NetworkStream _stream;
        private StreamReader _clientStreamReader;
        private StreamWriter _clientStreamWriter;

        public IrcSocket(IrcServer server) : base(server)
        {
        }

        public override async void Connect()
        {
            if (Server == null)
                return;

            IsAuthed = false;
            ReadOrWriteFailed = false;
            _conn = new TcpClient();
            _conn.NoDelay = true;
            try
            {
                await _conn.ConnectAsync(Server.Hostname, Server.Port);

                if (_conn.Connected)
                {
                    _stream = _conn.GetStream();
                    _clientStreamReader = new StreamReader(_stream);
                    _clientStreamWriter = new StreamWriter(_stream);

                    AttemptAuth();

                    while (true)
                    {
                        var line = await _clientStreamReader.ReadLineAsync();
                        await HandleLine(line);
                    }
                }
            } 
            catch (Exception e) 
            { 
                Console.WriteLine(e.Message);
                Console.WriteLine(e.StackTrace);
            }
        }

        public override void DisconnectAsync(string msg = "Powered by WinIRC", bool attemptReconnect = false)
        {
            WriteLine("QUIT :" + msg);

            if (Server.ShouldReconnect && attemptReconnect)
            {
                IsReconnecting = true;
                ReconnectionAttempts++;

                Task.Run(async () => {
                    if (ReconnectionAttempts < 3)
                        await Task.Delay(1000);
                    else
                        await Task.Delay(60000);

                    if (IsReconnecting)
                        Connect();
                }).Start();
            }
            else
            {
                IsConnected = false;
                HandleDisconnect?.Invoke(this);
            }
        }

        public override void WriteLine(string str)
        {
            _clientStreamWriter.WriteLine(str);
            _clientStreamWriter.Flush();
        }
    }
}