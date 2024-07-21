using System;
using System.IO;
using log4net;
using McHMR_Updater_v2.core.entity;
using Newtonsoft.Json;

namespace McHMR_Updater_v2.core.utils;
public class ConfigureReadAndWriteUtil
{
    public static readonly ILog Log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

    public static string GetConfigValue(string configKey)
    {
        string configPath = ConfigurationCheck.getConfigFile();

        if (!File.Exists(configPath))
        {
            Log.Error("配置文件未找到。");
            throw new FileNotFoundException("配置文件未找到。", configPath);
        }

        try
        {
            string configContent = File.ReadAllText(configPath);
            ApiEntity apiEntity = JsonConvert.DeserializeObject<ApiEntity>(configContent);

            if (apiEntity == null)
            {
                Log.Error("反序列化JSON失败，结果为空。");
                StartWindow startWindow = new StartWindow();
                startWindow.ShowDialog();
            }

            var propertyInfo = apiEntity.GetType().GetProperty(configKey);
            if (propertyInfo == null)
            {
                Log.Error($"在{apiEntity.GetType().Name}中找不到属性 '{configKey}'");
                throw new ArgumentException($"在{apiEntity.GetType().Name}中找不到属性 '{configKey}'");
            }

            object value = propertyInfo.GetValue(apiEntity);
            // 根据属性类型进行转换
            if (propertyInfo.PropertyType.IsPrimitive || propertyInfo.PropertyType == typeof(string) || propertyInfo.PropertyType == typeof(decimal))
            {
                if (value == null)
                {
                    return null;
                }
                return value.ToString();
            }
            else
            {
                // 非原始类型使用序列化对象
                if (value == null)
                {
                    return null;
                }
                return JsonConvert.SerializeObject(value);
            }
        }
        catch (JsonException jsonEx)
        {
            Log.Error("JSON格式不正确。", jsonEx);
            throw new InvalidOperationException("JSON格式不正确。", jsonEx);
        }
        catch (Exception ex)
        {
            Log.Error("配置文件未找到。", ex);
            throw new InvalidOperationException("读取配置时发生错误。", ex);
        }
    }

    public static void SetConfigValue(string configKey, string value)
    {
        string configPath = ConfigurationCheck.getConfigFile();

        if (!File.Exists(configPath))
        {
            Log.Error("配置文件未找到。");
            throw new FileNotFoundException("配置文件未找到。", configPath);
        }

        try
        {
            string configContent = File.ReadAllText(configPath);
            ApiEntity apiEntity = JsonConvert.DeserializeObject<ApiEntity>(configContent);

            if (apiEntity == null)
            {
                Log.Error("反序列化JSON失败，结果为空。");
                throw new InvalidOperationException("反序列化JSON失败，结果为空。");
            }

            var propertyInfo = apiEntity.GetType().GetProperty(configKey);
            if (propertyInfo == null)
            {
                Log.Error($"在{apiEntity.GetType().Name}中找不到属性 '{configKey}'");
                throw new ArgumentException($"在{apiEntity.GetType().Name}中找不到属性 '{configKey}'");
            }

            propertyInfo.SetValue(apiEntity, value, null);
            string updatedConfigContent = JsonConvert.SerializeObject(apiEntity, Formatting.Indented);
            File.WriteAllText(configPath, updatedConfigContent);
        }
        catch (JsonException jsonEx)
        {
            Log.Error("JSON格式不正确。", jsonEx);
            throw new InvalidOperationException("JSON格式不正确。", jsonEx);
        }
        catch (Exception ex)
        {
            Log.Error("配置文件未找到。", ex);
            throw new InvalidOperationException("读取配置时发生错误。", ex);
        }
    }
}
