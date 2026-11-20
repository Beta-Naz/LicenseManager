using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace Client
{
    class Program
    {
        static bool _isConnect = false;
        static IPAddress ServerIpAddress;
        static int ServerPort;
        static string _login;
        static string _password;

        static string ClientToken;
        static DateTime ClientDateConnection;
        static void Main(string[] args)
        {
            OnSettings();
            Thread tCheckToken = new Thread(CheckToken);
            tCheckToken.Start();
            while (true)
                SetCommand();
        }

        static void SetCommand()
        {
            Console.ForegroundColor = ConsoleColor.Red;
            string Command = Console.ReadLine();

            if (Command == "/config")
            {
                File.Delete(Directory.GetCurrentDirectory() + "/.config");
                OnSettings();
            }
            else if (Command.Contains("/connect"))
            {
                string[] loginAndPassword = Command.Replace("/connect", "").Trim().Split(' ');
                if(loginAndPassword.Length == 2)
                {
                    _login = loginAndPassword[0];
                    _password = loginAndPassword[1];
                    ConnectServer();
                }
                else if(!string.IsNullOrWhiteSpace(_login) && !string.IsNullOrWhiteSpace(_password))
                {
                    ConnectServer();
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("to: /connect [login] [password]");
                }
            }
            else if (Command == "/status") GetStatus();
            else if (Command == "/help") Help();
            else
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("Unknown command");
            }
        }

        static void ConnectServer()
        {
            try
            {
                if (_isConnect)
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("Already connected to the server");
                    return;
                }
                IPEndPoint EndPoint = new IPEndPoint(ServerIpAddress, ServerPort);
                Socket Socket = new Socket(
                    AddressFamily.InterNetwork,
                    SocketType.Stream,
                    ProtocolType.Tcp);
                Socket.ReceiveTimeout = 5000;
                Socket.SendTimeout = 5000;
                try
                {
                    Socket.Connect(EndPoint);
                }
                catch (Exception ex)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("Error: " + ex.Message);
                }
                if (Socket.Connected)
                {
                    try
                    {
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine("Connection to server successful");

                        Socket.Send(Encoding.UTF8.GetBytes($"/token {_login} {_password}"));

                        byte[] Bytes = new byte[10485760];
                        int ByteRec = Socket.Receive(Bytes);

                        string Response = Encoding.UTF8.GetString(Bytes, 0, ByteRec);
                        if (Response == "/limit")
                        {
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.WriteLine("There is not enough space on the license server");
                        }
                        else if (Response.Split(':')[0] == "/disconnect")
                        {
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.WriteLine(Response.Replace("/disconnect:", "").Trim());
                            ClientToken = null;
                            _isConnect = false; 
                        }
                        else
                        {
                            ClientToken = Response;
                            ClientDateConnection = DateTime.Now;
                            Console.ForegroundColor = ConsoleColor.White;
                            Console.WriteLine("Received connection token: " + ClientToken);
                            _isConnect = true;
                        }
                    }
                    catch (SocketException sex)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("Server did not respond: " + sex.Message);
                        _isConnect = false;
                    }
                    finally
                    {
                        Socket.Close();
                    }
                }
            }
            catch(Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Server time out " + ex);
                _isConnect = false;
            }
        
        }

        static void CheckToken()
        {
            while (true)
            {
                if(!string.IsNullOrEmpty(ClientToken))
                {
                    IPEndPoint EndPoint = new IPEndPoint(ServerIpAddress, ServerPort);
                    Socket Socket = new Socket(
                        AddressFamily.InterNetwork,
                        SocketType.Stream,
                        ProtocolType.Tcp);
                    Socket.ReceiveTimeout = 5000;
                    Socket.SendTimeout = 5000;
                    try
                    {
                        Socket.Connect(EndPoint);
                    }
                    catch (Exception ex)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("Error: " + ex.Message);
                    }
                    if (Socket.Connected)
                    {
                        try
                        {
                            Socket.Send(Encoding.UTF8.GetBytes(ClientToken));

                            byte[] Bytes = new byte[10485760];
                            int ByteRec = Socket.Receive(Bytes);

                            string Response = Encoding.UTF8.GetString(Bytes, 0, ByteRec);
                            if (Response.Contains("/disconnect"))
                            {
                                Response = Response.Replace("/disconnect", "").Trim();
                                if (string.IsNullOrWhiteSpace(Response))
                                {
                                    Console.ForegroundColor = ConsoleColor.Red;
                                    Console.WriteLine("The client is disconnected from the server");
                                }
                                else
                                {
                                    Console.ForegroundColor = ConsoleColor.Red;
                                    Console.WriteLine(Response);
                                }
                                ClientToken = String.Empty;
                                _isConnect = false;
                            }
                        }
                        catch (SocketException sex)
                        {
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.WriteLine("Server did not respond: " + sex.Message);
                        }
                        finally
                        {
                            Socket.Close();
                        }

                    }
                }
                Thread.Sleep(1000);
            }
            
        }
        static void GetStatus()
        {
            int Duration = (int)DateTime.Now.Subtract(ClientDateConnection).TotalSeconds;
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine($"Client: {ClientToken}, time connection: {ClientDateConnection.ToString("HH:mm:ss dd.MM")}," +
                $"duration: {Duration}");
        }

        static void Help()
        {
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("Commands to the server:");

            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write("/config");
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine(" - set initial serrings");

            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write("/connect");
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine(" - connection to the server");

            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write("/status");
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine(" - show list users");

        }


        static void OnSettings()
        {
            string Path = Directory.GetCurrentDirectory() + "/.config";
            string IpAddress = "";

            if (File.Exists(Path))
            {
                StreamReader streamReader = new StreamReader(Path);
                IpAddress = streamReader.ReadLine();
                ServerIpAddress = IPAddress.Parse(IpAddress);
                ServerPort = int.Parse(streamReader.ReadLine());
                _login = streamReader.ReadLine();
                _password = streamReader.ReadLine();
                streamReader.Close();

                Console.ForegroundColor = ConsoleColor.White;
                Console.Write("Server address: ");
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine(IpAddress);

                Console.ForegroundColor = ConsoleColor.White;
                Console.Write("Server port: ");
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine(ServerPort.ToString());

                Console.ForegroundColor = ConsoleColor.White;
                Console.Write("Login: ");
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine(_login?.ToString());

                Console.ForegroundColor = ConsoleColor.White;
                Console.Write("Password: ");
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine(_password?.ToString());
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.White;
                Console.Write("Please provide the IP address if the license server: ");
                Console.ForegroundColor = ConsoleColor.Green;
                IpAddress = Console.ReadLine();
                ServerIpAddress = IPAddress.Parse(IpAddress);

                Console.ForegroundColor = ConsoleColor.White;
                Console.Write("Please specify the license server port: ");
                Console.ForegroundColor = ConsoleColor.Green;
                ServerPort = int.Parse(Console.ReadLine());

                Console.ForegroundColor = ConsoleColor.White;
                Console.Write("Please specify login: ");
                Console.ForegroundColor = ConsoleColor.Green;
                _login = Console.ReadLine();

                Console.ForegroundColor = ConsoleColor.White;
                Console.Write("Please specify password: ");
                Console.ForegroundColor = ConsoleColor.Green;
                _password = Console.ReadLine();

                StreamWriter streamWriter = new StreamWriter(Path);
                streamWriter.WriteLine(IpAddress);
                streamWriter.WriteLine(ServerPort.ToString());
                streamWriter.WriteLine(_login);
                streamWriter.WriteLine(_password);
                streamWriter.Close();
            }

            Console.ForegroundColor = ConsoleColor.White;
            Console.Write("To change, write the command: ");
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("/config");
        }
    }
}
