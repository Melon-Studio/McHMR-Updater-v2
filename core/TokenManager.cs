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

    public TokenManager(RestSharpClient client)
    {
        _restClient = client;
    }

    public async Task<string> getToken()
    {
        string token = ConfigureReadAndWriteUtil.GetConfigValue("token");

        if (string.IsNullOrEmpty(token))
        {
            string baseUrl = ConfigureReadAndWriteUtil.GetConfigValue("apiUrl");
            if (string.IsNullOrEmpty(baseUrl))
            {
                throw new ArgumentException("API 地址无效");
            }
            tokenEntity = await asyncGetToken();

            ConfigureReadAndWriteUtil.SetConfigValue("token", tokenEntity.token);

            return tokenEntity.token;
        }
        Boolean isAvailability = await asyncCheckAvailability(token);
        if (!isAvailability)
        {
            tokenEntity = await asyncGetToken();

            ConfigureReadAndWriteUtil.SetConfigValue("token", tokenEntity.token);

            return tokenEntity.token;
        }
        return token;
    }

    private async Task<TokenEntity> asyncGetToken()
    {
        try
        {
            TokenEntity entity = new TokenEntity();

            var res = await _restClient.GetAsync<TokenEntity>("/GetToken");
            if (!string.IsNullOrEmpty(res.data.token))
            {
                ConfigureReadAndWriteUtil.SetConfigValue("token", res.data.token);
                entity.token = res.data.token;
                return entity;
            }
            return null;
        }
        catch (Exception ex)
        {
            Log.Error("asyncGetToken 获取Token失败: " + ex.Message, ex);
            throw new ApplicationException(ex.Message, ex);
        }
    }

    private async Task<Boolean> asyncCheckAvailability(string token)
    {
        try
        {
            TokenEntity entity = new TokenEntity();

            var res = await _restClient.GetAsync<VersionEntity>("/update/GetLatestVersion", token);
            if (!string.IsNullOrEmpty(res.data.latestVersion))
            {
                return true;
            }
            return false;
        }
        catch (Exception ex)
        {

            Log.Info("asyncCheckAvailability 获取Token失败，重新获取 Token", ex);
            return false;
        }
    }
}
