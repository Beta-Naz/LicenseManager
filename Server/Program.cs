using Server.Classes;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Server
{
    public class Program
    {
        private static IPAddress? _serverIpAddress;
        private static int _serverPort;
        private static int _maxClient;
        private static int _duration;
        private static List<ClientService> _allClient = new List<ClientService>();
        private static Dictionary<string, string> _keyValuePairs = new Dictionary<string, string>();
        private enum TypeOutput
        {
            White,
            Green,
            Yellow,
            Red,
        }
        public static void Main(string[] args)
        {
            OnSetting();
            Thread tListner = new Thread(StartServer);
            tListner.Start();

            Thread tDisconnect = new Thread(CheckDisconnectClient);
            tDisconnect.Start();

            while (true)
            {
                SetCommand();
            }
        }
        static void CheckDisconnectClient()
        {
            while (true)
            {
                for(int iClient = 0;  iClient < _allClient.Count; iClient++)
                {
                    int duration = (int)DateTime.Now.Subtract(_allClient[iClient].LastDateConnect).TotalSeconds;
                    if(duration > _duration)
                    {
                        OnConsole($"Client: {_allClient[iClient].Token} disconnect from server due time out", TypeOutput.Red);
                        _allClient.Remove(_allClient[iClient]);
                    }
                }
                Thread.Sleep(1000);
            }
        }
        private static void UserBan(string command)
        {
            command = command.Replace("/ban", "").Trim();
            using(var db = new DbManager())
            {
                var user = db.Users.Where(user => user.Login == command).FirstOrDefault();
                if (user == null)
                {
                    OnConsole($"The user don't find login: {command}", TypeOutput.Yellow);
                }
                else
                {
                    user.IsBan = true;
                    OnConsole($"{command} is ban", TypeOutput.Green);
                    var client = _allClient.Where(c => c.Token == _keyValuePairs[user.Login]).FirstOrDefault();
                    if(client != null)
                    {
                        OnConsole($"Client: {client.Token} disconnect from server", TypeOutput.Red);
                        _allClient.Remove(client);
                    }
                }
                db.SaveChanges();
            }
        }
        private static void UserUnlocking(string command)
        {
            command = command.Replace("/unlocking", "").Trim();
            using (var db = new DbManager())
            {
                var user = db.Users.Where(user => user.Login == command).FirstOrDefault();
                if (user == null)
                {
                    OnConsole($"The user don't find login: {command}", TypeOutput.Yellow);
                }
                else
                {
                    user.IsBan = false;
                    OnConsole($"{command} is unlocking", TypeOutput.Green);
                }
                db.SaveChanges();
            }
        }
        private static string SetCommandClient(string command)
        {
            try
            {
                if (command.Contains("/token"))
                {
                    string[] loginAndPassword = command.Replace("/token", "").Trim().Split(' ');
                    string login = loginAndPassword[0];
                    string password = loginAndPassword[1];
                    using (var db = new DbManager())
                    {
                        var user = db.Users.Where(user => user.Password == password && user.Login == login).FirstOrDefault();
                        if(user == null)
                        {
                            OnConsole($"The user entered an incorrect login or password: {login} {password}", TypeOutput.Yellow);
                            return "/disconnect: Incorrect login or password";
                        }
                        else if (user.IsBan)
                        {
                            OnConsole($"The user is ban: {login}", TypeOutput.Yellow);
                            return $"/disconnect: User {login} is ban";
                        }
                    }

                    if (_allClient.Count < _maxClient)
                    {
                        ClientService newClient = new ClientService();
                        _keyValuePairs[login] = newClient.Token;
                        _allClient.Add(newClient);
                        OnConsole("New client connection: " + newClient.Token, TypeOutput.Green);
                        return newClient.Token;
                    }
                    else
                    {
                        OnConsole("There is not enough space on the license server", TypeOutput.Red);
                        return "/limit";
                    }
                }
                else
                {
                    var client = _allClient.Find(c => c.Token == command);
                    return client != null ? "/connect" : "/disconnect";
                }
            }
            catch(Exception ex)
            {
                OnConsole($"Error: {ex.Message}", TypeOutput.Red);
                return "/disconnect";
            }
          
        }
        private static void SetCommand()
        {
            OnConsole("", TypeOutput.Red, false);
            string command = Console.ReadLine() ?? "";
            if (command.Contains("/disconnect"))
            {
                DisconnectServer(command);
            }
            else if (command.Contains("/ban"))
            {
                UserBan(command);
            }
            else if (command.Contains("/unlocking"))
            {
                UserUnlocking(command);
            }
            else
            {
                switch (command)
                {
                    case "/config":
                        File.Delete(Directory.GetCurrentDirectory() + "/.config");
                        OnSetting();
                        break;
                    case "/status":
                        GetStatus();
                        break;
                    case "/help":
                        Help();
                        break;
                    default:
                        OnConsole("Unknown Command", TypeOutput.Yellow);
                        break;
                }
            }

        }
        static void DisconnectServer(string command)
        {
            try
            {
                string token = command.Replace("/disconnect", "").Trim();
                Classes.ClientService disconnectClinet = _allClient.Find(c => c.Token == token);
                _allClient.Remove(disconnectClinet);
                OnConsole($"Client: {token} disconnect from server", TypeOutput.Red);
            }
            catch(Exception ex)
            {
                OnConsole($"Error: {ex.Message}", TypeOutput.Red);
            }

        }
        static void Help()
        {
            OnConsole("Command to the server: ", TypeOutput.White);
            OnConsole("/config ", TypeOutput.Green, false);
            OnConsole("- set initial settings", TypeOutput.White);
            OnConsole("/disconnect ", TypeOutput.Green, false);
            OnConsole("- disconnection user from the server", TypeOutput.White);
            OnConsole("/ban ", TypeOutput.Green, false);
            OnConsole("- ban user to the server", TypeOutput.White);
            OnConsole("/status ", TypeOutput.Green, false);
            OnConsole("- show list user", TypeOutput.White);
        }
        static void GetStatus()
        {
            OnConsole($"Count clients: {_allClient.Count}", _allClient.Count == 0 ? TypeOutput.Red : TypeOutput.Green);

            foreach(var client in _allClient)
            {
                int duration = (int)DateTime.Now.Subtract(client.LastDateConnect).TotalSeconds;
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine($"Client: {client.Token}, time connection: {client.LastDateConnect.ToString("HH:mm:ss dd.MM")}," +
                    $"duration: {duration}");
            }
        }

        static void StartServer()
        {
            if (_serverIpAddress == null) return;
            IPEndPoint endPoint = new IPEndPoint(_serverIpAddress, _serverPort);
            Socket socketListner = new Socket(
                AddressFamily.InterNetwork, 
                SocketType.Stream, 
                ProtocolType.Tcp);
            socketListner.Bind(endPoint);
            socketListner.Listen(10);
            while (true)
            {
                Socket handler = socketListner.Accept();
                byte[] bytes = new byte[10485760];
                int byteRec = handler.Receive(bytes);
                string message = Encoding.UTF8.GetString(bytes, 0, byteRec);
                string response = SetCommandClient(message);
                if(response != null)
                {
                    handler.Send(Encoding.UTF8.GetBytes(response));
                }
            }

        }
        static void ClearData()
        {
            _allClient.Clear();
            _serverIpAddress = null;
            _serverPort = 0;
            _maxClient = 0;
            _duration = 0;
        }
        static void OnSetting()
        {
            string path = Directory.GetCurrentDirectory() + "/.config";
            if (File.Exists(path))
            {
                using (StreamReader streamReader = new StreamReader(path))
                {
                    _serverIpAddress = IPAddress.Parse(streamReader.ReadLine() ?? "");
                    _serverPort = int.Parse(streamReader.ReadLine() ?? "");
                    _maxClient = int.Parse(streamReader.ReadLine() ?? "");
                    _duration = int.Parse(streamReader.ReadLine() ?? "");
                }
                OnConsole("Server address: ", TypeOutput.White, false);
                OnConsole(_serverIpAddress?.ToString() ?? "IP адрес не сохранен", TypeOutput.Green);
                OnConsole("Server port: ", TypeOutput.White, false);
                OnConsole(_serverPort != 0 ? _serverPort.ToString() : "Порт не сохранен", TypeOutput.Green);
                OnConsole("Max count clients: ", TypeOutput.White, false);
                OnConsole(_maxClient.ToString(), TypeOutput.Green);
                OnConsole("Token lifetime: ", TypeOutput.White, false);
                OnConsole(_duration.ToString(), TypeOutput.Green);
            }
            else
            {
                ClearData();
                OnConsole("Please provide the IP address if the license server: ", TypeOutput.White, false);
                while(_serverIpAddress == null)
                {
                    OnConsole("", TypeOutput.Green);
                    _serverIpAddress = IPAddress.Parse(Console.ReadLine() ?? "");
                    if(_serverIpAddress == null)
                    {
                        OnConsole("Please provide the IP address if the license server: ", TypeOutput.Red, false);
                    }
                }
                OnConsole("Please specify the license server port: ", TypeOutput.White, false);
                while (_serverPort == 0)
                {
                    OnConsole("", TypeOutput.Green);
                    _serverPort = int.Parse(Console.ReadLine() ?? "");
                    if (_serverPort == 0)
                    {
                        OnConsole("Please specify the license server port: ", TypeOutput.Red, false);
                    }
                }
                OnConsole("Please indicate the  largest number of clients: ", TypeOutput.White, false);
                do
                {
                    OnConsole("", TypeOutput.Green);
                    _maxClient = int.Parse(Console.ReadLine() ?? "");
                    if (_maxClient <= 0)
                    {
                        OnConsole("Please indicate the  largest number of clients: ", TypeOutput.Red, false);
                    }
                }
                while (_maxClient <= 0);
                OnConsole("Please specify the token life time: ", TypeOutput.White, false);
                while (_duration <= 0)
                {
                    OnConsole("", TypeOutput.Green);
                    _duration = int.Parse(Console.ReadLine() ?? "");
                    if (_duration <= 0)
                    {
                        OnConsole("Please specify the token life time: ", TypeOutput.Red, false);
                    }
                }
                using (var streamWriter = new StreamWriter(path))
                {
                    streamWriter.WriteLine(_serverIpAddress.ToString());
                    streamWriter.WriteLine(_serverPort.ToString());
                    streamWriter.WriteLine(_maxClient.ToString());
                    streamWriter.WriteLine(_duration.ToString());
                }
            }
            OnConsole("To change, write the command: ", TypeOutput.White, false);
            OnConsole("/config", TypeOutput.Green);
        }

        private static void OnConsole(string text, TypeOutput type, bool inLine = true)
        {
            Console.ResetColor();
            Console.ForegroundColor = type switch
            {
                TypeOutput.Green => ConsoleColor.Green,
                TypeOutput.Yellow => ConsoleColor.Yellow,
                TypeOutput.Red => ConsoleColor.Red,
                _ => ConsoleColor.White,
            };
            if (!string.IsNullOrWhiteSpace(text))
            {
                if(inLine) Console.WriteLine(text);
                else Console.Write(text);
            }
        }
    }
}