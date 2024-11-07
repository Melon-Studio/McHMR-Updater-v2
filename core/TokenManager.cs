using log4net;
using McHMR_Updater_v2.core.entity;
using McHMR_Updater_v2.core.utils;
using System;
using System.Threading.Tasks;

namespace McHMR_Updater_v2.core;
public class TokenManager
{
    public static readonly ILog Log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);


    private readonly RestSharpClient _restClient;
    private TokenEntity tokenEntity;

    public TokenManager()
    {
        _restClient = new RestSharpClient(ConfigureReadAndWriteUtil.GetConfigValue("apiUrl"));
    }

    public async Task setToken()
    {
        // 判断当前是否存在 Token，如果不存在则获取，存在则验证
        string token = ConfigureReadAndWriteUtil.GetConfigValue("token");

        if (string.IsNullOrEmpty(token))
        {
            // NULL
            string baseUrl = ConfigureReadAndWriteUtil.GetConfigValue("apiUrl");
            if (string.IsNullOrEmpty(baseUrl))
            {
                throw new ArgumentException("API 地址无效");
            }
            tokenEntity = await asyncGetToken();

            ConfigureReadAndWriteUtil.SetConfigValue("token", tokenEntity.token, typeof(string));
            return;
        }
        // NO NULL
        string timeout = ConfigureReadAndWriteUtil.GetConfigValue("timeout");
        if (string.IsNullOrEmpty(timeout) || IsNowAfterTimestamp((long)double.Parse(timeout)))
        {
            tokenEntity = await asyncGetToken();

            ConfigureReadAndWriteUtil.SetConfigValue("token", tokenEntity.token, typeof(string));

            return;
        }
    }

    private async Task<TokenEntity> asyncGetToken()
    {
        TokenEntity entity = new TokenEntity();
        try
        {
            var res = await _restClient.GetAsync<TokenEntity>("/GetToken");
            setTimeout();
            if (!string.IsNullOrEmpty(res.data.token))
            {
                ConfigureReadAndWriteUtil.SetConfigValue("token", res.data.token, typeof(string));
                entity.token = res.data.token;
                return entity;
            }
            return null;
        }
        catch (Exception ex)
        {
            string token = ConfigureReadAndWriteUtil.GetConfigValue("token");
            RestSharpClient client = new RestSharpClient(ConfigureReadAndWriteUtil.GetConfigValue("apiUrl"), token);
            RestApiResult<VersionEntity> res = await client.GetAsync<VersionEntity>("/update/GetLatestVersion");
            if (string.IsNullOrEmpty(res.data.latestVersion))
            {
                Log.Error("asyncGetToken 获取Token失败: " + ex.Message, ex);
                throw new ApplicationException(ex.Message, ex);
            }
            entity.token = token;
            return entity;
        }
    }

    // 设置超时时间
    private void setTimeout()
    {
        DateTime currentTime = DateTime.Now;
        DateTime tenMinutesLater = currentTime.AddMinutes(10);
        TimeSpan timeDiff = tenMinutesLater - new DateTime(1970, 1, 1);
        ConfigureReadAndWriteUtil.SetConfigValue("timeout", timeDiff.TotalSeconds.ToString(), typeof(string));
    }

    private bool IsNowAfterTimestamp(long timestamp)
    {
        DateTime currentTime = DateTime.Now;
        DateTime targetTime = new DateTime(1970, 1, 1).AddSeconds(timestamp);
        return currentTime > targetTime;
    }
}
