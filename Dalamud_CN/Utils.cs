using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Dalamud_CN
{
    internal static class Utils
    {
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

        public static List<Process> GetGameProcess()
        {
            var list = new List<Process>();
            list.AddRange(Process.GetProcessesByName("ffxiv_dx11"));
            if(list.Count == 0)
            {
                var haveDx9 = Process.GetProcessesByName("ffxiv").Length > 0;
                if (haveDx9)
                {
                    throw new GameRunInDX9Exception("Game run in dx9!");
                }
            }
            return list;
        }
        public static string GetGameVersion(Process p)
        {
            var gameDirectory = GetProcessPath(p.Id);
            return File.ReadAllText(Path.Combine(Path.GetDirectoryName(gameDirectory), "ffxivgame.ver"));
        }
    }
}
