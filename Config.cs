using System;
using System.Collections.Generic;

namespace dotnet_irc_testing
{
    public static class Config
    {
        public static Dictionary<string, object> roamingSettings = new Dictionary<string, object>();
        
        // behaviour settings
        public const string UserListClick = "userlistclick";
        public const string SwitchOnJoin = "switchonjoin";
        public const string UseTabs = "usetabs";

        // connection settings
        public const string DefaultUsername = "defaultusername";
        public const string AutoReconnect = "autoreconnect";
        public const string IgnoreSSL = "ignoressl";

        // display settings
        public const string DarkTheme = "darktheme";
        public const string FontFamily = "fontfamily";
        public const string FontSize = "fontsize";
        public const string ReducedPadding = "reducedpadding";
        public const string HideStatusBar = "hidestatusbar";
        public const string IgnoreJoinLeave = "ignorejoinleave";

        // handles for server storage
        public static string ServersStore = "serversstore";
        public static string ServersListStore = "serversliststore";

        public const string FirstRun = "firstrun";

        public static bool Contains(string key)
        {
            return roamingSettings.ContainsKey(key);
        }

        public static void SetString(string key, string value)
        {
            roamingSettings[key] = value;
        }

        public static void SetInt(string key, int value)
        {
            roamingSettings[key] = value;
        }

        public static void SetBoolean(string key, bool value)
        {
            roamingSettings[key] = value;
        }

        public static bool GetBoolean(string key, bool def = false)
        {
            if (Contains(key) && roamingSettings[key] is bool)
            {
                return (bool) roamingSettings[key];
            }
            else
            {
                return def;
            }
        }

        public static string GetString(string key, string def = "")
        {
            if (Contains(key) && roamingSettings[key] is string)
            {
                return roamingSettings[key] as string; 
            }
            else
            {
                return def;
            }
        }

        public static int GetInt(string key, int def = 0)
        {
            if (Contains(key) && roamingSettings[key] is int)
            {
                return (int) roamingSettings[key];
            }
            else
            {
                return def;
            }
        }

        public static void RemoveKey(string key)
        {
            roamingSettings.Remove(key);
        }
    }
}