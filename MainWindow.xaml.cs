using Downloader;
using Ionic.Zip;
using log4net;
using McHMR_Updater_v2.core;
using McHMR_Updater_v2.core.convert;
using McHMR_Updater_v2.core.entity;
using McHMR_Updater_v2.core.utils;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Security.Policy;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Wpf.Ui.Controls;

namespace McHMR_Updater_v2;

public partial class MainWindow : FluentWindow
{
    public static readonly ILog Log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

    private string token;
    private RestSharpClient client;
    private readonly string gamePath = ConfigurationCheck.getGameDir();
    private string inconsistentPath;
    private string version;
    private string[] imageExtensions = { ".jpg", ".jpeg" };
    private RestApiResult<BackgroundEntity> _bgResp;
    private RestSharpClient noTokenClient;

    public MainWindow()
    {
        Wpf.Ui.Appearance.SystemThemeWatcher.Watch(this);
        InitializeComponent();
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

    private async void FluentWindow_Loaded(object sender, RoutedEventArgs e)
    {
        // 初始化
        InitializationCheck();
        // Token 更新
        try
        {
            await new TokenManager().setToken();
            
            client = new RestSharpClient(ConfigureReadAndWriteUtil.GetConfigValue("apiUrl"), ConfigureReadAndWriteUtil.GetConfigValue("token"));
            noTokenClient = new RestSharpClient(ConfigureReadAndWriteUtil.GetConfigValue("apiUrl"));
            
        }
        catch (Exception ex) 
        {
            Log.Error(ex);
        }
        // 服务器名更新
        var apiResp = await noTokenClient.GetAsync<ApiEntity>("/server/GetServerAPI");
        if (apiResp.code == 0)
        {
            ConfigureReadAndWriteUtil.SetConfigValue("serverName", apiResp.data.serverName, typeof(string));
        }
        // 背景图片
        SetBackgroundAsync();
    }

    private async void FluentWindow_ContentRendered(object sender, EventArgs e)
    {
        progressMain.Visibility = Visibility.Hidden;

        await Task.Run(async () =>
        {
            await Task.Delay(1000);
        });
        tipText.Text = "正在获取最新版本";
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
            tipDescription.Text = "";
        }
        else
        {
            tipText.Text = "更新完成，正在为您打开启动器";
            //更新配置文件版本号
            await updateVersion();
            await startLauncher();
        }
    }

    private static void ClearFolder(string folderPath)
    {
        try
        {
            if (Directory.Exists(folderPath))
            {
                string[] files = Directory.GetFiles(folderPath);
                string[] directories = Directory.GetDirectories(folderPath);

                foreach (string file in files)
                {
                    File.Delete(file);
                }

                foreach (string directory in directories)
                {
                    ClearFolder(directory);
                    Directory.Delete(directory);
                }
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex);
        }
    }

    private async Task SetBackgroundAsync()
    {
        _bgResp = await client.GetAsync<BackgroundEntity>("/launcher/GetLauncherBackground");
        ConfigureReadAndWriteUtil.SetConfigValue("hasBackground", _bgResp.data.hasBackground.ToString(), typeof(int));

        if (ConfigureReadAndWriteUtil.GetConfigValue("hasBackground") == "1" || ConfigureReadAndWriteUtil.GetConfigValue("hasBackground") == null)
        {
            ImageBrush brush = null;
            if (_bgResp.code == 0)
            {
                if (_bgResp.data.backgroundHash == null && ConfigureReadAndWriteUtil.GetConfigValue("backgroundHash") != null)
                {
                    ConfigureReadAndWriteUtil.SetConfigValue("backgroundHash", null, typeof(string));
                }

                if (_bgResp.data.backgroundHash != null && _bgResp.data.backgroundHash != ConfigureReadAndWriteUtil.GetConfigValue("backgroundHash"))
                {
                    string temeurl = ConfigureReadAndWriteUtil.GetConfigValue("apiUrl");
                    string url = temeurl.Replace("/v1", "");
                    ConfigureReadAndWriteUtil.SetConfigValue("backgroundUrl", _bgResp.data.backgroundUrl, typeof(string));
                    ClearFolder(ConfigurationCheck.getBackgroundDir());
                    //var downloader = noTokenClient.GetDownloadService();
                    //downloader.DownloadFileCompleted += onDownloadBackgroundFileCompleted;
                    string phat = ConfigurationCheck.getBackgroundDir() + "\\" + _bgResp.data.backgroundUrl.Replace("/images/background/", "");
                    //await downloader.DownloadFileTaskAsync(url + _bgResp.data.backgroundUrl, phat);

                    WebClient downloader = new WebClient();
                    downloader.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/125.0.0.0 Safari/537.36 Edg/125.0.0.0");
                    downloader.DownloadFile(url + _bgResp.data.backgroundUrl, phat);
                    ConfigureReadAndWriteUtil.SetConfigValue("backgroundHash", _bgResp.data.backgroundHash, typeof(string));

                }
            }
            if (FindFirstImageInFolder(ConfigurationCheck.getBackgroundDir(), imageExtensions) == null)
            {
                Bitmap bitmap = Properties.Resources.DefaultBackground;
                using (MemoryStream memory = new MemoryStream())
                {
                    bitmap.Save(memory, System.Drawing.Imaging.ImageFormat.Bmp);
                    memory.Position = 0;
                    BitmapImage bitmapImage = new BitmapImage();
                    bitmapImage.BeginInit();
                    bitmapImage.StreamSource = memory;
                    bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                    bitmapImage.EndInit();
                    brush = new ImageBrush(bitmapImage);
                    brush.Stretch = Stretch.UniformToFill;
                }
            }
            background.Background = brush;
            string firstImagePath = FindFirstImageInFolder(ConfigurationCheck.getBackgroundDir(), imageExtensions);
            if (!string.IsNullOrEmpty(firstImagePath))
            {
                BitmapImage bitmapImage = new BitmapImage(new Uri(firstImagePath));
                background.Background = new ImageBrush(bitmapImage);
            }
        }
        else
        {
            title.Text = ConfigureReadAndWriteUtil.GetConfigValue("serverName");
        }
    }

    private string FindFirstImageInFolder(string folderPath, string[] extensions)
    {
        try
        {
            string[] files = Directory.GetFiles(folderPath);
            for (int i = 0; i < files.Length; i++)
            {
                string extension = Path.GetExtension(files[i]).ToLower();
                if (Array.Exists(extensions, ext => ext == extension))
                {
                    return files[i];
                }
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex);
        }
        return null;
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

                            progressMain.Visibility = Visibility.Hidden;

                            tipText.Text = "正在安装新版本，请稍后";

                            await install();
                        });
            await Dispatcher.BeginInvoke(method);
        });
    }
    private void onDownloadProgressChanged(object sender, Downloader.DownloadProgressChangedEventArgs e)
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
            progressMain.Visibility = Visibility.Visible;
        });
    }

    private async void onDownloadBackgroundFileCompleted(object sender, AsyncCompletedEventArgs e)
    {
        await progressBar.Dispatcher.Invoke(async () =>
        {
            ConfigureReadAndWriteUtil.SetConfigValue("backgroundHash", _bgResp.data.backgroundHash, typeof(string));
        });
    }

    private async Task<Boolean> judgmentUpdate()
    {
        string baseUrl = ConfigureReadAndWriteUtil.GetConfigValue("apiUrl");

        string localVersion = ConfigureReadAndWriteUtil.GetConfigValue("version");

        if (string.IsNullOrEmpty(localVersion))
        {
            ConfigureReadAndWriteUtil.SetConfigValue("version", "0.0.0", typeof(string));
            localVersion = "0.0.0";
        }

        try
        {
            var serverVersion = await client.GetAsync<VersionEntity>("/update/GetLatestVersion");
            version = serverVersion.data.latestVersion;
            Console.WriteLine(serverVersion.data.latestVersion);
            Console.WriteLine(serverVersion.data.description);
            if (new Version(localVersion) >= new Version(version))
            {
                tipText.Text = "暂无更新，正在打开启动器";
                await startLauncher();
                return true;
            }
            tipText.Text = "发现新版本: " + serverVersion.data.latestVersion;
            tipDescription.Text = serverVersion.data.description;
            await Task.Run(async () =>
            {
                await Task.Delay(2000);
            });

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
        try
        {
            Process.Start(ConfigurationCheck.getCurrentDir() + ConfigureReadAndWriteUtil.GetConfigValue("launcher"));
        }
        catch
        {
            await exitUpdater("启动器配置异常，请联系服主");
            return;
        }

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
        await Task.Run(() =>
        {
            ConfigureReadAndWriteUtil.SetConfigValue("version", version, typeof(string));
        });
    }
}
