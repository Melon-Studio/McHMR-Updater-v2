using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using log4net;

namespace McHMR_Updater_v2.core.utils;
public class ConfigurationCheck
{
    public static readonly ILog Log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

    public void check()
    {
        string currentDirectory = Directory.GetCurrentDirectory();
        string configDirectory = currentDirectory + "\\mchmr";
        string apiConfigFile = configDirectory + "\\config.json";

        if (!Directory.Exists(configDirectory))
        {
            Directory.CreateDirectory(configDirectory);
        }

        if (!File.Exists(apiConfigFile))
        {
            File.Create(apiConfigFile);
        }
    }

    public string getCurrentDir()
    {
        return Directory.GetCurrentDirectory();
    }

    public string getConfigDir()
    {
        string currentDirectory = Directory.GetCurrentDirectory();
        return currentDirectory + "\\mchmr";
    }

    public string getConfigFile()
    {
        string currentDirectory = Directory.GetCurrentDirectory();
        string configDirectory = currentDirectory + "\\mchmr";
        return configDirectory + "\\config.json";
    }
}
