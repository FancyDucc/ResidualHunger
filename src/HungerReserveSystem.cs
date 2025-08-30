using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace FancyDucc.ResidualHunger
{
    // Keeps the single source of truth for the reserve (0..1) and exposes helpers.
    internal static class HungerReserveSystem
    {
        // If you later support splits per-player, lift this into a Dictionary<character, State>
        internal class State
        {
            public float reserve01 = 0f;
            public float hungerLast01 = 0f;   // previous hunger bar fraction
            public float hungerTickPerSec01 = 0.0f; // learned drain rate
            public bool hungerActiveLast = false;

            public GameObject extraFoodGO;      // our cloned bar root
            public RectTransform fillRT;        // driven width rect
            public float fillFullWidth = 0f;    // cached full px width
        }

        internal static readonly State S = new();

        internal static void Init() { }

        // --- UI Bootstrap / Update ---
        internal static void EnsureExtraFoodBar(GameObject barGroup, Transform hungerBar)
        {
            if (S.extraFoodGO != null) return;

            // locate the existing ExtraStaminaBar under BarGroup and clone it
            var extraStam = barGroup.transform.Find("ExtraStaminaBar");
            if (extraStam == null)
            {
                Plugin.Log.LogWarning("ExtraStaminaBar not found under BarGroup; falling back to creating a simple bar.");
                S.extraFoodGO = new GameObject("ExtraFoodBar", typeof(RectTransform), typeof(ContentSizeFitter));
                S.extraFoodGO.transform.SetParent(barGroup.transform, false);
            }
            else
            {
                S.extraFoodGO = UnityEngine.Object.Instantiate(extraStam.gameObject, extraStam.parent);
                S.extraFoodGO.name = "ExtraFoodBar";
            }

            // stomach icon: copy sprite from Hunger/Icon onto our clone's Icon (if present)
            var hungerIconImg = hungerBar.Find("Icon")?.GetComponent<Image>();
            var ourIconImg = S.extraFoodGO.transform.Find("Icon")?.GetComponent<Image>();
            if (hungerIconImg != null && ourIconImg != null) ourIconImg.sprite = hungerIconImg.sprite;

            // tint the fill to yellow
            var fill = S.extraFoodGO.transform.Find("Fill")?.GetComponent<Image>();
            if (fill != null)
            {
                fill.color = new Color(1.00f, 0.82f, 0.15f, fill.color.a);
                S.fillRT = fill.rectTransform;
                S.fillFullWidth = S.fillRT.rect.width; // initial width as 100%
            }

            // start hidden
            S.extraFoodGO.SetActive(false);
        }

        internal static void SetReserve(float v01)
        {
            S.reserve01 = Mathf.Clamp01(v01);
            UpdateBarVisual();
        }

        internal static void AddReserve(float delta01)
        {
            if (delta01 <= 0f) return;
            S.reserve01 = Mathf.Clamp01(S.reserve01 + delta01);
            UpdateBarVisual();
        }

        internal static float ConsumeReserve(float want01)
        {
            if (want01 <= 0f || S.reserve01 <= 0f) return 0f;
            float take = Mathf.Min(want01, S.reserve01);
            S.reserve01 -= take;
            UpdateBarVisual();
            return take;
        }

        internal static void UpdateBarVisual()
        {
            bool show = S.reserve01 > 0.0001f;
            if (S.extraFoodGO != null && S.extraFoodGO.activeSelf != show)
                S.extraFoodGO.SetActive(show);

            if (S.fillRT != null && S.fillFullWidth > 1f)
            {
                float w = S.fillFullWidth * Mathf.Clamp01(S.reserve01);
                S.fillRT.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, w);
            }
        }

        // Adaptive “learned” hunger drain rate (so reserve drains at the same speed)
        internal static void LearnHungerTick(float hungerPre01, float hungerPost01, float dt)
        {
            // only learn when hunger increased (delta>0) and no reserve was interfering
            float d = hungerPost01 - hungerPre01;
            if (d > 0.0001f && dt > 0f)
            {
                float perSec = d / dt;
                S.hungerTickPerSec01 = Mathf.Lerp(S.hungerTickPerSec01, perSec, 0.2f); // smooth
            }
        }

        internal static void DrainReserveLikeHunger(float dt)
        {
            if (S.reserve01 <= 0f || S.hungerTickPerSec01 <= 0f) return;
            SetReserve(S.reserve01 - S.hungerTickPerSec01 * dt);
        }
    }
}