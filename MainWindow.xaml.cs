using Downloader;
using Ionic.Zip;
using log4net;
using McHMR_Updater_v2.core;
using McHMR_Updater_v2.core.entity;
using McHMR_Updater_v2.core.utils;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using Wpf.Ui.Controls;

namespace McHMR_Updater_v2;

public partial class MainWindow : FluentWindow
{
    public static readonly ILog Log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

    private string token;
    private RestSharpClient client;
    private readonly string gamePath = ConfigurationCheck.getCurrentDir() + "\\.minecraft";
    private string inconsistentPath;
    private string version;

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
        ConfigurationCheck.check();
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
        if (await judgmentUpdate()) return;
        // 请求最新版本哈希列表
        ListEntity hashList = await requestDifferenceList();
        // 本地校验
        List<string> inconsistentFile = await differentialFiles(hashList.hashList, hashList.whiteList);
        //删除服务器不存在的文件
        NoFileUtil noFile = new NoFileUtil();
        List<string> noFileList = await noFile.CheckFiles(hashList.hashList, hashList.whiteList, gamePath);
        if (noFileList.Count > 0)
        {
            tipText.Text = "正在删除服务端不存在的文件";
            foreach (string file in noFileList)
            {
                File.Delete(file);
            }
        }
        await noFile.RemoveEmptyDirectories(gamePath);
        // 请求增量包
        if (inconsistentFile.Count > 0)
        {
            await requestIncrementalPackage(inconsistentFile);
        }
        else
        {
            tipText.Text = "暂未发现需要更新的文件，正在为您打开启动器";
            //更新配置文件版本号
            await updateVersion();
            await startLauncher();
        }
        // 覆盖安装
        //await install(inconsistentFilePath);
    }

    private async void onDownloadFileCompleted(object sender, AsyncCompletedEventArgs e)
    {
        await progressBar.Dispatcher.Invoke(async () =>
        {
            Action method = new Action(async delegate
                        {
                            if (e.Error != null)
                            {
                                Log.Error(e.Error);
                                await exitUpdater(e.Error.Message);
                                return;
                            }

                            progressBar.Visibility = Visibility.Collapsed;
                            progressBarSpeed.Visibility = Visibility.Collapsed;

                            tipText.Text = "正在安装新版本，请稍后";

                            await install();
                        });
            await Dispatcher.BeginInvoke(method);
        });
    }
    private void onDownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
    {
        progressBar.Dispatcher.Invoke(() =>
        {
            progressBar.Value = e.ProgressPercentage;
            double speedInBps = e.AverageBytesPerSecondSpeed;

            double speedInKbps = speedInBps / 1024;

            string speedDisplay;
            if (speedInKbps >= 1000)
            {
                double speedInMbps = speedInKbps / 1024;
                speedDisplay = $"{speedInMbps:F2} MB/s";
            }
            else
            {
                speedDisplay = $"{speedInKbps:F2} KB/s";
            }

            progressBarSpeed.Text = speedDisplay;
        });
    }
    private void OnDownloadStarted(object sender, DownloadStartedEventArgs e)
    {
        progressBar.Dispatcher.Invoke(() =>
        {
            progressBar.Visibility = Visibility.Visible;
            progressBarSpeed.Visibility = Visibility.Visible;
        });
    }
    private async Task<Boolean> judgmentUpdate()
    {
        string baseUrl = ConfigureReadAndWriteUtil.GetConfigValue("apiUrl");

        client = new RestSharpClient(baseUrl);
        try
        {
            token = await new TokenManager(client).getToken();
        }
        catch (Exception ex)
        {
            Log.Error(ex);
            await exitUpdater(ex.Message);
            return true;
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
            version = serverVersion.data.latestVersion;
            if (new Version(localVersion) >= new Version(version))
            {
                tipText.Text = "暂无更新，正在打开启动器";
                await startLauncher();
                return true;
            }
            tipText.Text = "检测到更新，正在获取差异文件";
            return false;
        }
        catch (Exception ex)
        {
            Log.Error(ex);
            await exitUpdater(ex.Message);
            return true;
        }
    }

    private async Task<ListEntity> requestDifferenceList()
    {
        try
        {
            tipText.Text = "正在请求服务端文件信息";
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
        Process.Start(ConfigurationCheck.getCurrentDir() + ConfigureReadAndWriteUtil.GetConfigValue("launcher"));
        await Task.Delay(3000);
        Process.GetCurrentProcess().Kill();
        return;
    }

    private async Task exitUpdater(string tip)
    {
        Log.Info("Exit: " + tip);
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

    private async Task<List<string>> differentialFiles(List<HashEntity> laset, string whitelist)
    {
        tipText.Text = "正在分析本地客户端差异";
        string[] whitelistArrayBefore = whitelist.Split(Environment.NewLine.ToCharArray());
        string[] whitelistArrayAfter = whitelistArrayBefore.Where(s => !string.IsNullOrEmpty(s)).ToArray();
        List<string> files = new List<string>();

        await Task.Run(() =>
        {
            // 遍历 laset 列表，检查文件哈希值是否一致
            foreach (HashEntity hashEntity in laset)
            {
                string absoluteFilePath = gamePath + hashEntity.filePath;
                absoluteFilePath = absoluteFilePath.Replace('/', '\\');

                // 尝试计算文件当前哈希值
                FileHashUtil fileHash = new FileHashUtil();
                string currentFileHash;

                try
                {
                    // 计算文件哈希值
                    currentFileHash = fileHash.CalculateHash(absoluteFilePath);
                }
                catch (IOException)
                {
                    // 如果文件被占用，复制一份然后计算哈希值
                    string tempFilePath = absoluteFilePath + ".tmp";
                    File.Copy(absoluteFilePath, tempFilePath, true);
                    currentFileHash = fileHash.CalculateHash(tempFilePath);
                    File.Delete(tempFilePath);
                }

                // 如果哈希值不一致，将文件路径添加到 files 列表中
                if (currentFileHash != hashEntity.fileHash)
                {
                    files.Add(hashEntity.filePath);
                }
            }
        });
        return files;
    }

    private async Task requestIncrementalPackage(List<string> inconsistentFile)
    {
        tipText.Text = "正在等待服务器响应";
        var jsonBodyObject = new { fileList = inconsistentFile };
        string jsonBody = JsonConvert.SerializeObject(jsonBodyObject);

        var packageHash = await client.PostAsync<PackageEntity>("/update/GenerateIncrementalPackage", jsonBody);

        try
        {
            var downloader = client.GetDownloadService();

            inconsistentPath = ConfigurationCheck.getTempDir() + "\\" + packageHash.data.packageHash + ".zip";

            if (!File.Exists(inconsistentPath))
            {
                File.Create(inconsistentPath).Dispose();
            }

            downloader.DownloadStarted += OnDownloadStarted;
            downloader.DownloadProgressChanged += onDownloadProgressChanged;
            downloader.DownloadFileCompleted += onDownloadFileCompleted;

            tipText.Text = "正在下载最新版本";

            await downloader.DownloadFileTaskAsync(client.baseUrl + "/update/download" + "?fileHash=" + packageHash.data.packageHash, inconsistentPath);
        }
        catch (Exception ex)
        {
            Log.Error(ex);
            await exitUpdater(ex.Message);
        }
    }

    private async Task install()
    {
        await Task.Run(() =>
        {
            using (ZipFile zip = ZipFile.Read(inconsistentPath))
            {
                foreach (ZipEntry entry in zip)
                {
                    entry.Extract(gamePath, ExtractExistingFileAction.OverwriteSilently);
                }
            }
        });

        File.Delete(inconsistentPath);
        //更新配置文件版本号
        await updateVersion();
        // 启动游戏
        tipText.Text = "安装完成，正在打开启动器";
        await startLauncher();
    }

    private async Task updateVersion()
    {
        ConfigureReadAndWriteUtil.SetConfigValue("version", version);
    }
}
