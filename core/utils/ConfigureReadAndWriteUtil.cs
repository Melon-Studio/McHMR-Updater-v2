using log4net;
using McHMR_Updater_v2.core.entity;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Reflection;

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

    [Obsolete("该方法已经过时，将在未来版本移除，请在第三个参数传入属性值的参数类型")]
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


    public static void SetConfigValue(string configKey, string value, Type valueType)
    {
        string configPath = ConfigurationCheck.getConfigFile();

        if (!File.Exists(configPath))
        {
            Console.Error.WriteLine("配置文件未找到。");
            throw new FileNotFoundException("配置文件未找到。", configPath);
        }

        try
        {
            string configContent = File.ReadAllText(configPath);
            object configObject = JsonConvert.DeserializeObject(configContent);

            if (configObject == null)
            {
                Console.Error.WriteLine("反序列化 JSON 失败，结果为空。");
                throw new InvalidOperationException("反序列化 JSON 失败，结果为空。");
            }

            bool keyExists = false;
            if (configObject is Newtonsoft.Json.Linq.JObject jObject)
            {
                if (jObject.ContainsKey(configKey))
                {
                    keyExists = true;
                }
                else
                {
                    object instance = CreateInstance(valueType);
                    jObject.Add(configKey, instance is JToken jToken ? jToken : new JValue(instance));
                }
            }

            object convertedValue = null;
            if (valueType == typeof(int))
            {
                convertedValue = int.Parse(value);
            }
            else if (valueType == typeof(double))
            {
                convertedValue = double.Parse(value);
            }
            else if (valueType == typeof(bool))
            {
                convertedValue = bool.Parse(value);
            }
            else if (valueType == typeof(string))
            {
                convertedValue = value;
            }
            else if (valueType == typeof(long))
            {
                convertedValue = long.Parse(value);
            }

            if (!keyExists)
            {
                if (configObject is Newtonsoft.Json.Linq.JObject updatedJObject)
                {
                    updatedJObject[configKey] = convertedValue is JToken jToken ? jToken : new JValue(convertedValue);
                }
            }
            else
            {
                if (configObject is Newtonsoft.Json.Linq.JObject existingJObject)
                {
                    existingJObject[configKey] = convertedValue is JToken jToken ? jToken : new JValue(convertedValue);
                }
            }

            string updatedConfigContent = JsonConvert.SerializeObject(configObject, Formatting.Indented);
            File.WriteAllText(configPath, updatedConfigContent);
        }
        catch (JsonException jsonEx)
        {
            Console.Error.WriteLine("JSON 格式不正确。", jsonEx);
            throw new InvalidOperationException("JSON 格式不正确。", jsonEx);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine("配置文件未找到。", ex);
            throw new InvalidOperationException("读取配置时发生错误。", ex);
        }
    }

    private static object CreateInstance(Type valueType)
    {
        if (valueType == typeof(int))
            return 0;
        else if (valueType == typeof(double))
            return 0.0;
        else if (valueType == typeof(bool))
            return false;
        else if (valueType == typeof(string))
            return "";
        else if (valueType == typeof(long))
            return 0L;
        else
            throw new NotImplementedException($"不支持的类型：{valueType.Name}");
    }
}
