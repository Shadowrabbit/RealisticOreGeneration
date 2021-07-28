﻿// ******************************************************************
//       /\ /|       @file       PatchWorldGeneratorGenerateWorld.cs
//       \ V/        @brief      to patch WorldGenerator.GenerateWorld()
//       | "")       @author     Shadowrabbit, yingtu0401@gmail.com
//       /  |                    
//      /  \\        @Modified   2021-07-28 17:25:23
//    *(__\_\        @Copyright  Copyright (c) 2021, Shadowrabbit
// ******************************************************************
using HarmonyLib;
using JetBrains.Annotations;
using RimWorld.Planet;
using Verse;

namespace RabiSquare.RealisticOreGeneration
{
    [UsedImplicitly]
    [HarmonyPatch(typeof(WorldGenerator), "GenerateWorld")]
    public class PatchWorldGeneratorGenerateWorld
    {
        /// <summary>
        /// calc info of surface ore and underground ore in each tile
        /// </summary>
        [UsedImplicitly]
        [HarmonyPostfix]
        public static void Postfix()
        {
            var world = Current.CreatingWorld;
            if (world == null)
            {
                Log.Error($"{CoreDef.LogTag}world in generating is null");
                return;
            }

            var worldGrid = world.grid;
            if (worldGrid == null)
            {
                Log.Error($"{CoreDef.LogTag}world grid in generating is null");
                return;
            }

            var allTiles = worldGrid.tiles;
            if (allTiles == null || allTiles.Count <= 0)
            {
                Log.Error($"{CoreDef.LogTag}wrong tile count");
                return;
            }

            for (var i = 0; i < allTiles.Count; i++)
            {
                var surfaceAbundance = OreWeightGenerator.GenerateSurfaceAbundance(i);
                var undergroundAbundance = OreWeightGenerator.GenerateUndergroundAbundance(i);
                OreInfoRecoder.Instance.SetSurfaceOreAbundant(i, surfaceAbundance);
                OreInfoRecoder.Instance.SetUndergroundOreAbundant(i, undergroundAbundance);
            }
        }
    }
}
