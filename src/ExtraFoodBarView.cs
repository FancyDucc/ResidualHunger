using UnityEngine;
using UnityEngine.SceneManagement;

namespace FancyDucc.ResidualHunger
{
    // Finds BarGroup / Hunger once the HUD exists, then builds the extra bar.
    internal class ExtraFoodBarView : MonoBehaviour
    {
        float t;

        void Update()
        {
            // wait a few frames for HUD to spawn
            t += Time.unscaledDeltaTime;
            if (t < 0.5f) return;

            var barGroup = GameObject.Find("BarGroup");
            if (barGroup == null) return;

            var hunger = GameObject.Find("Hunger"); // HUD bar
            if (hunger == null)
            {
                // try relative path under BarGroup/Bar/LayoutGroup/Hunger
                var b = barGroup.transform.Find("Bar/LayoutGroup/Hunger");
                if (b == null) return;
                HungerReserveSystem.EnsureExtraFoodBar(barGroup, b);
            }
            else
            {
                HungerReserveSystem.EnsureExtraFoodBar(barGroup, hunger.transform);
            }

            // once created, stop polling
            enabled = false;
        }
    }
}