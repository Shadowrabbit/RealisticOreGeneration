﻿// ******************************************************************
//       /\ /|       @file       OreWeightGenerator.cs
//       \ V/        @brief      
//       | "")       @author     Shadowrabbit, yingtu0401@gmail.com
//       /  |                    
//      /  \\        @Modified   2021-07-26 21:10:22
//    *(__\_\        @Copyright  Copyright (c) 2021, Shadowrabbit
// ******************************************************************

using System.Collections.Generic;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace RabiSquare.RealisticOreGeneration
{
    public class OreWeightGenerator : BaseSingleTon<OreWeightGenerator>
    {
        private const float Relief = 15;

        public static Dictionary<string, float> GenerateSurfaceAbundance(int tileId)
        {
            var mapCommonality = new Dictionary<string, float>();
            //generate random resource abundances
            const float qMin = 1f;
            var n = OreInfoRecoder.Instance.GetSurfaceOreDataListCount();
            var q = qMin + Rand.Value * ((float) n / 2 - qMin);

            var arrayNewCommonality = new float[n];
            for (var i = 0; i < n; i++)
            {
                arrayNewCommonality[i] = 1 / (q * Mathf.Sqrt(2 * 3.14f)) *
                                         Mathf.Exp(-Mathf.Pow((i - n / 2), 2) / (2 * Mathf.Pow(q, 2)));
            }

            //shuffle
            arrayNewCommonality.Shuffle();
            //normalisation
            var originTotalCommonality = 0f;
            var currentTotalCommonality = 0f;

            //notmalisation by total resource value
            for (var i = 0; i < n; i++)
            {
                var oreData = OreInfoRecoder.Instance.GetSurfaceOreDataByIndex(i);
                originTotalCommonality += oreData.mineableScatterCommonality * oreData.mineableYield *
                                          oreData.marketValue * oreData.mineableScatterLumpSize;
                currentTotalCommonality += arrayNewCommonality[i] * oreData.mineableYield *
                                           oreData.marketValue * oreData.mineableScatterLumpSize;
            }

            //get grid to find tiles
            var worldGrid = Find.WorldGrid;
            var tile = worldGrid[tileId];
            if (tile == null)
            {
                Log.Error($"[RabiSquare.RealisticOreGeneration]can't find tile: {tileId}");
                return mapCommonality;
            }

            //scale total value to vanilla
            var scaleCommonality = originTotalCommonality / currentTotalCommonality;
            //scale by hilliness type

            var hillinessFactor = CalcHillinessFactor(tile.hilliness);
            scaleCommonality *= hillinessFactor;
            //todo scale by mineable resource multiplier
            //scale by value generated by berlin algorithm map
            var pos = worldGrid.GetTileCenter(tileId);
            var seed = tile.GetHashCode();
            var berlinFactor =
                Mathf.PerlinNoise((pos.x + seed % 100) / Relief, (pos.z + seed % 100) / Relief);
            if (Prefs.DevMode)
            {
                Log.Message($"[RabiSquare.RealisticOreGeneration]surface:");
                Log.Message($"[RabiSquare.RealisticOreGeneration]q: {q}");
                Log.Message($"[RabiSquare.RealisticOreGeneration]hillinessFactor: {hillinessFactor}");
                Log.Message($"[RabiSquare.RealisticOreGeneration]mineable resource multiplier: 1");
                Log.Message($"[RabiSquare.RealisticOreGeneration]pos: {pos}");
                Log.Message($"[RabiSquare.RealisticOreGeneration]seed: {seed}");
                Log.Message($"[RabiSquare.RealisticOreGeneration]relief: {Relief}");
                Log.Message($"[RabiSquare.RealisticOreGeneration]berlinFactor: {berlinFactor}");
            }

            scaleCommonality *= berlinFactor;
            //normalize abundances
            for (var i = 0; i < n; i++)
            {
                var oreData = OreInfoRecoder.Instance.GetSurfaceOreDataByIndex(i);
                mapCommonality.Add(oreData.defName, arrayNewCommonality[i] * scaleCommonality);
            }

            return mapCommonality;
        }

        public static Dictionary<string, float> GenerateUndergroundAbundance(int tileId)
        {
            var mapCommonality = new Dictionary<string, float>();
            //generate random resource abundances
            const float qMin = 1f;
            var n = OreInfoRecoder.Instance.GetUndergroundOreDataListCount();
            var q = qMin + Rand.Value * ((float) n / 2 - qMin);

            var arrayNewCommonality = new float[n];
            for (var i = 0; i < n; i++)
            {
                arrayNewCommonality[i] = 1 / (q * Mathf.Sqrt(2 * 3.14f)) *
                                         Mathf.Exp(-Mathf.Pow((i - n / 2), 2) / (2 * Mathf.Pow(q, 2)));
            }

            //shuffle
            arrayNewCommonality.Shuffle();
            //normalisation
            var originTotalCommonality = 0f;
            var currentTotalCommonality = 0f;

            //notmalisation by total resource value
            for (var i = 0; i < n; i++)
            {
                var oreData = OreInfoRecoder.Instance.GetUndergroundOreDataByIndex(i);
                originTotalCommonality += oreData.mineableScatterCommonality * oreData.mineableYield *
                                          oreData.marketValue * oreData.mineableScatterLumpSize;
                currentTotalCommonality += arrayNewCommonality[i] * oreData.mineableYield *
                                           oreData.marketValue * oreData.mineableScatterLumpSize;
            }

            //get grid to find tiles
            var worldGrid = Find.WorldGrid;
            var tile = worldGrid[tileId];
            if (tile == null)
            {
                Log.Error($"[RabiSquare.RealisticOreGeneration]can't find tile: {tileId}");
                return mapCommonality;
            }

            //scale total value to vanilla
            var scaleCommonality = originTotalCommonality / currentTotalCommonality;
            //scale by hilliness type

            var hillinessFactor = CalcHillinessFactor(tile.hilliness);
            scaleCommonality *= hillinessFactor;
            //todo scale by mineable resource multiplier
            //scale by value generated by berlin algorithm map
            var pos = worldGrid.GetTileCenter(tileId);
            var seed = tile.GetHashCode();
            var berlinFactor =
                Mathf.PerlinNoise((pos.x + seed % 100) / Relief, (pos.z + seed % 100) / Relief);
            if (Prefs.DevMode)
            {
                Log.Message($"[RabiSquare.RealisticOreGeneration]underground:");
                Log.Message($"[RabiSquare.RealisticOreGeneration]q: {q}");
                Log.Message($"[RabiSquare.RealisticOreGeneration]hillinessFactor: {hillinessFactor}");
                Log.Message($"[RabiSquare.RealisticOreGeneration]mineable resource multiplier: 1");
                Log.Message($"[RabiSquare.RealisticOreGeneration]pos: {pos}");
                Log.Message($"[RabiSquare.RealisticOreGeneration]seed: {seed}");
                Log.Message($"[RabiSquare.RealisticOreGeneration]relief: {Relief}");
                Log.Message($"[RabiSquare.RealisticOreGeneration]berlinFactor: {berlinFactor}");
            }

            scaleCommonality *= berlinFactor;
            //normalize abundances
            for (var i = 0; i < n; i++)
            {
                var oreData = OreInfoRecoder.Instance.GetUndergroundOreDataByIndex(i);
                mapCommonality.Add(oreData.defName, arrayNewCommonality[i] * scaleCommonality);
            }

            return mapCommonality;
        }

        private static float CalcHillinessFactor(Hilliness hilliness)
        {
            switch (hilliness)
            {
                case Hilliness.Flat:
                    return 0.25f;
                case Hilliness.SmallHills:
                    return 0.35f;
                case Hilliness.LargeHills:
                    return 0.75f;
                case Hilliness.Mountainous:
                    return 1f;
                default:
                    return 0f;
            }
        }
    }
}