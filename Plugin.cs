using BepInEx;
using HarmonyLib;
using UnityEngine;
using TMPro;
using Behaviour.UI.Travel;
using Behaviour.Managers;
using Behaviour.Util;
using Source.Galaxy;
using Source.Galaxy.POI;
using Source.Player;

namespace RouteDistanceMod
{
    [BepInPlugin("h3draut3r.routedistance", "Route Distance HUD", "0.1.0")]
    public class Plugin : BaseUnityPlugin
    {
        private void Awake()
        {
            new Harmony("h3draut3r.routedistance").PatchAll();
        }
    }
	
	
	[HarmonyPatch(typeof(TravelInfo), "Awake")]
    public static class TravelInfo_Awake_Patch
    {
        public static TextMeshProUGUI routeDistanceText;
        public static TextMeshProUGUI routeEtaText;

        // Abstand der neuen Zeilen zu ihrem Original (negativ = nach unten)
        private const float DistanceLineOffset = -25f;
        private const float EtaLineOffset = -25f;

        static void Postfix(TravelInfo __instance)
        {
            // Distanz-Zeile: unter TravelInfo.distanceText
            var distanceText = AccessTools.Field(typeof(TravelInfo), "distanceText")
                .GetValue(__instance) as TextMeshProUGUI;
            if (distanceText != null)
            {
                routeDistanceText = CloneBelow(distanceText, "RouteDistanceText", DistanceLineOffset);
            }

            // ETA-Zeile: unter TravelETAInfo.etaText
            TextMeshProUGUI etaText = null;
            if (__instance.travelETAInfo != null)
            {
                var f = AccessTools.Field(typeof(TravelETAInfo), "etaText");
                etaText = f != null ? f.GetValue(__instance.travelETAInfo) as TextMeshProUGUI : null;
            }
            if (etaText != null)
            {
                routeEtaText = CloneBelow(etaText, "RouteEtaText", EtaLineOffset);
            }
        }

        private static TextMeshProUGUI CloneBelow(TextMeshProUGUI anchor, string name, float yOffset)
        {
            GameObject clone = Object.Instantiate(anchor.gameObject, anchor.transform.parent);
            clone.name = name;

            // Falls TravelETAInfo auf demselben GameObject sitzt: nicht mitklonen
            foreach (var c in clone.GetComponents<TravelETAInfo>())
            {
                Object.Destroy(c);
            }

            Transform parent = anchor.transform.parent;
            bool hasLayoutGroup = parent != null
                && (parent.GetComponent("HorizontalOrVerticalLayoutGroup") != null
                 || parent.GetComponent("GridLayoutGroup") != null);

            if (hasLayoutGroup)
            {
                // Layout-Group bestimmt die Position -> nur Reihenfolge festlegen
                clone.transform.SetSiblingIndex(anchor.transform.GetSiblingIndex() + 1);
            }
            else
            {
                RectTransform rt = clone.GetComponent<RectTransform>();
                rt.anchoredPosition = anchor.rectTransform.anchoredPosition + new Vector2(0f, yOffset);
            }

            TextMeshProUGUI text = clone.GetComponent<TextMeshProUGUI>();
            text.enableAutoSizing = true;
            text.fontSizeMin = 4f;
            text.fontSizeMax = anchor.fontSize;
            text.overflowMode = TextOverflowModes.Truncate;
            return text;
        }
    }

    // Start der Reise / Chargen: initialDistance ist hier sicher
    [HarmonyPatch(typeof(TravelInfo), "InitializeTravelInformation")]
    public static class TravelInfo_Init_Patch
    {
        static void Postfix(float initialDistance, float unitsPerSecond)
        {
            // Beim Chargen anzeigen, solange noch Sprünge folgen oder wir gerade
            // aus einem Sprungtor kommen (dort läuft das Chargen des letzten Segments)
            bool show = HudTextHelper.HasJumpsAhead() || HudTextHelper.IsAtGate();
            HudTextHelper.SetVisible(show);
            if (show)
            {
                HudTextHelper.UpdateText(initialDistance, unitsPerSecond);
            }
        }
    }

    // Laufende Reise: der remainingDistance-Parameter wird im Original mit *100
    // überschrieben, daher holen wir den Rohwert aus dem TravelManager.
    [HarmonyPatch(typeof(TravelInfo), "UpdateTravelInfo")]
    public static class TravelInfo_Update_Patch
    {
        static void Postfix(float unitsPerSecond)
        {
            var tm = Singleton<TravelManager>.Instance;
            if (tm == null) return;

            // Letztes Segment: keine Zusatzzeilen mehr
            bool show = HudTextHelper.HasJumpsAhead();
            HudTextHelper.SetVisible(show);
            if (show)
            {
                HudTextHelper.UpdateText(tm.remainingDistance, unitsPerSecond);
            }
        }
    }

    internal static class HudTextHelper
    {
        // waypoints[0] ist das aktuelle Ziel. Mehr als ein Eintrag = noch Sprünge/Segmente danach
        internal static bool HasJumpsAhead()
        {
            var player = GamePlayer.current;
            return player != null && player.waypoints != null && player.waypoints.Count > 1;
        }

        internal static bool IsAtGate()
        {
            var player = GamePlayer.current;
            if (player == null) return false;
            var poi = player.currentPointOfInterest;
            return poi is JumpGate || poi is Wormhole;
        }

        internal static void SetVisible(bool visible)
        {
            SetActive(TravelInfo_Awake_Patch.routeDistanceText, visible);
            SetActive(TravelInfo_Awake_Patch.routeEtaText, visible);
        }

        private static void SetActive(TextMeshProUGUI text, bool active)
        {
            if (text != null && text.gameObject.activeSelf != active)
            {
                text.gameObject.SetActive(active);
            }
        }

        internal static void UpdateText(float firstSegmentDistance, float currentSpeed)
        {
            RouteEstimate est = RouteDistanceCalculator.Compute(firstSegmentDistance, currentSpeed);

            if (TravelInfo_Awake_Patch.routeDistanceText != null)
            {
                TravelInfo_Awake_Patch.routeDistanceText.text = $"To Dest: {est.distance *100f:0} Ls";
            }
            if (TravelInfo_Awake_Patch.routeEtaText != null)
            {
                TravelInfo_Awake_Patch.routeEtaText.text = $"Dest ETA: {FormatTime(est.seconds)}";
            }
        }

        private static string FormatTime(float seconds)
        {
            if (seconds < 0f || float.IsNaN(seconds) || float.IsInfinity(seconds)) return "--";
            int s = Mathf.CeilToInt(seconds);
            int h = s / 3600;
            int m = (s % 3600) / 60;
            int sec = s % 60;
            return h > 0 ? $"{h}:{m:00}:{sec:00}" : $"{m}:{sec:00}";
        }
    }
}