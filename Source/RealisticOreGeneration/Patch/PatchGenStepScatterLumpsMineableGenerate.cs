﻿// ******************************************************************
//       /\ /|       @file       PatchGenStepScatterLumpsMineableGenerate.cs
//       \ V/        @brief      to patch GenStep_ScatterLumpsMineable.Generate()
//       | "")       @author     Shadowrabbit, yingtu0401@gmail.com
//       /  |                    
//      /  \\        @Modified   2021-07-29 12:54:24
//    *(__\_\        @Copyright  Copyright (c) 2021, Shadowrabbit
// ******************************************************************

using HarmonyLib;
using JetBrains.Annotations;
using RimWorld;
using Verse;

namespace RabiSquare.RealisticOreGeneration
{
    [UsedImplicitly]
    [HarmonyPatch(typeof(GenStep_ScatterLumpsMineable), "Generate")]
    public class PatchGenStepScatterLumpsMineableGenerate
    {
        /// <summary>
        ///     hook countPer10kCellsRange with my abundance
        /// </summary>
        [UsedImplicitly]
        [HarmonyPrefix]
        // ReSharper disable once InconsistentNaming
        public static bool Prefix(GenStep_ScatterLumpsMineable __instance, Map map)
        {
            if (map == null) return true;
            var tileId = map.Tile;
            var tileOreData = WorldOreDataGenerator.Instance.GetTileOreData(tileId);
            __instance.countPer10kCellsRange *=
                WorldOreInfoRecorder.Instance.IsTileAbandoned(tileId) ? 0 : tileOreData.OreGenerationFactor;
            if (!Prefs.DevMode) return true;
            Log.Message($"{MsicDef.LogTag}hook abundance success in tile: {tileId}");
            tileOreData.DebugShowSurfaceFactors();
            return true;
        }
    }
}