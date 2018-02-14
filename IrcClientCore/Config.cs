using System;
using System.Collections.Generic;

namespace IrcClientCore
{
    public static class Config
    {
        public static Dictionary<string, object> RoamingSettings = new Dictionary<string, object>();
        
        // behaviour settings
        public const string UserListClick = "userlistclick";
        public const string SwitchOnJoin = "switchonjoin";
        public const string UseTabs = "usetabs";

        // connection settings
        public const string DefaultUsername = "defaultusername";
        public const string AutoReconnect = "autoreconnect";
        public const string IgnoreSsl = "ignoressl";

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
            return RoamingSettings.ContainsKey(key);
        }

        public static void SetString(string key, string value)
        {
            RoamingSettings[key] = value;
        }

        public static void SetInt(string key, int value)
        {
            RoamingSettings[key] = value;
        }

        public static void SetBoolean(string key, bool value)
        {
            RoamingSettings[key] = value;
        }

        public static bool GetBoolean(string key, bool def = false)
        {
            if (Contains(key) && RoamingSettings[key] is bool)
            {
                return (bool) RoamingSettings[key];
            }
            else
            {
                return def;
            }
        }

        public static string GetString(string key, string def = "")
        {
            if (Contains(key) && RoamingSettings[key] is string)
            {
                return RoamingSettings[key] as string; 
            }
            else
            {
                return def;
            }
        }

        public static int GetInt(string key, int def = 0)
        {
            if (Contains(key) && RoamingSettings[key] is int)
            {
                return (int) RoamingSettings[key];
            }
            else
            {
                return def;
            }
        }

        public static void RemoveKey(string key)
        {
            RoamingSettings.Remove(key);
        }
    }
}