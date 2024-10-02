using log4net;
using McHMR_Updater_v2.core.customException;
using McHMR_Updater_v2.core.entity;
using McHMR_Updater_v2.core.utils;
using Microsoft.Win32;
using Newtonsoft.Json;
using System;
using System.Diagnostics;
using System.IO;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using Wpf.Ui.Controls;

namespace McHMR_Updater_v2;

public partial class StartWindow : FluentWindow
{
    public static readonly ILog Log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

    private int BtnStatus = 0;
    private int selectExe = 0;
    private string selectedFilePath;

    public StartWindow()
    {
        this.Height = 150;
        Wpf.Ui.Appearance.SystemThemeWatcher.Watch(this);
        InitializeComponent();
        resultMsg.Text = "如果你看到了此窗口，请联系服主";
    }



    private async void verifyBtn_Click(object sender, RoutedEventArgs e)
    {
        // 初始化
        apiInput.BorderBrush = base.BorderBrush;
        launcherInput.BorderBrush = base.BorderBrush;
        if (BtnStatus == 1)
        {
            Process.Start(ConfigurationCheck.getCurrentDir() + "\\McHMR-Updater v2.exe");
            await Task.Delay(1000);
            Process.GetCurrentProcess().Kill();

        }
        // 检测地址是否填写
        if (apiInput.Text.Equals(null) || apiInput.Text.Equals(""))
        {
            Log.Info("请填写 API 地址");
            apiInput.BorderBrush = Brushes.Red;
            resultMsg.Text = "请填写 API 地址";
            return;
        }
        // 检测启动器是否选择
        if (launcherInput.Text.Equals(null) || launcherInput.Text.Equals(""))
        {
            Log.Info("请选择启动器路径");
            launcherInput.BorderBrush = Brushes.Red;
            resultMsg.Text = "请选择启动器路径";
            return;
        }

        // 检测是否选择文件正常
        if (selectExe == 0)
        {
            Log.Info("文件不在程序目录中");
            launcherInput.BorderBrush = Brushes.Red;
            resultMsg.Text = "文件必须在程序目录中";
            return;
        }

        // 检测地址是否正常
        btnText.Visibility = Visibility.Hidden;
        btnLoadding.Visibility = Visibility.Visible;

        try
        {
            var client = new RestSharpClient(apiInput.Text, null, false);
            var apiResponse = await client.GetAsync<ApiEntity>("");

            if (apiResponse.code == 0) 
            {
                // 保存至配置
                FileStream fileStream = new FileStream(ConfigurationCheck.getConfigFile(), FileMode.Truncate);
                StreamWriter writer = new StreamWriter(fileStream, Encoding.UTF8);
                writer.Write(JsonConvert.SerializeObject(apiResponse.data));

                writer.Close();
                fileStream.Close();

                ConfigureReadAndWriteUtil.SetConfigValue("launcher", launcherInput.Text,typeof(string));

                Log.Info("配置完成");
                resultMsg.Foreground = Brushes.Green;
                resultMsg.Text = "配置完成，点击完成重新启动";
                btnText.Visibility = Visibility.Visible;
                btnLoadding.Visibility = Visibility.Hidden;
                btnText.Text = "完成";
                BtnStatus = 1;
            }
        }
        catch (Exception ex)
        {
            Log.Error("ex.Message", ex);
            resultMsg.Text = ex.Message;
            btnText.Visibility = Visibility.Visible;
            btnLoadding.Visibility = Visibility.Hidden;
        }
    }

    private void TitleBar_CloseClicked(TitleBar sender, RoutedEventArgs args)
    {
        Process.GetCurrentProcess().Kill();
    }

    private void FluentWindow_Loaded(object sender, RoutedEventArgs e)
    {
        // 网络检测
        if (!NetworkInterface.GetIsNetworkAvailable())
        {
            Log.Error("网络未连接");
            throw new NetworkNotConnectedException("网络未连接");
        }
    }

    private void selectLauncherBtn_Click(object sender, RoutedEventArgs e)
    {
        // 显示文件选择对话框
        OpenFileDialog openFileDialog = new OpenFileDialog();
        openFileDialog.Filter = "可执行文件|*.exe";
        openFileDialog.InitialDirectory = ConfigurationCheck.getCurrentDir();
        openFileDialog.Title = "选择exe可执行文件";
        openFileDialog.Multiselect = false;
        if (openFileDialog.ShowDialog() == true) // 确保用户选择了文件
        {
            selectedFilePath = openFileDialog.FileName;

            // 检查文件是否在程序目录中
            string appDir = ConfigurationCheck.getCurrentDir();

            if (!IsFileInDirectory(selectedFilePath, appDir))
            {
                // 文件不在程序目录中
                Log.Info("文件不在程序目录中");
                launcherInput.BorderBrush = Brushes.Red;
                resultMsg.Text = "文件必须在程序目录中";
                selectExe = 0;
            }
            else
            {
                // 文件在程序目录中
                selectedFilePath = selectedFilePath.Replace(appDir, "");

                launcherInput.BorderBrush = base.BorderBrush;
                launcherInput.Text = selectedFilePath;
                resultMsg.Text = "";
                selectExe = 1;
            }
        }
        else
        {
            // 用户取消了文件选择
            Log.Info("请选择启动器路径");
            launcherInput.BorderBrush = Brushes.Red;
            resultMsg.Text = "请选择启动器路径";
        }
    }

    private bool IsFileInDirectory(string filePath, string directoryPath)
    {
        // 获取文件的目录路径
        string fileDirectory = System.IO.Path.GetDirectoryName(filePath);

        // 检查文件目录是否与应用程序目录相同
        return string.Equals(fileDirectory, directoryPath, StringComparison.OrdinalIgnoreCase);
    }
}
