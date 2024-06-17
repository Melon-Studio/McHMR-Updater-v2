using System;
using System.Collections.Generic;
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
        InitializationCheck();

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
            string json = File.ReadAllText(new ConfigurationCheck().getConfigFile());

            ApiEntity entity = JsonConvert.DeserializeObject<ApiEntity>(json);
            if (entity?.apiUrl == null)
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

    private async void FluentWindow_Loaded(object sender, RoutedEventArgs e)
    {
        // 初始化
        progressBar.Visibility = Visibility.Collapsed;
        progressBarSpeed.Visibility = Visibility.Collapsed;
        tipText.Text = "正在获取最新版本";
        // 网络检测
        if (!NetworkInterface.GetIsNetworkAvailable())
        {
            throw new NetworkNotConnectedException("网络未连接");
        }
        // 客户端向服务器请求获取最新版本信息。服务器返回最新版本信息。进行更新匹配。
        string baseUrl = ConfigureReadAndWriteUtil.GetConfigValue("baseUrl");

        client = new RestSharpClient(baseUrl);

        token = await new TokenManager(client).getToken();

        client = new RestSharpClient(baseUrl, token);

        string localVersion = ConfigureReadAndWriteUtil.GetConfigValue("version");
        var serverVersion = await client.GetAsync<VersionEntity>("/GetLatestVersion");

        if (new Version(localVersion) >= new Version(serverVersion.data.latestVersion))
        {
            tipText.Text = "暂无更新，正在打开启动器";
            startLauncher();
        }
        // 客户端在本地进行版本对比，如有更新，向服务器请求获取最新版本游戏的文件哈希列表。

        // 服务器返回最新版本游戏的文件哈希列表。客户端根据文件哈希列表进行本地校验。

        // 客户端向服务器请求生成最新版本的游戏的增量包。

        // 服务器返回最新版本的游戏增量包。客户端将最新版本的游戏覆盖安装到本地。
        
    }

    private void startLauncher()
    {
        return;
    }
}
