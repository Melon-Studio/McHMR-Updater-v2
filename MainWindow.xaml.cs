using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using log4net;
using McHMR_Updater_v2.core;
using McHMR_Updater_v2.core.customException;
using McHMR_Updater_v2.core.entity;
using McHMR_Updater_v2.core.utils;
using Newtonsoft.Json;
using Wpf.Ui.Controls;

namespace McHMR_Updater_v2;

public partial class MainWindow : FluentWindow
{
    public static readonly ILog Log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

    private string token;
    private RestSharpClient client;

    public MainWindow()
    {
        InitializeComponent();

        Loaded += (sender, args) =>
        {
            Wpf.Ui.Appearance.SystemThemeWatcher.Watch(this);
        };
    }

    private void InitializationCheck() 
    {
        // 检查McHMR配置文件
        new ConfigurationCheck().check();
        // 检查API配置
        try
        {
            string apiUrl = ConfigureReadAndWriteUtil.GetConfigValue("apiUrl");
            if (string.IsNullOrEmpty(apiUrl))
            {
                Window startWindow = new StartWindow();
                startWindow.ShowDialog();
            }
        }
        catch
        {
            Window startWindow = new StartWindow();
            startWindow.ShowDialog();
        }
        
    }

    private void FluentWindow_Loaded(object sender, RoutedEventArgs e)
    {
        // 初始化
        progressBar.Visibility = Visibility.Collapsed;
        progressBarSpeed.Visibility = Visibility.Collapsed;
        tipText.Text = "正在获取最新版本";
        InitializationCheck();
        titleBar.Title = ConfigureReadAndWriteUtil.GetConfigValue("serverName");
        // 网络检测
        if (!NetworkInterface.GetIsNetworkAvailable())
        {
            throw new NetworkNotConnectedException("网络未连接");
        }
        // 判断更新
        judgmentUpdate();
        // 请求最新版本哈希列表
        requestDifferenceList();
        // 本地校验
        Console.WriteLine("test");
        Console.WriteLine("test12334 ");
        // 请求增量包

        // 覆盖安装本地

    }

    private async void judgmentUpdate()
    {
        string baseUrl = ConfigureReadAndWriteUtil.GetConfigValue("apiUrl");

        client = new RestSharpClient(baseUrl);
        try
        {
            token = await new TokenManager(client).getToken();
        }
        catch(Exception ex)
        {
            Log.Error(ex);
            exitUpdater(ex.Message);
            return;
        }

        client = new RestSharpClient(baseUrl, token);

        string localVersion = ConfigureReadAndWriteUtil.GetConfigValue("version");

        if (string.IsNullOrEmpty(localVersion))
        {
            ConfigureReadAndWriteUtil.SetConfigValue("version", "0.0.0");
            localVersion = "0.0.0";
        }

        try
        {
            var serverVersion = await client.GetAsync<VersionEntity>("/update/GetLatestVersion");

            if (new Version(localVersion) > new Version(serverVersion.data.latestVersion))
            {
                tipText.Text = "暂无更新，正在打开启动器";
                startLauncher();
                return;
            }
            tipText.Text = "检测到更新，正在获取差异文件";
        }
        catch (Exception ex)
        {
            Log.Error(ex);
            exitUpdater(ex.Message);
            return;
        }
    }

    private async void requestDifferenceList()
    {
        try
        {
            tipText.Text = "正在分析本地客户端差异";

            var versionHashList = await client.GetAsync<List<HashEntity>>("/update/GetLatestVersionHashList");
            var whitelist = await client.GetAsync<string>("/update/GetWhitelist");

            string[] differentialFilesArray = await differentialFiles(versionHashList.data, whitelist.data);

            Console.WriteLine(differentialFilesArray.Where(s => true).ToString());
            //if ( /* TODO */ )
            //{
            //    tipText.Text = "暂无更新，正在打开启动器";
            //    startLauncher();
            //    return;
            //}
            //tipText.Text = "检测到更新，正在获取差异文件";
        }
        catch (Exception ex)
        {
            Log.Error(ex);
            exitUpdater(ex.Message);
            return;
        }
    }

    private async void startLauncher()
    {
        Process.Start(new ConfigurationCheck().getCurrentDir() + ConfigureReadAndWriteUtil.GetConfigValue("launcher"));
        await Task.Delay(3000);
        Process.GetCurrentProcess().Kill();
        return;
    }

    private async void exitUpdater(string tip)
    {
        tipText.Text = tip;
        await Task.Delay(3000);
        Process.GetCurrentProcess().Kill();
        return;
    }

    private async Task<string[]> differentialFiles(List<HashEntity> laset, string whitelist)
    {
        string[] whitelistArrayBefore = whitelist.Split(Environment.NewLine.ToCharArray());
        string[] whitelistArrayAfter = whitelistArrayBefore.Where(s => !string.IsNullOrEmpty(s)).ToArray();
        string[] files = new string[] { };
        await Task.Run(() =>
        {
            // 获取本地匹配目录
            string matchingDir = new ConfigurationCheck().getCurrentDir();
            // 分析 whitelist 中的是目录还是文件
            ArrayList whitelistDir = new ArrayList();
            ArrayList whitelistFiles = new ArrayList();

            whitelistDir.AddRange(
                whitelistArrayAfter
                .Where(s => Directory.Exists(s))
                .ToArray()
            );

            whitelistFiles.AddRange(
                whitelistArrayAfter
                .Where(s => File.Exists(s))
                .ToArray()
            );

        });
        return files;
    }
}
