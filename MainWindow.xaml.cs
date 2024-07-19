using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using log4net;
using McHMR_Updater_v2.core;
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


    private async void FluentWindow_ContentRendered(object sender, EventArgs e)
    {
        // 初始化
        progressBar.Visibility = Visibility.Collapsed;
        progressBarSpeed.Visibility = Visibility.Collapsed;
        tipText.Text = "正在获取最新版本";
        InitializationCheck();
        titleBar.Title = ConfigureReadAndWriteUtil.GetConfigValue("serverName");
        // 网络检测
        if (!IsConnectionAvailable())
        {
            Log.Error("网络未连接");
            await exitUpdater("网络未连接，程序即将退出");
        }
        // 服务器连接测试
        try
        {
            using (HttpClient client = new HttpClient())
            {
                HttpResponseMessage response = await client.GetAsync(ConfigureReadAndWriteUtil.GetConfigValue("apiUrl"));
                if (!response.IsSuccessStatusCode)
                {
                    Log.Error("无法连接至服务器");
                    await exitUpdater("无法连接至服务器，请联系服主");
                }
            }
        }
        catch (Exception ex)
        {
            Log.Error("无法连接至服务器: " + ex.Message);
            await exitUpdater("无法连接至服务器，请联系服主");
        }
        // 判断更新
        await judgmentUpdate();
        // 请求最新版本哈希列表
        requestDifferenceList();
        // 本地校验

        List<string> inconsistentFile = await differentialFiles(hashLits.hashList, hashLits.whiteList);
        //删除服务器不存在的文件
        NoFileUtil noFile = new NoFileUtil();
        List<string> noFileList = await noFile.CheckFiles(hashLits.hashList, hashLits.whiteList, gamePath);
        foreach (string file in noFileList)
        {
            File.Delete(file);
        }
        // 请求增量包
        var jsonBodyObject = new { fileList = inconsistentFile };
        string jsonBody = JsonConvert.SerializeObject(jsonBodyObject);
        await client.DownloadIncrementalPackage("/update/GenerateIncrementalPackage", jsonBody, gamePath + "\\inconsistentFile");
        // 覆盖安装本地
        using (ZipFile zip = ZipFile.Read(gamePath + "\\inconsistentFile"))
        {
            foreach (ZipEntry entry in zip)
            {
                entry.Extract(gamePath, ExtractExistingFileAction.OverwriteSilently);
            }
        }
    }

    private async Task judgmentUpdate()
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
            await exitUpdater(ex.Message);
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
                await startLauncher();
                return;
            }
            tipText.Text = "检测到更新，正在获取差异文件";
        }
        catch (Exception ex)
        {
            Log.Error(ex);
            await exitUpdater(ex.Message);
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

            ListEntity listEntity = new ListEntity();
            listEntity.hashList = versionHashList.data;
            listEntity.whiteList = whitelist.data;
            return listEntity;
        }
        catch (Exception ex)
        {
            Log.Error(ex);
            await exitUpdater(ex.Message);
            return null;
        }
    }

    private async Task startLauncher()
    {
        Process.Start(new ConfigurationCheck().getCurrentDir() + ConfigureReadAndWriteUtil.GetConfigValue("launcher"));
        await Task.Delay(3000);
        Process.GetCurrentProcess().Kill();
        return;
    }

    private async Task exitUpdater(string tip)
    {
        tipText.Text = tip;
        await Task.Delay(3000);
        Process.GetCurrentProcess().Kill();
    }

    [DllImport("wininet.dll", SetLastError = true)]
    private static extern bool InternetGetConnectedState(out int description, int reservedValue);

    public static bool IsConnectionAvailable()
    {
        int description;
        return InternetGetConnectedState(out description, 0);
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
