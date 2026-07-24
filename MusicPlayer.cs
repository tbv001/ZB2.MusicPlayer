using System;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using MusicPlayer.Components;

namespace MusicPlayer;

[BepInPlugin(PluginGuid, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
public class MusicPlayer : BaseUnityPlugin
{
    internal const string PluginGuid = "com.theblackvoid.musicplayer";
    internal new static ManualLogSource Logger;
    public Harmony HarmonyInstance = new(PluginGuid);
    public static ConfigEntry<int> MusicVolumeCfg;

    private void Awake()
    {
        Logger = base.Logger;
        try
        {
            Configure();
            gameObject.AddComponent<AudioLoader>();
            HarmonyInstance.PatchAll(Assembly.GetExecutingAssembly());
            Logger.LogInfo("Successfully loaded!");
        }
        catch (Exception ex)
        {
            Logger.LogError($"Error while loading the plugin: {ex}");
        }
    }

    private void Configure()
    {
        MusicVolumeCfg = Config.Bind("Settings", "Music Volume", 50,
            new ConfigDescription("Sets the music volume", new AcceptableValueRange<int>(0, 100)));
    }
}
