// ******************************************************************
//       /\ /|       @file       PatchWorldInspectPaneCurTabs.cs
//       \ V/        @brief      to patch WorldInspectPane.WorldInspectPaneCurTab
//       | "")       @author     Shadowrabbit, yingtu0401@gmail.com
//       /  |                    
//      /  \\        @Modified   2021-07-29 18:36:35
//    *(__\_\        @Copyright  Copyright (c) 2021, Shadowrabbit
// ******************************************************************

using System.Collections.Generic;
using HarmonyLib;
using JetBrains.Annotations;
using RabiSquare.RealisticOreGeneration.UI.Planet;
using RimWorld.Planet;
using Verse;

// ReSharper disable All
namespace RabiSquare.RealisticOreGeneration
{
    [UsedImplicitly]
    [HarmonyPatch(typeof(WorldInspectPane))]
    [HarmonyPatch("CurTabs", MethodType.Getter)]
    public class PatchWorldInspectPaneCurTabs
    {
        private static readonly WITab oreTileInfoTab = new OreTileInfoTab();

        /// <summary>
        /// add new tab
        /// </summary>
        [UsedImplicitly]
        [HarmonyPostfix]
        public static void Postfix(WorldInspectPane __instance, ref IEnumerable<InspectTabBase> __result,
            WITab[] ___TileTabs)
        {
            if (__result == null)
            {
                return;
            }

            if (Find.WorldSelector == null)
            {
                Log.Error($"{MsicDef.LogTag}cant't find worldSelector");
                return;
            }

            var numSelectedObjects = Find.WorldSelector.NumSelectedObjects;
            var selectedTile = Find.WorldSelector.selectedTile;
            //no worldObject and one tile
            if (numSelectedObjects == 0 && selectedTile >= 0)
            {
                if (___TileTabs == null || ___TileTabs.Length == 0)
                {
                    Log.Error($"{MsicDef.LogTag}empty ___TileTabs");
                    return;
                }

                var newTileTabs = new WITab[___TileTabs.Length + 1];
                for (var i = 0; i < ___TileTabs.Length; i++)
                {
                    newTileTabs[i] = ___TileTabs[i];
                }

                newTileTabs[___TileTabs.Length] = oreTileInfoTab;
                __result = newTileTabs;
            }
        }
    }
}