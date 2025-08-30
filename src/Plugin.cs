using BepInEx;
using BepInEx.Logging;
using HarmonyLib;

namespace FancyDucc.ResidualHunger
{
    [BepInPlugin("FancyDucc.ResidualHunger", "Residual Hunger", "1.0.0")]
    [BepInDependency("PEAKModding.PEAKLib.Core", BepInDependency.DependencyFlags.SoftDependency)]
    public class Plugin : BaseUnityPlugin
    {
        internal static ManualLogSource Log;
        internal static Harmony Harmony;

        private void Awake()
        {
            Log = Logger;
            Harmony = new Harmony("FancyDucc.ResidualHunger");
            Harmony.PatchAll();
            HungerReserveSystem.Init();
            Log.LogInfo("Residual Hunger loaded.");
        }
    }
}