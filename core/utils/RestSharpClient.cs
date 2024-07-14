using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using RestSharp;
using RestSharp.Authenticators;
using Downloader;
using log4net;

namespace McHMR_Updater_v2.core.utils;

public class RestApiResult<T>
{
    public int code { get; set; }
    public string msg { get; set; }
    public T data { get; set; }

    public RestApiResult(string msg) {
        this.msg = msg;
    }

}

public class RestSharpClient
{

    private readonly RestClientOptions _options;
    private readonly RestClient _client;
    private readonly int timeout = 30;

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

        }catch (Exception ex)
        {
            Log.Error(ex);
            throw ex;
        }
    }

    public async Task<RestApiResult<T>> GetAsync<T>(string url, string token=null)
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

    public DownloadService DownloadFileAsync()
    {
        WebHeaderCollection collection = new WebHeaderCollection();
        if (_token != null)
        {
            collection.Add("Authorization", "Bearer " + _token);
        }
        collection.Add("Accept", "application/octet-stream");

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
            ChunkCount = 8,
            ParallelDownload = true,
            Timeout = 10000,
            RequestConfiguration = requestConfig
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
