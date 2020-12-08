﻿using Dalamud;
using EasyHook;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Dalamud_CN_cli
{

    class Program
    {

        static void Main(string[] args)
        {
            //init
            Process gameProcess;
            var pid = -1;
            if (args.Length >= 1)
            {
                try
                {
                    pid = Convert.ToInt32(args[0]);
                }
                catch
                {
                    throw new Exception("the first argument should be the pid of ffxiv game");
                }

            }

            try
            {
                if (pid == -1)
                {
                    gameProcess = Process.GetProcessesByName("ffxiv_dx11")[0];
                }
                else
                {
                    gameProcess = Process.GetProcessById(pid);
                }
            }
            catch (Exception)
            {

                throw new Exception("can not find ffxiv process");
            }
               

            var lang = ClientLanguage.ChineseSimplified;
            if (args.Length >= 2)
            {
                try
                {
                    lang = (ClientLanguage)Convert.ToInt32(args[1]);
                }
                catch
                {
                    throw new Exception("the second argument should be the language enum");
                }
            }
                

            // File check
            var libPath = Path.GetFullPath("Dalamud.dll");
            var pluginPath = Path.GetDirectoryName(libPath);

            if (!File.Exists(libPath))
            {
                Console.WriteLine("can not find dalamud.dll");
                return;
            }

            var xivLauncherPath = Environment.ExpandEnvironmentVariables(@"%appdata%");//国际服only

            var loadPath = lang == ClientLanguage.Japanese ? xivLauncherPath : pluginPath;

            //构建command line
            var command = new DalamudStartInfo
            {
                WorkingDirectory = pluginPath,
                ConfigurationPath = loadPath + @"\XIVLauncher\dalamudConfig.json",
                PluginDirectory = loadPath + @"\XIVLauncher\installedPlugins",
                DefaultPluginDirectory = loadPath + @"\XIVLauncher\devPlugins",
                GameVersion = GetGameVersion(gameProcess),
                Language = lang
            };

            CleanDalamudLog();

            try
            {
                Thread.Sleep(500);
                RemoteHooking.Inject(gameProcess.Id, InjectionOptions.DoNotRequireStrongName, libPath, libPath, command);
            }
            catch (Exception)
            {
                throw;
            }

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

        private static void CleanDalamudLog()
        {
            var logpath = "dalamud.txt";
            var logFileInfo = new FileInfo(logpath);
            if (logFileInfo.Exists)
            {
                if (logFileInfo.Length > 5000000)
                {
                    try
                    {
                        File.Delete(logpath);
                    }
                    catch (Exception)
                    {

                    }
                }
            }
        }
    }
}
