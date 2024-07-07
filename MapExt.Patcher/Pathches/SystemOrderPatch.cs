using ExtMap.Patcher.Systems;
using Game;
using Game.Common;
using HarmonyLib;

namespace ExtMap.Patcher.Pathches
{
    [HarmonyPatch(typeof(SystemOrder))]
    public static class SystemOrderPatch
    {
        [HarmonyPatch("Initialize")]
        [HarmonyPostfix]
        public static void Postfix(UpdateSystem updateSystem)
        {
            updateSystem.UpdateAt<MySystem>(SystemUpdatePhase.GameSimulation);
        }
    }
}
