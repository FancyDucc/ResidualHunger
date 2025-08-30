using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace FancyDucc.ResidualHunger
{
    // Hook HUD spawn to add our bootstrapper MonoBehaviour
    [HarmonyPatch(typeof(BarAffliction), "OnEnable")]
    class Patch_BarAffliction_OnEnable
    {
        static void Postfix(BarAffliction __instance)
        {
            if (__instance.afflictionType.ToString() != "Hunger") return;

            // make sure our bootstrapper exists in the scene
            if (GameObject.FindObjectOfType<ExtraFoodBarView>() == null)
            {
                var go = new GameObject("ResidualHungerBootstrap");
                go.hideFlags = HideFlags.HideAndDontSave;
                go.AddComponent<ExtraFoodBarView>();
            }

            // prime last state
            HungerReserveSystem.S.hungerLast01 = ReadSize01(__instance);
            HungerReserveSystem.S.hungerActiveLast = __instance.isActiveAndEnabled;
        }

        static float ReadSize01(BarAffliction b)
        {
            var f = b.GetType().GetField("size", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (f != null) return Mathf.Clamp01((float)f.GetValue(b) / 100f);
            var w = b.GetType().GetField("width", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (w != null) return Mathf.Clamp01((float)w.GetValue(b) / 100f);
            return 0f;
        }

        static void WriteSize01(BarAffliction b, float v01)
        {
            var f = b.GetType().GetField("size", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (f != null) { f.SetValue(b, Mathf.Clamp01(v01) * 100f); return; }
            var w = b.GetType().GetField("width", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (w != null) { w.SetValue(b, Mathf.Clamp01(v01) * 100f); return; }
        }
    }

    // Intercept the per-frame UI update to:
    //  - learn hunger drain rate
    //  - siphon any hunger increases into our reserve while reserve > 0
    //  - detect heal/overshoot when hunger flips inactive and convert to reserve
    [HarmonyPatch(typeof(BarAffliction), "UpdateAffliction")]
    class Patch_BarAffliction_Update
    {
        static float _pre01;
        static float _dt;

        static void Prefix(BarAffliction __instance)
        {
            if (__instance.afflictionType.ToString() != "Hunger") return;
            _pre01 = ReadSize01(__instance);
            _dt = Time.deltaTime;
        }

        static void Postfix(BarAffliction __instance)
        {
            if (__instance.afflictionType.ToString() != "Hunger") return;

            float post01 = ReadSize01(__instance);
            bool nowActive = __instance.isActiveAndEnabled;

            // learn: when hunger crept up naturally (no reserve interference)
            HungerReserveSystem.LearnHungerTick(_pre01, post01, _dt);

            // if hunger tried to increase this frame and we have reserve, consume reserve instead and counteract the UI raise
            if (post01 > _pre01 && HungerReserveSystem.S.reserve01 > 0f)
            {
                float want = post01 - _pre01;
                float took = HungerReserveSystem.ConsumeReserve(want);
                if (took > 0f)
                {
                    post01 -= took;
                    WriteSize01(__instance, post01);
                    nowActive = post01 > 0.001f;
                }
            }

            // if hunger decreased (player ate) and bar hit 0 this frame, treat any extra as reserve.
            if (HungerReserveSystem.S.hungerActiveLast && !nowActive)
            {
                // overshoot is post01 clamped to 0; approximate using the drop we observed
                float drop = Mathf.Max(0f, _pre01 - post01);
                // if it dropped to (or past) 0, add the remainder into reserve
                if (post01 <= 0.0001f && drop > 0f)
                    HungerReserveSystem.AddReserve(drop);
            }

            HungerReserveSystem.S.hungerLast01 = post01;
            HungerReserveSystem.S.hungerActiveLast = nowActive;

            // while hunger inactive, drain reserve at learned speed
            if (!nowActive) HungerReserveSystem.DrainReserveLikeHunger(_dt);
        }

        static float ReadSize01(BarAffliction b)
        {
            var f = b.GetType().GetField("size", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (f != null) return Mathf.Clamp01((float)f.GetValue(b) / 100f);
            var w = b.GetType().GetField("width", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (w != null) return Mathf.Clamp01((float)w.GetValue(b) / 100f);
            return 0f;
        }

        static void WriteSize01(BarAffliction b, float v01)
        {
            var f = b.GetType().GetField("size", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (f != null) { f.SetValue(b, Mathf.Clamp01(v01) * 100f); return; }
            var w = b.GetType().GetField("width", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (w != null) { w.SetValue(b, Mathf.Clamp01(v01) * 100f); return; }
        }
    }
}