using BepInEx;
using BepInEx.Logging;
using HarmonyLib;

#if BEPINEX_V6
using BepInEx.Unity.Mono;
#endif

namespace ExtMap.Patcher
{
    [BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        public static ManualLogSource m_logger { get; private set; }
        private void Awake()
        {
            m_logger = Logger;
            var harmony = new Harmony(MyPluginInfo.PLUGIN_NAME);
            harmony.PatchAll();
            Logger.LogInfo($"Plugin {MyPluginInfo.PLUGIN_NAME} is loaded!");
        }
    }
}

