using System.Timers;
using DiscordRPC;
using DiscordRPC.Logging;
using Newtonsoft.Json;
using VkNet;
using VkNet.Enums.Filters;
using VkNet.Model;
using Application = System.Windows.Forms.Application;
using Console = Colorful.Console;
using Settings = VkMusicRpc.Models.Settings;
using Timer = System.Timers.Timer;

namespace VkMusicRpc;

internal class Program
{
    private const string _settingsFileName = "settings.ini";
    private static Settings _settings;
    private static DiscordRpcClient _rpcClient;
    private static readonly Timer _updateTimer = new(TimeSpan.FromSeconds(5).TotalMilliseconds);
    private static readonly VkApi _vkApi = new();
    private static readonly WindowStateWatcher _windowStateWatcher = new(NativeMethods.GetConsoleWindow());
    private static bool _createdNew;
    private static Mutex _mutex = new(true, "VkMusicRpc", out _createdNew);

    private static readonly NotifyIcon _notificationIcon = new()
    {
        Text = "VkMusicRpc",
        Icon = new Icon(SystemIcons.Application, 40, 40)
    };

    private static void Main(string[] args)
    {
        // Notification icon setup
        if (args.Length != 0 && args.Contains("-quietStart")) Task.Factory.StartNew(() => HideToTray());

        // Mutex check
        if (!_createdNew)
        {
            MessageBox.Show("VkMusicRpc is already running", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            Environment.Exit(0);
        }

        // Read settings
        if (File.Exists(_settingsFileName))
        {
            try
            {
                _settings = JsonConvert.DeserializeObject<Settings>(File.ReadAllText(_settingsFileName));
            }
            catch (Exception e)
            {
                Console.WriteLine("Error while trying to read settings file", Color.Red);
                Console.ReadLine();
                Environment.Exit(0);
            }

            if (string.IsNullOrWhiteSpace(_settings.UserID) || string.IsNullOrWhiteSpace(_settings.AppToken) ||
                _settings.UserID == null || _settings.AppToken == null)
            {
                Console.WriteLine("Empty config file. Please edit it!", Color.Red);
                Console.ReadLine();
                Environment.Exit(0);
            }
        }
        else
        {
            _settings = new Settings();
            File.WriteAllText(_settingsFileName, JsonConvert.SerializeObject(_settings, Formatting.Indented));
            Console.WriteLine("Empty config file. Please edit it!", Color.Red);
            Environment.Exit(0);
        }

        // Log into VK
        _vkApi.Authorize(new ApiAuthParams
        {
            AccessToken = _settings.AppToken
        });

        // Set client up
        _rpcClient = new DiscordRpcClient("543726720289734656");

        // RPC Logger
        _rpcClient.Logger = new ConsoleLogger { Level = LogLevel.Error };

        //Subscribe to events
        _notificationIcon.MouseDoubleClick += delegate { Task.Factory.StartNew(() => ShowFromTray()); };
        _windowStateWatcher.WindowMinimized += delegate { Task.Factory.StartNew(() => HideToTray()); };
        _rpcClient.OnReady += (sender, e) => { Console.WriteLine("Received Ready from user {0}", e.User.Username); };
        _rpcClient.OnPresenceUpdate += (sender, e) => { Console.WriteLine("Received Update! {0}", e.Presence); };
        _updateTimer.Elapsed += UpdateTimerOnElapsed;

        //Connect to the RPC
        _rpcClient.Initialize();

        // Set timer up
        _updateTimer.Start();

        Console.ReadLine();
    }

    public static void ShowFromTray()
    {
        var handle = NativeMethods.GetConsoleWindow();

        // Show
        NativeMethods.ShowWindow(handle, NativeMethods.SW_SHOW);
        NativeMethods.SetForegroundWindow(handle);
        _notificationIcon.Visible = false;
    }

    private static void HideToTray()
    {
        var handle = NativeMethods.GetConsoleWindow();

        // Hide
        NativeMethods.ShowWindow(handle, NativeMethods.SW_HIDE);
        _notificationIcon.Visible = true;
        Application.Run();
    }

    private static void UpdateTimerOnElapsed(object? sender, ElapsedEventArgs e)
    {
        var res = _vkApi.Users.Get(new List<string> { _settings.UserID }, ProfileFields.Status);

        if (res != null && res.Count > 0)
        {
            var statusAudio = res[0].StatusAudio;

            if (statusAudio != null)
                _rpcClient.SetPresence(new RichPresence
                {
                    State = "Автор - " + statusAudio.Artist,
                    Details = "Трек - " + statusAudio.Title,
                    Assets = new Assets
                    {
                        LargeImageKey = "vk"
                    }
                });
            else
                _rpcClient.SetPresence(new RichPresence
                {
                    State = "Музыка не воспроизводится",
                    Assets = new Assets
                    {
                        LargeImageKey = "vk"
                    }
                });
        }
    }
}