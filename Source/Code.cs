using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;
using Outposts;

namespace WorldDomination
{
    public class CompProperties_AutoAddPawn : WorldObjectCompProperties
    {
        public CompProperties_AutoAddPawn()
        {
            this.compClass = typeof(WorldObjectComp_AutoAddPawn);
        }
    }

    public class WorldObjectComp_AutoAddPawn : WorldObjectComp
    {
        public bool autoAddActive = true;

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref autoAddActive, "autoAddActive", true);
        }

        public Gizmo GetToggleGizmo()
        {
            return new Command_Toggle
            {
                defaultLabel = "Auto-Add Arrivals",
                defaultDesc = "When enabled, any caravan arriving at this tile will be automatically added to the outpost.",
                icon = TexCommand.ForbidOff,
                isActive = () => autoAddActive,
                toggleAction = () => { autoAddActive = !autoAddActive; }
            };
        }
    }

    [StaticConstructorOnStartup]
    public static class HarmonyInit
    {
        static HarmonyInit()
        {
            new Harmony("com.worlddomination.autoaddpawn").PatchAll();
        }
    }

    [HarmonyPatch(typeof(Outpost), nameof(Outpost.GetGizmos))]
    public static class Patch_Outpost_GetGizmos
    {
        [HarmonyPostfix]
        public static IEnumerable<Gizmo> Postfix(IEnumerable<Gizmo> __result, Outpost __instance)
        {
            foreach (var g in __result) yield return g;

            var comp = __instance.GetComponent<WorldObjectComp_AutoAddPawn>();
            if (comp != null)
            {
                yield return comp.GetToggleGizmo();
            }
        }
    }

    [HarmonyPatch(typeof(Outpost), nameof(Outpost.TickInterval))]
    public static class Patch_Outpost_TickInterval
    {
        [HarmonyPostfix]
        public static void Postfix(Outpost __instance)
        {
            var comp = __instance.GetComponent<WorldObjectComp_AutoAddPawn>();
            if (comp == null || !comp.autoAddActive) return;

            Caravan caravan = Find.WorldObjects.PlayerControlledCaravanAt(__instance.Tile);

            // IMPROVED CHECK: 
            // 1. Caravan is at this tile.
            // 2. Caravan is NOT currently moving.
            // 3. Caravan's destination is actually this tile (it didn't just stop for a rest while traveling elsewhere).
            if (caravan != null && !caravan.pather.MovingNow && caravan.pather.Destination == __instance.Tile)
            {
                List<Pawn> pawns = caravan.PawnsListForReading.ToList();
                bool added = false;
                foreach (var pawn in pawns)
                {
                    if (__instance.AddPawn(pawn)) added = true;
                }

                if (added)
                {
                    Messages.Message($"Caravan auto-added to {__instance.Name}.", __instance, MessageTypeDefOf.PositiveEvent);
                }
            }
        }
    }
}