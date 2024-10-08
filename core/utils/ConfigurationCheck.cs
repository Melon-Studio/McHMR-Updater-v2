﻿using log4net;
using System.IO;

namespace McHMR_Updater_v2.core.utils;
public class ConfigurationCheck
{
    public static readonly ILog Log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

    public static void check()
    {
        string currentDirectory = Directory.GetCurrentDirectory();
        string configDirectory = currentDirectory + "\\mchmr";
        string tempDirectory = currentDirectory + "\\mchmr\\temp";
        string apiConfigFile = configDirectory + "\\config.json";
        string gameDirectory = currentDirectory + "\\.minecraft";
        string backgroundDirectory = currentDirectory + "\\mchmr\\background";

        if (!Directory.Exists(configDirectory))
        {
            Directory.CreateDirectory(configDirectory);
        }

        if (!Directory.Exists(tempDirectory))
        {
            Directory.CreateDirectory(tempDirectory);
        }

        if (!File.Exists(apiConfigFile))
        {
            File.Create(apiConfigFile);
        }

        if (!File.Exists(gameDirectory))
        {
            Directory.CreateDirectory(gameDirectory);
        }

        if (!Directory.Exists(backgroundDirectory))
        {
            Directory.CreateDirectory(backgroundDirectory);
        }
    }

    public static string getCurrentDir()
    {
        return Directory.GetCurrentDirectory();
    }

    public static string getConfigDir()
    {
        return getCurrentDir() + "\\mchmr";
    }

    public static string getConfigFile()
    {
        return getConfigDir() + "\\config.json";
    }

    public static string getTempDir()
    {
        return getConfigDir() + "\\temp";
    }

    public static string getGameDir()
    {
        return getCurrentDir() + "\\.minecraft";
    }

    public static string getBackgroundDir()
    {
        return getCurrentDir() + "\\mchmr\\background";
    }

}
