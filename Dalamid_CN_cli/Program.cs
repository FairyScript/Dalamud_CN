using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Text;
using System.Threading.Tasks;

namespace Dalamud_CN_cli
{
    public enum ClientLanguage
    {
        Japanese,
        English,
        German,
        French,
        Chinese
    }

    class Program
    {

        static void Main(string[] args)
        {
            //init
            Process gameProcess;
            try
            {
                var pid = Convert.ToInt32(args[0]);
                if(pid == -1)
                {
                    gameProcess = Process.GetProcessesByName("ffxiv_dx11")[0];
                }
                else
                {
                    gameProcess = Process.GetProcessById(pid);
                }
            }
            catch
            {
                throw new Exception("the first argument should be the pid of ffxiv game");
            }

            var lang = ClientLanguage.Chinese;
            try
            {
                lang = (ClientLanguage)Convert.ToInt32(args[1]);
            }
            catch
            {
                throw new Exception("the second argument should be the language enum");
            }

            var pluginPath = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            var xivLauncherPath = Environment.ExpandEnvironmentVariables(@"%appdata%");//国际服only

            var loadPath = lang == ClientLanguage.Chinese ? pluginPath : xivLauncherPath;

            InjectorCommand command = new InjectorCommand
            {
                ConfigurationPath = loadPath + @"\XIVLauncher\dalamudConfig.json",
                PluginDirectory = loadPath + @"\XIVLauncher\installedPlugins",
                DefaultPluginDirectory = loadPath + @"\XIVLauncher\devPlugins",
                GameVersion = GetGameVersion(gameProcess),
                Language = lang
            };
            var json = JsonConvert.SerializeObject(command);
            var commandLine = Convert.ToBase64String(Encoding.UTF8.GetBytes(json));
            Console.WriteLine(commandLine);
            Process p = new Process();
            p.StartInfo.FileName = pluginPath + @"\Dalamud.Injector.exe";
            p.StartInfo.UseShellExecute = false;
            p.StartInfo.Arguments = $"{gameProcess.Id} {commandLine}";
            p.Start();

        }

        public static string GetProcessPath(int processId)
        {
            string MethodResult = "";
            try
            {
                string Query = "SELECT ExecutablePath FROM Win32_Process WHERE ProcessId = " + processId;

                using (ManagementObjectSearcher mos = new ManagementObjectSearcher(Query))
                {
                    using (ManagementObjectCollection moc = mos.Get())
                    {
                        string ExecutablePath = (from mo in moc.Cast<ManagementObject>() select mo["ExecutablePath"]).First().ToString();

                        MethodResult = ExecutablePath;

                    }

                }

            }
            catch //(Exception ex)
            {
                //ex.HandleException();
            }
            return MethodResult;
        }

        public static string GetGameVersion(Process p)
        {
            var gameDirectory = GetProcessPath(p.Id);
            return File.ReadAllText(Path.Combine(Path.GetDirectoryName(gameDirectory), "ffxivgame.ver"));
        }
    }

    /// <summary>
    /// 注入器的参数model
    /// </summary>
    class InjectorCommand
    {
        public string WorkingDirectory { get; } = null;
        public string ConfigurationPath { get; set; }
        public string PluginDirectory { get; set; }
        public string DefaultPluginDirectory { get; set; }
        public ClientLanguage Language { get; set; } = ClientLanguage.Chinese;
        public string GameVersion { get; set; }
    }
}
