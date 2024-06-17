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
using System.Windows.Shapes;
using log4net;
using McHMR_Updater_v2.core.customException;
using McHMR_Updater_v2.core.entity;
using McHMR_Updater_v2.core.utils;
using Newtonsoft.Json;
using Wpf.Ui.Controls;

namespace McHMR_Updater_v2;

public partial class StartWindow : FluentWindow
{
    public static readonly ILog Log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

    private int BtnStatus = 0;

    public StartWindow()
    {
        this.Height = 150;
        InitializeComponent();

        Loaded += (sender, args) =>
        {
            Wpf.Ui.Appearance.SystemThemeWatcher.Watch(this);
        };
    }



    private async void verifyBtn_Click(object sender, RoutedEventArgs e)
    {
        // 初始化
        resultMsg.Text = "";
        apiInput.BorderBrush = base.BorderBrush;
        if (BtnStatus == 1) 
        {
            this.Close();
        }
        // 检测地址是否填写
        if (apiInput.Text.Equals(null) || apiInput.Text.Equals(""))
        {
            Log.Info("请填写 API 地址");
            apiInput.BorderBrush = Brushes.Red;
            resultMsg.Text = "请填写 API 地址";
            return;
        }
        // 检测地址是否正常
        btnText.Visibility = Visibility.Hidden;
        btnLoadding.Visibility = Visibility.Visible;

        try
        {
            var client = new RestSharpClient(apiInput.Text, null, false);

            var apiResponse = await client.GetAsync<ApiEntity>("");

            if (getTaskContent(apiResponse, "data") == "null")
            {
                resultMsg.Text = getTaskContent(apiResponse, "msg").TrimStart('\"').TrimEnd('\"');
                btnText.Visibility = Visibility.Visible;
                btnLoadding.Visibility = Visibility.Hidden;
                Log.Info(getTaskContent(apiResponse, "msg").TrimStart('\"').TrimEnd('\"'));
                return;
            }
            else
            {
                btnText.Visibility = Visibility.Visible;
                btnLoadding.Visibility = Visibility.Hidden;

                // 保存至配置
                FileStream fileStream = new FileStream(new ConfigurationCheck().getConfigFile(), FileMode.Truncate);
                StreamWriter writer = new StreamWriter(fileStream, Encoding.UTF8);
                writer.Write(getTaskContent(apiResponse, "data"));

                writer.Close();
                fileStream.Close();

                Log.Info("配置完成");
                resultMsg.Foreground = Brushes.Green;
                resultMsg.Text = "配置完成";
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

    private string getTaskContent(RestApiResult<ApiEntity> restApiResult, string Attributes)
    {
        switch (Attributes)
        {
            case("code"):
                return JsonConvert.SerializeObject(restApiResult.code);
            case ("msg"):
                return JsonConvert.SerializeObject(restApiResult.msg);
            case ("data"):
                return JsonConvert.SerializeObject(restApiResult.data);
            default:
                return null;
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
}
