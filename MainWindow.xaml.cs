using System;
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

        // 请求增量包

        // 覆盖安装本地

    }

    private async void judgmentUpdate()
    {
        string baseUrl = ConfigureReadAndWriteUtil.GetConfigValue("apiUrl");

        client = new RestSharpClient(baseUrl);

        token = await new TokenManager(client).getToken();

        client = new RestSharpClient(baseUrl, token);

        string localVersion = ConfigureReadAndWriteUtil.GetConfigValue("version");

        if (string.IsNullOrEmpty(localVersion))
        {
            ConfigureReadAndWriteUtil.SetConfigValue("version", "0.0.0");
            localVersion = "0.0.0";
        }

        var serverVersion = await client.GetAsync<VersionEntity>("/update/GetLatestVersion");

        if (new Version(localVersion) <= new Version(serverVersion.data.latestVersion))
        {
            tipText.Text = "暂无更新，正在打开启动器";
            startLauncher();
            return;
        }
        tipText.Text = "检测到更新，正在获取差异文件";
    }

    private async void requestDifferenceList()
    {
    
    }

    private async void startLauncher()
    {
        Process.Start(new ConfigurationCheck().getCurrentDir() + ConfigureReadAndWriteUtil.GetConfigValue("launcher"));
        await Task.Delay(3000);
        Process.GetCurrentProcess().Kill();
        return;
    }


}
