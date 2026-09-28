using System.Collections.Generic;
using System.Reflection;
using Behaviour.Managers;
using Behaviour.Util;
using HarmonyLib;
using Source.Galaxy;
using Source.Galaxy.POI;
using Source.Player;
using UnityEngine;

namespace RouteDistanceMod
{
    public struct RouteEstimate
    {
        public float distance; // Ls, gesamte Restroute
        public float seconds;  // < 0 = unbekannt
    }

    public static class RouteDistanceCalculator
    {
        public const float GateTransitionSeconds = 7f;
        public const float FastLaneMultiplier = 7f;

        private static readonly FieldInfo FuelField = AccessTools.Field(typeof(TravelManager), "fuelMultiplier");
        private static readonly FieldInfo BonusField = AccessTools.Field(typeof(TravelManager), "bonusMultiplier");

        public static RouteEstimate Compute(float firstSegmentDistance, float currentSpeed)
        {
            var result = new RouteEstimate { distance = firstSegmentDistance, seconds = -1f };

            var tm = Singleton<TravelManager>.Instance;
            var player = GamePlayer.current;
            var ship = GameplayManager.Instance != null ? GameplayManager.Instance.spaceShip : null;
            if (tm == null || player == null || ship == null) return result;

            float fuel = FuelField != null ? (float)FuelField.GetValue(tm) : 1f;
            float bonus = BonusField != null ? (float)BonusField.GetValue(tm) : 1f;

            float vNormal = ship.baseMaxWarpSpeed * fuel * bonus;
            float acc = ship.baseWarpAcceleration * fuel * bonus;
            float vFast = ship.baseMaxWarpSpeed * FastLaneMultiplier;
            bool valid = vNormal > 0f && acc > 0f && vFast > 0f;

            float distance = firstSegmentDistance;
            float seconds = 0f;

            // Segment 0: aktuelles Segment, echter Ist-Zustand
            seconds += tm.fastLaneTravelActive
                ? firstSegmentDistance / vFast
                : NormalTravelTime(firstSegmentDistance, currentSpeed, vNormal, acc);

            // Alle weiteren Segmente
            List<MapPointOfInterest> waypoints = player.waypoints;
            if (waypoints != null)
            {
                for (int i = 0; i < waypoints.Count - 1; i++)
                {
                    MapPointOfInterest from = waypoints[i];
                    MapPointOfInterest to = waypoints[i + 1];
                    if (from == null || to == null) continue;

                    float d = Vector2.Distance(GetArrivalPosition(from, to), to.position);
                    distance += d;

                    if (IsJump(from, to))
                    {
                        seconds += GateTransitionSeconds;
                    }

                    bool fast = player.fastLaneTravelUnlocked && to is JumpGate g && g.canUseJumpGate;
                    seconds += fast ? d / vFast : NormalTravelTime(d, 0f, vNormal, acc);
                }
            }

            result.distance = distance;
            result.seconds = valid ? seconds : -1f;
            return result;
        }

        // Zeit für ein Segment mit Beschleunigung -> Reisegeschwindigkeit -> Abbremsen
        private static float NormalTravelTime(float distance, float v0, float vMax, float acc)
        {
            if (distance <= 0f) return 0f;

            v0 = Mathf.Clamp(v0, 0f, vMax);

            // Schon so schnell, dass nur noch gebremst werden kann
            float brakeDist = v0 * v0 / (2f * acc);
            if (brakeDist >= distance)
            {
                return v0 > 0f ? 2f * distance / v0 : 0f;
            }

            float accelDist = (vMax * vMax - v0 * v0) / (2f * acc);
            float decelDist = vMax * vMax / (2f * acc);

            if (accelDist + decelDist <= distance)
            {
                float cruiseDist = distance - accelDist - decelDist;
                return (vMax - v0) / acc + cruiseDist / vMax + vMax / acc;
            }

            // Strecke zu kurz, um vMax zu erreichen
            float vPeak = Mathf.Sqrt(acc * distance + v0 * v0 / 2f);
            return (vPeak - v0) / acc + vPeak / acc;
        }

        private static bool IsJump(MapPointOfInterest from, MapPointOfInterest to)
        {
            return from is JumpGate || (from is Wormhole && from.system != to.system);
        }

        private static Vector2 GetArrivalPosition(MapPointOfInterest from, MapPointOfInterest to)
        {
            if (from is JumpGate gate && gate.targetSystem != null)
            {
                MapPointOfInterest target = gate.GetTargetPOI();
                if (target != null)
                {
                    return target.position;
                }
            }
            if (from.system != to.system)
            {
                return to.position; // z.B. Wormhole -> Wormhole: keine Strecke
            }
            return from.position;
        }
    }
}