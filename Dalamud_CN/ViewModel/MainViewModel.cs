using Dalamud;
using EasyHook;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.CommandWpf;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;

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
        #region define
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
                if (value != canInject)
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
                    WatchGame();
                }
            }
        }



        public RelayCommand RefreshListCommand { get; set; }
        public RelayCommand InjectCommand { get; set; }
        public RelayCommand<bool> AutoInjectCommand { get; set; }

        System.Timers.Timer timer;
        System.Timers.Timer injectTimer = new System.Timers.Timer
        {
            Interval = 2000,
            AutoReset = false,
        };

        Task gameWatchDog;
        CancellationTokenSource token;

        #endregion

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
            timer = new System.Timers.Timer
            {
                Interval = 500,
                AutoReset = true
            };
            timer.Elapsed += (_, ee) => FindGameProcess();
            timer.Start();

            //初始化注入Timer
            injectTimer.Elapsed += (_, ee) => StartInject();

            //尝试清理log file
            Task.Run(() => CleanDalamudLog());

        }

        private void CleanDalamudLog()
        {
            var logpath = "dalamud.txt";
            var logFileInfo = new FileInfo(logpath);
            if (logFileInfo.Exists)
            {
                if(logFileInfo.Length > 5000000)
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

        /// <summary>
        /// Watch Dog
        /// </summary>
        private void WatchGame()
        {
            if (gameWatchDog != null && !gameWatchDog.IsCompleted) token.Cancel();
            if (GameProcess == null) return;
            token = new CancellationTokenSource();
            gameWatchDog = Task.Run(() => GameProcess.WaitForExit()).ContinueWith(t =>
            {
                timer.Start();
            }, token.Token);
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
            // File check
            var libPath = Path.GetFullPath("Dalamud.dll");
            var pluginPath = Path.GetDirectoryName(libPath);

            if (!File.Exists(libPath))
            {
                MessageBox.Show($"Can't find a dll on {libPath}");
                return;
            }

            //构建command line
            var command = new DalamudStartInfo
            {
                WorkingDirectory = pluginPath,
                ConfigurationPath = pluginPath + @"\XIVLauncher\dalamudConfig.json",
                PluginDirectory = pluginPath + @"\XIVLauncher\installedPlugins",
                DefaultPluginDirectory = pluginPath + @"\XIVLauncher\devPlugins",
                GameVersion = Utils.GetGameVersion(GameProcess),
                Language = ClientLanguage.ChineseSimplified
            };
            
            try
            {
                RemoteHooking.Inject(GameProcess.Id, InjectionOptions.DoNotRequireStrongName, libPath, libPath, command);
            }
            catch (Exception e)
            {
                MessageBox.Show(e.StackTrace);
            }

            if (AutoExit)
            {
                Environment.Exit(0);
            }

        }

    }
}
