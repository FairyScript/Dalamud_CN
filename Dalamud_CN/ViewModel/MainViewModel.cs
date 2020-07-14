using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.CommandWpf;
using Newtonsoft.Json;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Threading;

namespace Dalamud_CN
{
    /// <summary>
    /// This class contains properties that the main View can data bind to.
    /// <para>
    /// Use the <strong>mvvminpc</strong> snippet to add bindable properties to this ViewModel.
    /// </para>
    /// <para>
    /// You can also use Blend to data bind with the tool's support.
    /// </para>
    /// <para>
    /// See http://www.galasoft.ch/mvvm
    /// </para>
    /// </summary>
    public class MainViewModel : ViewModelBase
    {
        private bool auto = true;
        public bool AutoExit
        {
            get => auto;
            set
            {
                if (value != auto)
                {
                    auto = value;
                    RaisePropertyChanged();
                }
            }
        }

        private bool canInject = false;
        public bool CanInject
        {
            get => canInject;
            set
            {
                if(value != canInject)
                {
                    canInject = value;
                    RaisePropertyChanged();
                }
            }
        }
        public ObservableCollection<Process> GameList { get; set; } = new ObservableCollection<Process>();

        private Process game;
        public Process GameProcess
        {
            get => game;
            set
            {
                if(value != game)
                {
                    game = value;
                    RaisePropertyChanged();
                    CanInject = (value != null);
                }
            }
        }
        public RelayCommand RefreshListCommand { get; set; }
        public RelayCommand InjectCommand { get; set; }
        void FindGameProcess()
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                GameList.Clear();
                foreach (var item in Utils.GetGameProcess())
                {
                    GameList.Add(item);
                }

                if (GameList.Count > 0)
                {
                    GameProcess = GameList[0];
                    timer.Stop();
                }
                else
                {
                    timer.Start();
                }
            });
            
        }

        Timer timer;

        /// <summary>
        /// Initializes a new instance of the MainViewModel class.
        /// </summary>
        public MainViewModel()
        {
            ////if (IsInDesignMode)
            ////{
            ////    // Code runs in Blend --> create design time data.
            ////}
            ////else
            ////{
            ////    // Code runs "for real"
            ////}

            RefreshListCommand = new RelayCommand(FindGameProcess);
            InjectCommand = new RelayCommand(StartInject);

            timer = new Timer
            {
                Interval = 500,
                AutoReset = true
            };
            timer.Elapsed += (_, ee) => FindGameProcess();
            timer.Start();
        }
        private void StartInject()
        {
            var pluginPath = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            var command = new InjectorCommand
            {
                ConfigurationPath = pluginPath + @"\XIVLauncher\dalamudConfig.json",
                PluginDirectory = pluginPath + @"\XIVLauncher\installedPlugins",
                DefaultPluginDirectory = pluginPath + @"\XIVLauncher\devPlugins",
                GameVersion = Utils.GetGameVersion(GameProcess)
            };

            var json = JsonConvert.SerializeObject(command);
            var commandLine = Convert.ToBase64String(Encoding.UTF8.GetBytes(json));

            Process p = new Process();
            p.StartInfo.FileName = pluginPath + @"\Dalamud.Injector.exe";
            p.StartInfo.Arguments = $"{GameProcess.Id} {commandLine}";
            if (AutoExit)
            {
                p.EnableRaisingEvents = true;
                p.Exited += (_, ex) =>
                {
                    Environment.Exit(0);
                };
            }
            p.Start();
        }
        class InjectorCommand
        {
            public string WorkingDirectory { get; } = null;
            public string ConfigurationPath { get; set; }
            public string PluginDirectory { get; set; }
            public string DefaultPluginDirectory { get; set; }
            public int Language { get; } = 4;
            public string GameVersion { get; set; }
        }

    }
}