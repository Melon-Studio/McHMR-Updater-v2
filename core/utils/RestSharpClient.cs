using Downloader;
using log4net;
using Newtonsoft.Json;
using RestSharp;
using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;

namespace McHMR_Updater_v2.core.utils;

public class RestApiResult<T>
{
    public int code
    {
        get; set;
    }
    public string msg
    {
        get; set;
    }
    public T data
    {
        get; set;
    }

    public RestApiResult(string msg)
    {
        this.msg = msg;
    }

}

public class RestSharpClient
{

    private readonly RestClientOptions _options;
    private readonly RestClient _client;
    private readonly int timeout = 60;

    public string baseUrl
    {
        get { return _options.BaseUrl.ToString(); }
    }

    private string _token;
    private readonly Boolean _needToThrowGlobally;

    public static readonly ILog Log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

    public RestSharpClient(string baseUrl, string token = null, Boolean needToThrowGlobally = true)
    {
        _token = token;
        _needToThrowGlobally = needToThrowGlobally;

        try
        {
            _options = new RestClientOptions(baseUrl)
            {
                Timeout = TimeSpan.FromSeconds(timeout),
                ThrowOnAnyError = true
            };
            _client = new RestClient(_options);

            if (token != null)
            {
                _client.AddDefaultHeader("Authorization", "Bearer " + token);
            }

        }
        catch (Exception ex)
        {
            Log.Error(ex);
            throw ex;
        }
    }

    public async Task<RestApiResult<T>> GetAsync<T>(string url, string token = null)
    {
        if (token != null)
        {
            _client.AddDefaultHeader("Authorization", "Bearer " + token);
        }

        RestRequest request = new RestRequest(url, Method.Get);

        var response = await _client.ExecuteAsync(request);

        if (response.IsSuccessStatusCode)
        {
            try
            {
                var result = JsonConvert.DeserializeObject<RestApiResult<T>>(response.Content);
                if (result.code == 0)
                {
                    return result;
                }
                else if (result.code == 429)
                {
                    throw new Exception("当前IP重复获取Token，请10分钟后再试");
                }
                else
                {
                    if (_needToThrowGlobally)
                    {
                        Log.Info(result.msg);
                        throw new Exception(result.msg);
                    }
                    else
                    {
                        Log.Info(result.msg);
                        return new RestApiResult<T>(result.msg);
                    }
                }
            }
            catch (JsonException jsonEx)
            {
                if (_needToThrowGlobally)
                {
                    Log.Info($"JSON解析错误。{jsonEx}");
                    throw new Exception("JSON解析错误", jsonEx);
                }
                else
                {
                    Log.Info($"JSON解析错误。{jsonEx}");
                    return new RestApiResult<T>("JSON解析错误");
                }
            }
        }
        else
        {
            Log.Error($"HTTP错误: {response.StatusCode}");
            throw new WebException($"HTTP错误: {response.StatusCode}", WebExceptionStatus.ProtocolError);
        }
    }

    public async Task<RestApiResult<T>> PostAsync<T>(string url, object body, string token = null)
    {
        if (token != null)
        {
            _client.AddDefaultHeader("Authorization", "Bearer " + token);
        }

        var request = new RestRequest(url, Method.Post);
        request.AddJsonBody(body);
        var response = await _client.ExecuteAsync(request);

        if (response.IsSuccessStatusCode)
        {
            try
            {
                var result = JsonConvert.DeserializeObject<RestApiResult<T>>(response.Content);
                if (result.code == 0)
                {
                    return result;
                }
                else
                {
                    if (_needToThrowGlobally)
                    {
                        Log.Info(result.msg);
                        throw new Exception(result.msg);
                    }
                    else
                    {
                        Log.Info(result.msg);
                        return new RestApiResult<T>(result.msg);
                    }
                }
            }
            catch (JsonException jsonEx)
            {
                if (_needToThrowGlobally)
                {
                    Log.Error("JSON解析错误");
                    throw new Exception("JSON解析错误", jsonEx);
                }
                else
                {
                    Log.Error("JSON解析错误");
                    return new RestApiResult<T>("JSON解析错误");
                }
            }
        }
        else
        {
            if (_needToThrowGlobally)
            {
                Log.Error($"HTTP错误: {response.StatusCode}");
                throw new WebException($"HTTP错误: {response.StatusCode}", WebExceptionStatus.ProtocolError);
            }
            else
            {
                Log.Error($"HTTP错误: {response.StatusCode}");
                return new RestApiResult<T>($"HTTP错误: {response.StatusCode}");
            }
        }
    }

    public DownloadService GetDownloadService(int ChunkCount = 8)
    {
        WebHeaderCollection collection = new WebHeaderCollection();
        if (_token != null)
        {
            collection.Add("Authorization", "Bearer " + _token);
        }
        //collection.Add("Conetent-Type", "application/octet-stream;charset=utf-8");

        RequestConfiguration requestConfig = new RequestConfiguration
        {
            Accept = "*/*",
            Headers = collection,
            KeepAlive = true,
            ProtocolVersion = HttpVersion.Version11,
            UseDefaultCredentials = false,
            UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/125.0.0.0 Safari/537.36 Edg/125.0.0.0",
        };

        DownloadConfiguration downloadOpt = new DownloadConfiguration()
        {
            ChunkCount = ChunkCount,
            ParallelDownload = ChunkCount!=1,
            Timeout = 10000,
            RequestConfiguration = requestConfig,
        };

        return new DownloadService(downloadOpt);
    }

    public async Task DownloadIncrementalPackage(string url, string json, string path, string token = null)
    {
        if (token != null)
        {
            _client.AddDefaultHeader("Authorization", "Bearer " + token);
        }

        var request = new RestRequest(url, Method.Post);
        request.AddHeader("Content-Type", "application/json");
        request.AddStringBody(json, DataFormat.Json);

        var response = await _client.ExecuteAsync(request);

        using (var responseStream = new MemoryStream(response.RawBytes))
        {
            using (var fileStream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                await responseStream.CopyToAsync(fileStream);
            }
        }
    }
}
