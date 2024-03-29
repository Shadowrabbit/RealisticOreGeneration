﻿// ******************************************************************
//       /\ /|       @file       CompOreScanner.cs
//       \ V/        @brief      This will replace CompProperties_LongRangeMineralScanner work
//       | "")       @author     Shadowrabbit, yingtu0401@gmail.com
//       /  |                    
//      /  \\        @Modified   2021-07-30 17:19:09
//    *(__\_\        @Copyright  Copyright (c) 2021, Shadowrabbit
// ******************************************************************

using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace RabiSquare.RealisticOreGeneration
{
    [StaticConstructorOnStartup]
    public class CompOreScanner : CompScanner
    {
        private const int RangeModeRadius = 6;
        private const int SingleModeRadius = 24;

        private static readonly Texture2D SingleScanModeCommand =
            ContentFinder<Texture2D>.Get("UI/Commands/SingleMode");

        private static readonly Texture2D
            RangeScanModeCommand = ContentFinder<Texture2D>.Get("UI/Commands/AutoRangeMode");

        private static readonly Texture2D SurfaceScanModeCommand =
            ContentFinder<Texture2D>.Get("UI/Commands/SurfaceMode");

        private static readonly Texture2D UndergroundScanModeCommand =
            ContentFinder<Texture2D>.Get("UI/Commands/UndergroundMode");

        private static readonly Texture2D
            TileSelectedCommand = ContentFinder<Texture2D>.Get("UI/Commands/SelectTarget");

        private static readonly Texture2D ScanCursor =
            ContentFinder<Texture2D>.Get("UI/Overlays/ScanCursor");

        private OreScanMode _oreScanMode = OreScanMode.RangeSurface;
        private Dictionary<int, int> _ringMap = new Dictionary<int, int>();
        private int _selectedTile = -1;
        private new CompPropertiesOreScanner Props => (CompPropertiesOreScanner) props;

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            GetRingMap(SingleModeRadius);
            FindDefaultTarget();
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref _selectedTile, "_selectedTile");
        }

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            foreach (var baseGizmo in base.CompGetGizmosExtra()) yield return baseGizmo;
            yield return GetScanAreaGizmo();
            foreach (var scanModeGizmo in GetScanModeGizmo()) yield return scanModeGizmo;
        }

        protected override void DoFind(Pawn worker)
        {
            switch (_oreScanMode)
            {
                case OreScanMode.SingleUnderground:
                case OreScanMode.RangeUnderground:
                    OnUndergroundFind();
                    break;
                default:
                    OnSurfaceFind();
                    break;
            }

            Find.LetterStack.ReceiveLetter("SrScanComplete".Translate(), "SrScanCompleteDesc".Translate(),
                LetterDefOf.PositiveEvent, new GlobalTargetInfo(_selectedTile));
            if (Prefs.DevMode) Log.Message($"{MsicDef.LogTag}scanning complete: {_selectedTile}");
            FindDefaultTarget();
        }

        public override void CompTickRare()
        {
            //works well
            if (_selectedTile != -1) return;
            //try find target
            FindDefaultTarget();
            if (_selectedTile != -1) return;
            //still no target disable try change mode
            if ((_oreScanMode & OreScanMode.RangeSurface) == OreScanMode.RangeSurface)
            {
                Messages.Message("SrAutomaticSwitchingMode".Translate(parent.Label), MessageTypeDefOf.NeutralEvent);
                _oreScanMode &= ~OreScanMode.RangeSurface;
                FindDefaultTarget();
                if (_selectedTile != -1) return;
            }

            if (!CanUseNow)
            {
                return;
            }

            //still no target shut down
            var comp = parent.GetComp<CompFlickable>();
            if (comp == null)
            {
                Log.Warning($"{MsicDef.LogTag}can't find comp");
                return;
            }

            comp.SwitchIsOn = false;
            Messages.Message("SrNoTargetTile".Translate(parent.Label), MessageTypeDefOf.NeutralEvent);
        }

        private void FindDefaultTarget()
        {
            switch (_oreScanMode)
            {
                case OreScanMode.SingleSurface:
                    FindDefaultTargetSingle(true);
                    break;
                case OreScanMode.SingleUnderground:
                    FindDefaultTargetSingle(false);
                    break;
                case OreScanMode.RangeSurface:
                    FindDefaultTargetRange(true);
                    break;
                case OreScanMode.RangeUnderground:
                    FindDefaultTargetRange(false);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            if (_selectedTile != -1)
            {
                UpdateCostTime();
            }

            if (Prefs.DevMode) Log.Message($"{MsicDef.LogTag}set default target: {_selectedTile}");
        }

        private void FindDefaultTargetSingle(bool isSurface)
        {
            var list = _ringMap.Keys.ToList();
            if (list.Count <= 0)
            {
                _selectedTile = -1;
                return;
            }

            list.Shuffle();
            foreach (var tileId in list)
            {
                switch (isSurface)
                {
                    //not valid target
                    case true when WorldOreInfoRecorder.Instance.IsTileScannedSurface(tileId):
                    case false when WorldOreInfoRecorder.Instance.IsTileScannedUnderground(tileId):
                        continue;
                }

                if (tileId.IsTileOceanOrLake())
                {
                    continue;
                }

                _selectedTile = tileId;
                return;
            }

            _selectedTile = -1;
        }

        private void FindDefaultTargetRange(bool isSurface)
        {
            foreach (var kvp in from kvp in _ringMap
                where !isSurface || !WorldOreInfoRecorder.Instance.IsTileScannedSurface(kvp.Key)
                where isSurface || !WorldOreInfoRecorder.Instance.IsTileScannedUnderground(kvp.Key)
                where !kvp.Key.IsTileOceanOrLake()
                where kvp.Value <= RangeModeRadius
                select kvp)
            {
                _selectedTile = kvp.Key;
                return;
            }

            _selectedTile = -1;
        }

        private void GetRingMap(int radius)
        {
            var worldGrid = Find.WorldGrid;
            if (worldGrid == null) throw new Exception($"{MsicDef.LogTag}can't find world grid");
            _ringMap = new Dictionary<int, int>(); // the result we want
            var currentRing = new List<int> {parent.Tile}; //to calc outer ring
            var outerRing = new List<int>(); //to calc which tile can be scanned
            var innerCircleSet = new HashSet<int> {parent.Tile}; //searched tiles
            var tempNeighborList = new List<int>();
            for (var i = 1; i <= radius; i++)
            {
                //calc outer ring
                outerRing.Clear();
                foreach (var tileId in currentRing)
                {
                    worldGrid.GetTileNeighbors(tileId, tempNeighborList);
                    foreach (var neighbor in tempNeighborList.Where(neighbor => !innerCircleSet.Contains(neighbor)))
                    {
                        innerCircleSet.Add(neighbor);
                        //invalid tile
                        if (neighbor.IsTileOceanOrLake())
                        {
                            continue;
                        }

                        outerRing.Add(neighbor);
                    }
                }

                currentRing.Clear();
                currentRing.AddRange(outerRing);
                foreach (var tileId in outerRing) _ringMap.Add(tileId, i);
            }

            if (!Prefs.DevMode) return;
            Log.Message($"{MsicDef.LogTag}ring map count: {_ringMap.Count}");
        }

        /// <summary>
        /// distance of target tile will affect cost
        /// </summary>
        private void UpdateCostTime()
        {
            //set time 
            var distance = GetTargetDistance(_selectedTile);
            Props.scanFindGuaranteedDays = 1.4f * distance / (distance + 1);
            Props.scanFindMtbDays = Props.scanFindGuaranteedDays / 2;
            if ((_oreScanMode & OreScanMode.SingleUnderground) != OreScanMode.SingleUnderground) return;
            Props.scanFindGuaranteedDays *= 2;
            Props.scanFindMtbDays *= 2;
        }

        private int GetTargetDistance(int tileId)
        {
            if (_ringMap.ContainsKey(tileId)) return _ringMap[tileId];
            Log.Error($"{MsicDef.LogTag}can't find target tile in circle tileId: {tileId}");
            return 1;
        }

        private void OnSurfaceFind()
        {
            if (_selectedTile == -1)
            {
                Log.Warning($"{MsicDef.LogTag}find tile with none on surface");
                return;
            }

            WorldOreInfoRecorder.Instance.RecordScannedTileSurface(_selectedTile);
        }

        private void OnUndergroundFind()
        {
            if (_selectedTile == -1)
            {
                Log.Warning($"{MsicDef.LogTag}find tile with none in underground");
                return;
            }

            WorldOreInfoRecorder.Instance.RecordScannedTileUnderground(_selectedTile);
        }

        private IEnumerable<Gizmo> GetScanModeGizmo()
        {
            var commandChange = new Command_Action
            {
                defaultLabel = "SrScanModeChange".Translate(),
                icon = (_oreScanMode & OreScanMode.RangeSurface) == OreScanMode.RangeSurface
                    ? RangeScanModeCommand
                    : SingleScanModeCommand,
                defaultDesc = (_oreScanMode & OreScanMode.RangeSurface) == OreScanMode.RangeSurface
                    ? "SrChangeToSingleMode".Translate()
                    : "SrChangeToRangeMode".Translate(),
                action = OnClickScanModeChange
            };

            yield return commandChange;
            if ((_oreScanMode & OreScanMode.RangeSurface) == OreScanMode.RangeSurface) yield break;
            var commandSelectTile = new Command_Action
            {
                defaultLabel = "SrSelect".Translate(),
                icon = TileSelectedCommand,
                action = OnClickTileSelect
            };

            yield return commandSelectTile;
        }

        private Gizmo GetScanAreaGizmo()
        {
            var commandAction = new Command_Action
            {
                defaultLabel = "SrScanAreaChange".Translate(),
                icon = (_oreScanMode & OreScanMode.SingleUnderground) == OreScanMode.SingleUnderground
                    ? UndergroundScanModeCommand
                    : SurfaceScanModeCommand,
                defaultDesc = (_oreScanMode & OreScanMode.SingleUnderground) == OreScanMode.SingleUnderground
                    ? "SrChangeToSurfaceMode".Translate()
                    : "SrChangeToUndergroundMode".Translate(),
                action = OnClickScanAreaChange
            };
            return commandAction;
        }

        private void OnClickScanModeChange()
        {
            _oreScanMode = (_oreScanMode & OreScanMode.RangeSurface) == OreScanMode.RangeSurface
                ? _oreScanMode & ~OreScanMode.RangeSurface
                : _oreScanMode | OreScanMode.RangeSurface;
            FindDefaultTarget();
        }

        private void OnClickScanAreaChange()
        {
            _oreScanMode = (_oreScanMode & OreScanMode.SingleUnderground) == OreScanMode.SingleUnderground
                ? _oreScanMode & ~OreScanMode.SingleUnderground
                : _oreScanMode | OreScanMode.SingleUnderground;
            FindDefaultTarget();
        }

        private bool OnWorldTargetSelected(GlobalTargetInfo target)
        {
            if (!target.IsValid)
            {
                Messages.Message("MessageTransportPodsDestinationIsInvalid".Translate(),
                    MessageTypeDefOf.RejectInput, false);
                return false;
            }

            if (Find.WorldGrid.TraversalDistanceBetween(parent.Tile, target.Tile) > SingleModeRadius)
            {
                Messages.Message("TransportPodDestinationBeyondMaximumRange".Translate(), MessageTypeDefOf.RejectInput,
                    false);
                return false;
            }

            //has scanned
            if (WorldOreInfoRecorder.Instance.IsTileScannedUnderground(target.Tile) &&
                (_oreScanMode & OreScanMode.SingleUnderground) == OreScanMode.SingleUnderground ||
                WorldOreInfoRecorder.Instance.IsTileScannedSurface(target.Tile) &&
                (_oreScanMode & OreScanMode.SingleUnderground) != OreScanMode.SingleUnderground)
            {
                Messages.Message("SrRepeatScan".Translate(), MessageTypeDefOf.RejectInput, false);
                return false;
            }

            _selectedTile = target.Tile;
            UpdateCostTime();
            return true;
        }

        private string TargetingLabelGetter(GlobalTargetInfo target)
        {
            if (!target.IsValid)
            {
                GUI.color = ColoredText.WarningColor;
                return "MessageTransportPodsDestinationIsInvalid".Translate();
            }

            if (Find.WorldGrid.TraversalDistanceBetween(parent.Tile, target.Tile) > SingleModeRadius)
            {
                GUI.color = ColoredText.WarningColor;
                return "TransportPodDestinationBeyondMaximumRange".Translate();
            }

            if (WorldOreInfoRecorder.Instance.IsTileScannedUnderground(target.Tile) &&
                (_oreScanMode & OreScanMode.SingleUnderground) == OreScanMode.SingleUnderground ||
                WorldOreInfoRecorder.Instance.IsTileScannedSurface(target.Tile) &&
                (_oreScanMode & OreScanMode.SingleUnderground) != OreScanMode.SingleUnderground)
            {
                GUI.color = ColoredText.WarningColor;
                return "SrRepeatScan".Translate();
            }

            GUI.color = ColoredText.ExpectationsColor;
            return "SrScan".Translate();
        }

        private void OnClickTileSelect()
        {
            CameraJumper.TryJump(CameraJumper.GetWorldTarget((GlobalTargetInfo) parent));
            Find.WorldSelector.ClearSelection();
            Find.WorldTargeter.BeginTargeting(OnWorldTargetSelected, true, ScanCursor, true,
                () => GenDraw.DrawWorldRadiusRing(parent.Tile, SingleModeRadius), TargetingLabelGetter);
        }
    }
}