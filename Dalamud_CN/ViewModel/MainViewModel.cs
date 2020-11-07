using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.CommandWpf;
using Newtonsoft.Json;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Reflection;
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
        //private bool autoExit = true;
        public bool AutoExit
        {
            get => Properties.Settings.Default.AutoExit;
            set
            {
                if (value != Properties.Settings.Default.AutoExit)
                {
                    Properties.Settings.Default.AutoExit = value;
                    Properties.Settings.Default.Save();
                    RaisePropertyChanged();
                }
            }
        }

        //private bool autoInject = false;
        public bool AutoInject 
        {
            get => Properties.Settings.Default.AutoInject;
            set
            {
                if (value != Properties.Settings.Default.AutoInject)
                {
                    Properties.Settings.Default.AutoInject = value;
                    Properties.Settings.Default.Save();
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

        readonly string pluginPath = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);

        public RelayCommand RefreshListCommand { get; set; }
        public RelayCommand InjectCommand { get; set; }
        public RelayCommand<bool> AutoInjectCommand { get; set; }

        Timer timer;
        Timer injectTimer = new Timer
        {
            Interval = 2000,
            AutoReset = false,
        };

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

            //Title Version
            Application.Current.MainWindow.Title += $" Ver {Assembly.GetExecutingAssembly().GetName().Version}";

            //初始化Command
            RefreshListCommand = new RelayCommand(FindGameProcess);
            InjectCommand = new RelayCommand(StartInject);
            AutoInjectCommand = new RelayCommand<bool>(isChecked => AutoInjectFunc(isChecked));

            //初始化进程Timer
            timer = new Timer
            {
                Interval = 500,
                AutoReset = true
            };
            timer.Elapsed += (_, ee) => FindGameProcess();
            timer.Start();

            //初始化注入Timer
            injectTimer.Elapsed += (_, ee) => StartInject();
        }

        private void AutoInjectFunc(bool isChecked)
        {
            if (isChecked)
            {

            }
            else
            {
                injectTimer.Stop();
            }
        }

        /// <summary>
        /// 查找游戏进程方法
        /// </summary>
        void FindGameProcess()
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                GameList.Clear();
                try
                {
                    foreach (var item in Utils.GetGameProcess())
                    {
                        GameList.Add(item);
                    }

                    if (GameList.Count > 0)
                    {
                        GameProcess = GameList[0];
                        timer.Stop();
                        if (AutoInject)
                        {
                            injectTimer.Start();
                        }
                    }
                    else
                    {
                        if (!timer.Enabled) timer.Start();
                    }
                }
                catch (GameRunInDX9Exception)
                {
                    timer.Stop();

                    MessageBox.Show("请以DX11启动游戏!");
                }

            });

        }

        private void StartInject()
        {
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

        /// <summary>
        /// 注入器的参数model
        /// </summary>
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