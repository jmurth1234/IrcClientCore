using System;
using System.IO;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace IrcClientCore
{
    class IrcSocket : Irc
    {
        private TcpClient conn;
        private NetworkStream stream;
        private StreamReader clientStreamReader;
        private StreamWriter clientStreamWriter;

        public IrcSocket(IrcServer server) : base(server)
        {
        }

        public override async void Connect()
        {
            if (server == null)
                return;

            IsAuthed = false;
            ReadOrWriteFailed = false;
            conn = new TcpClient();
            conn.NoDelay = true;
            try
            {
                await conn.ConnectAsync(server.hostname, server.port);

                if (conn.Connected)
                {
                    stream = conn.GetStream();
                    clientStreamReader = new StreamReader(stream);
                    clientStreamWriter = new StreamWriter(stream);

                    AttemptAuth();

                    while (true)
                    {
                        var line = await clientStreamReader.ReadLineAsync();
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

            if (attemptReconnect)
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

        public override async Task WriteLine(string str)
        {
            await clientStreamWriter.WriteLineAsync(str);
            await clientStreamWriter.FlushAsync();
        }
    }
}