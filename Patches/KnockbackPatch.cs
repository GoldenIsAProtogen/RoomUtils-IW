using GorillaLocomotion;
using HarmonyLib;
using UnityEngine;

namespace RoomUtils.Patches
{
    [HarmonyPatch(typeof(GTPlayer), "ApplyKnockback")]
    public class KnockbackPatch
    {
        public static bool enabled = false;

        public static bool Prefix(Vector3 direction, float speed)
        {
            bool disabled = enabled || Plugin.Knockback.Value || Plugin.KnockbackState.KnockbackEnabled;

            return !disabled;
        }
    }
}