using BepInEx;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;

namespace SpawnAnalysis;

[BepInPlugin(GUID, MODNAME, VERSION)]
public class Plugin : BasePlugin
{
    internal const string AUTHOR = "tru0067";
    internal const string MODNAME = "SpawnAnalysis";
    internal const string GUID = AUTHOR + "." + MODNAME;
    internal const string VERSION = "1.0.0";

    public override void Load()
    {
        Logger.SetupFromInit(this.Log);
        Logger.Info(MODNAME + " is loading...");
        new Harmony(GUID).PatchAll();
        Logger.Info(MODNAME + " loaded!");
    }
}
