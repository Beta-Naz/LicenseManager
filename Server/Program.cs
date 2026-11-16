using System.IO;
using System.Net;

namespace Server
{
    public class Program
    {
        private static IPAddress? _serverIpAddress;
        private static int _serverPort;
        private static int _maxClient;
        private static int _duration;
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
            while (true)
            {
                SetCommand();
            }
        }
        private static void SetCommand()
        {
            OnConsole("", TypeOutput.Red);
            string command = Console.ReadLine() ?? "";
            switch (command)
            {
                case "/config": 
                    break;
                case "/connect":
                    break;
                case "/status":
                    break;
                case "/help":
                    Help();
                    break;
                default: 
                    OnConsole("Unknown Command", TypeOutput.Yellow);
                    break;
            }
        }
        static void Help()
        {
            OnConsole("Command to the server: ", TypeOutput.White);
            OnConsole("/config ", TypeOutput.Green, false);
            OnConsole("- set initial serrings", TypeOutput.White);
            OnConsole("/connect ", TypeOutput.Green, false);
            OnConsole("- connection to the server", TypeOutput.White);
            OnConsole("/status ", TypeOutput.Green, false);
            OnConsole("- show list user", TypeOutput.White);
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
                }
                OnConsole("Server address: ", TypeOutput.White);
                OnConsole(_serverIpAddress?.ToString() ?? "IP адрес не сохранен", TypeOutput.Green);
                OnConsole("Server port: ", TypeOutput.White);
                OnConsole(_serverPort != 0 ? _serverPort.ToString() : "Порт не сохранен", TypeOutput.Green);
            }
            else
            {
                OnConsole("Please provide the IP address if the license server: ", TypeOutput.White);
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
                using (var streamWriter = new StreamWriter(path))
                {
                    streamWriter.WriteLine(_serverIpAddress.ToString());
                    streamWriter.WriteLine(_serverPort.ToString());
                }
                OnConsole("To change, write the command: ", TypeOutput.White, false);
                OnConsole("/config", TypeOutput.Green);
            }
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