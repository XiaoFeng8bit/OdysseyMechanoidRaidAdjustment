using System;
using System.Linq;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using RimWorld;
using Verse;
using System.Reflection;



namespace OdysseyMechanoidRaidAdjustment
{
    [StaticConstructorOnStartup]
    public class PatchMain
    {
        static PatchMain()
        {
            Harmony harmony = new Harmony("com.OdysseyMechanoidRaidAdjustment");
            harmony.PatchAll(Assembly.GetExecutingAssembly());
        }

        [HarmonyPatch(typeof(ScenPart_PursuingMechanoids))]
        [HarmonyPatch("StartTimers")]
        [HarmonyPatch(new Type[] { typeof(Map) })]
        public static class ScenPart_PursuingMechanoids_StartTimers_Patch
        {
            public static FieldInfo onStartMapField = AccessTools.Field(typeof(ScenPart_PursuingMechanoids), "onStartMap");
            public static FieldInfo mapWarningTimersField = AccessTools.Field(typeof(ScenPart_PursuingMechanoids), "mapWarningTimers");
            public static FieldInfo mapRaidTimersField = AccessTools.Field(typeof(ScenPart_PursuingMechanoids), "mapRaidTimers");
            public static bool Prefix(ScenPart_PursuingMechanoids __instance, Map map)
            {
                if (ModSet._autoClearOnStartMap)
                {
                    if (map.generatorDef == MapGeneratorDefOf.OrbitalRelay)
                    {
                        onStartMapField.SetValue(__instance, true);
                        ModSet._onStartMap = true;
                        Log.Message($"[OMRA] ScenPart_PursuingMechanoids_StartTimers_Patch: onStartMap set to true for map {map} (Orbital Relay).");
                    }
                    else 
                    {
                        onStartMapField.SetValue(__instance, false);
                        ModSet._onStartMap = false;
                        Log.Message($"[OMRA] ScenPart_PursuingMechanoids_StartTimers_Patch: onStartMap set to false for map {map} (not Orbital Relay).");
                    }
                    return true;
                }
                onStartMapField.SetValue(__instance, ModSet._onStartMap);
                Log.Message($"[OMRA] ScenPart_PursuingMechanoids_StartTimers_Patch: onStartMap set to {ModSet._onStartMap} for map {map}.");
                return true;
            }
            public static void Postfix(ScenPart_PursuingMechanoids __instance, Map map)
            {
                if (ModSet._modifyMapWarningTimers)
                {
                    Dictionary<Map, int> temp;
                    temp = (Dictionary<Map, int>)mapWarningTimersField.GetValue(__instance);
                    temp[map] = Find.TickManager.TicksGame + ModSet._mapWarningTimers;
                    Log.Message($"[OMRA] ScenPart_PursuingMechanoids_StartTimers_Patch: mapWarningTimers set to {ModSet._mapWarningTimers} for map {map}.");
                }

                if (ModSet._modifyMapRaidTimers)
                {
                    Dictionary<Map, int> temp;
                    temp = (Dictionary<Map, int>)mapRaidTimersField.GetValue(__instance);
                    temp[map] = Find.TickManager.TicksGame + ModSet._mapRaidTimers;
                    Log.Message($"[OMRA] ScenPart_PursuingMechanoids_StartTimers_Patch: mapRaidTimers set to {ModSet._mapRaidTimers} for map {map}.");
                }
                ModSet._mapStartTimers = Find.TickManager.TicksGame;
            }
        }

        [HarmonyPatch(typeof(ScenPart_PursuingMechanoids))]
        [HarmonyPatch("PostWorldGenerate")]
        [HarmonyPatch(new Type[] { })]
        public static class ScenPart_PursuingMechanoids_PostWorldGenerate_Patch
        {
            public static FieldInfo onStartMapField = AccessTools.Field(typeof(ScenPart_PursuingMechanoids), "onStartMap");
            public static void Postfix(ScenPart_PursuingMechanoids __instance)
            {
                onStartMapField.SetValue(__instance, ModSet._onStartMap);
                Log.Message($"[OMRA] ScenPart_PursuingMechanoids_PostWorldGenerate_Patch: onStartMap set to {ModSet._onStartMap} for world generation.");
            }
        }

        [HarmonyPatch(typeof(ScenPart_PursuingMechanoids))]
        [HarmonyPatch("MapRemoved")]
        [HarmonyPatch(new Type[] { typeof(Map) })]
        public static class ScenPart_PursuingMechanoids_MapRemoved_Patch
        {
            public static FieldInfo onStartMapField = AccessTools.Field(typeof(ScenPart_PursuingMechanoids), "onStartMap");
            public static void Postfix(ScenPart_PursuingMechanoids __instance, Map map)
            {
                if (ModSet._autoClearOnStartMap)
                {
                    ModSet._onStartMap = false;
                    onStartMapField.SetValue(__instance, false);
                    Log.Message($"[OMRA] ScenPart_PursuingMechanoids_MapRemoved_Patch: onStartMap set to false for map {map}.");
                }
            }
        }

        [HarmonyPatch(typeof(ScenPart_PursuingMechanoids))]
        [HarmonyPatch("Tick")]
        [HarmonyPatch(new Type[] { })]
        public static class ScenPart_PursuingMechanoids_Tick_Patch
        {
            public static FieldInfo mapWarningTimersField = AccessTools.Field(typeof(ScenPart_PursuingMechanoids), "mapWarningTimers");
            public static FieldInfo mapRaidTimersField = AccessTools.Field(typeof(ScenPart_PursuingMechanoids), "mapRaidTimers");
            public static FieldInfo questCompletedField = AccessTools.Field(typeof(ScenPart_PursuingMechanoids), "questCompleted");
            public static FieldInfo onStartMapField = AccessTools.Field(typeof(ScenPart_PursuingMechanoids), "onStartMap");
            public static bool Prefix(ScenPart_PursuingMechanoids __instance)
            {
                if (ModSet._applyChangesAtNextTick) 
                {
                    onStartMapField.SetValue(__instance, ModSet._onStartMap);
                    Log.Message($"[OMRA] ScenPart_PursuingMechanoids_Tick_Patch: onStartMap set to {ModSet._onStartMap} at tick {Find.TickManager.TicksGame}.");

                    questCompletedField.SetValue(__instance, !ModSet._enableMechanoidRaids);
                    Log.Message($"[OMRA] ScenPart_PursuingMechanoids_Tick_Patch: questCompleted set to {!ModSet._enableMechanoidRaids} at tick {Find.TickManager.TicksGame}.");

                    if (ModSet._modifyMapWarningTimers)
                    {
                        Dictionary<Map, int> temp;
                        temp = (Dictionary<Map, int>)mapWarningTimersField.GetValue(__instance);
                        var keys = temp.Keys.ToList();
                        foreach (Map map in keys)
                        {
                            temp[map] = ModSet._mapStartTimers + ModSet._mapWarningTimers;
                            Log.Message($"[OMRA] ScenPart_PursuingMechanoids_Tick_Patch: mapWarningTimers set to {ModSet._mapWarningTimers} for map {map} at tick {Find.TickManager.TicksGame}.");
                        }
                    }

                    if (ModSet._modifyMapRaidTimers)
                    {
                        Dictionary<Map, int> temp;
                        temp = (Dictionary<Map, int>)mapRaidTimersField.GetValue(__instance);
                        var keys = temp.Keys.ToList();
                        foreach (Map map in keys)
                        {
                            temp[map] = ModSet._mapStartTimers + ModSet._mapRaidTimers;
                            Log.Message($"[OMRA] ScenPart_PursuingMechanoids_Tick_Patch: mapRaidTimers set to {ModSet._mapRaidTimers} for map {map} at tick {Find.TickManager.TicksGame}.");
                        }                        
                    }
                    ModSet._applyChangesAtNextTick = false;
                    Log.Message($"[OMRA] ScenPart_PursuingMechanoids_Tick_Patch: Changes applied at tick {Find.TickManager.TicksGame}.");
                }
                return true;
            }

            public static void Postfix(ScenPart_PursuingMechanoids __instance)
            {
                if (ModSet._modifyMapRaidTimers) 
                {
                    Dictionary<Map, int> temp;
                    temp = (Dictionary<Map, int>)mapRaidTimersField.GetValue(__instance);
                    foreach (Map map in temp.Keys)
                    {
                        if (Find.TickManager.TicksGame >= temp[map] && (Find.TickManager.TicksGame - temp[map]) % ModSet._raidIntervalTimer == 0)
                        {
                            Log.Message($"[OMRA] ScenPart_PursuingMechanoids_Tick_Patch: Attempting to trigger raid on map {map} at tick {Find.TickManager.TicksGame}.");

                            if (ModSet._modifyFireRaid)
                            {
                                IncidentParms incidentParms = new IncidentParms();
                                incidentParms.forced = true;
                                incidentParms.target = map;
                                incidentParms.points = Mathf.Max(ModSet._threatPoints, StorytellerUtility.DefaultThreatPointsNow(map) * ModSet._threatMultiplier);
                                incidentParms.faction = Faction.OfMechanoids;
                                incidentParms.raidArrivalMode = PawnsArrivalModeDefOf.RandomDrop;
                                incidentParms.raidStrategy = RaidStrategyDefOf.ImmediateAttack;
                                IncidentDefOf.RaidEnemy.Worker.TryExecute(incidentParms);

                                Log.Message($"[OMRA] ScenPart_PursuingMechanoids_Tick_Patch: Fire raid executed on map {map} with threat points {ModSet._threatPoints} and multiplier {ModSet._threatMultiplier} at tick {Find.TickManager.TicksGame}.");

                            }
                            else
                            {
                                IncidentParms incidentParms = new IncidentParms();
                                incidentParms.forced = true;
                                incidentParms.target = map;
                                incidentParms.points = Mathf.Max(5000f, StorytellerUtility.DefaultThreatPointsNow(map) * 1.5f);
                                incidentParms.faction = Faction.OfMechanoids;
                                incidentParms.raidArrivalMode = PawnsArrivalModeDefOf.RandomDrop;
                                incidentParms.raidStrategy = RaidStrategyDefOf.ImmediateAttack;
                                IncidentDefOf.RaidEnemy.Worker.TryExecute(incidentParms);

                                Log.Message($"[OMRA] ScenPart_PursuingMechanoids_Tick_Patch: Default raid executed on map {map} with default threat points at tick {Find.TickManager.TicksGame}.");

                            }


                        }
                    }
                }
            }


        }

        [HarmonyPatch(typeof(ScenPart_PursuingMechanoids))]
        [HarmonyPatch("Notify_QuestCompleted")]
        [HarmonyPatch(new Type[] { })]
        public static class ScenPart_PursuingMechanoids_Notify_QuestCompleted_Patch
        {
            public static void Postfix(ScenPart_PursuingMechanoids __instance)
            {
                ModSet._enableMechanoidRaids = false;
                
            }
        }

        [HarmonyPatch(typeof(ScenPart_PursuingMechanoids))]
        [HarmonyPatch("FireRaid")]
        [HarmonyPatch(new Type[] { typeof(Map) })]
        public static class ScenPart_PursuingMechanoids_FireRaid_Patch
        {
            public static bool Prefix(ScenPart_PursuingMechanoids __instance, Map map)
            {
                if (ModSet._modifyFireRaid) 
                {
                    IncidentParms incidentParms = new IncidentParms();
                    incidentParms.forced = true;
                    incidentParms.target = map;
                    incidentParms.points = Mathf.Max(ModSet._threatPoints, StorytellerUtility.DefaultThreatPointsNow(map) * ModSet._threatMultiplier); 
                    incidentParms.faction = Faction.OfMechanoids;
                    incidentParms.raidArrivalMode = PawnsArrivalModeDefOf.RandomDrop;
                    incidentParms.raidStrategy = RaidStrategyDefOf.ImmediateAttack;
                    IncidentDefOf.RaidEnemy.Worker.TryExecute(incidentParms);

                    Log.Message($"[OMRA] ScenPart_PursuingMechanoids_FireRaid_Patch: Fire raid executed on map {map} with threat points {ModSet._threatPoints} and multiplier {ModSet._threatMultiplier}.");

                    return false;
                }
                return true;
            }
        }

    }

    public class ModSet : ModSettings
    {
        #region param
        //save
        public static bool _onStartMap;
        public static bool _modifyMapWarningTimers;
        public static bool _modifyMapRaidTimers;
        public static bool _autoClearOnStartMap;
        public static bool _enableMechanoidRaids;
        public static bool _modifyFireRaid;
        public static bool _modifyRaidIntervalTimer;

        public static int _mapWarningTimers;
        public static int _mapRaidTimers;
        public static float _threatPoints;
        public static float _threatMultiplier;
        public static int _raidIntervalTimer;
        //not save
        public static int _mapStartTimers;
        public static bool _applyChangesAtNextTick = false;
        #endregion

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref ModSet._onStartMap, "OMRA_onStartMap", true);
            Scribe_Values.Look(ref ModSet._modifyMapWarningTimers, "OMRA_modifyMapWarningTimers", false);
            Scribe_Values.Look(ref ModSet._modifyMapRaidTimers, "OMRA_modifyMapRaidTimers", false);
            Scribe_Values.Look(ref ModSet._autoClearOnStartMap, "OMRA_autoClearOnStartMap", true);
            Scribe_Values.Look(ref ModSet._enableMechanoidRaids, "OMRA_enableMechanoidRaids", true);
            Scribe_Values.Look(ref ModSet._modifyFireRaid, "OMRA_modifyFireRaid", false);
            Scribe_Values.Look(ref ModSet._modifyRaidIntervalTimer, "OMRA_modifyRaidIntervalTimer", false);
            Scribe_Values.Look(ref ModSet._mapWarningTimers, "OMRA_mapWarningTimers", 0);
            Scribe_Values.Look(ref ModSet._mapRaidTimers, "OMRA_mapRaidTimers", 0);
            Scribe_Values.Look(ref ModSet._threatPoints, "OMRA__threatPoints", 5000);
            Scribe_Values.Look(ref ModSet._threatMultiplier, "OMRA__threatMultiplier", 1.5f);
            Scribe_Values.Look(ref ModSet._raidIntervalTimer, "OMRA__raidIntervalTimer", 30000);
        }

        public void InitData()
        {
            _onStartMap = true;
            _modifyMapWarningTimers = false;
            _modifyMapRaidTimers = false;
            _autoClearOnStartMap = true;
            _enableMechanoidRaids = true;
            _modifyFireRaid = false;
            _modifyRaidIntervalTimer = false;
            _mapWarningTimers = 0;
            _mapRaidTimers = 0;
            _threatPoints = 5000;
            _threatMultiplier = 1.5f;
            _raidIntervalTimer = 30000;

        }
    }

    public class ModShow : Mod
    {
        public ModSet modSet;
        public static ModShow Instance { get; set; }
        public ModShow(ModContentPack content) : base(content)
        {
            modSet = GetSettings<ModSet>();
            Instance = this;
        }
        public override string SettingsCategory()
        {
            string category = "category".Translate();
            return category;
        }
        public override void DoSettingsWindowContents(Rect inRect)
        {
            Listing_Standard listingStandard = new Listing_Standard();
            listingStandard.Begin(inRect);
            listingStandard.GapLine();

            string MechanoidRaidAdjustmentSettings = "Settings".Translate();
            listingStandard.Label(MechanoidRaidAdjustmentSettings);
            listingStandard.GapLine();

            string onStartMap_ = "onStartMap".Translate();
            string onStartMapTooltip = "onStartMapTooltip".Translate();
            listingStandard.CheckboxLabeled(onStartMap_, ref ModSet._onStartMap, onStartMapTooltip);
            listingStandard.GapLine();

            string autoClearOnStartMap = "autoClearOnStartMap".Translate();
            string autoClearOnStartMapTooltip = "autoClearOnStartMapTooltip".Translate();
            listingStandard.CheckboxLabeled(autoClearOnStartMap, ref ModSet._autoClearOnStartMap, autoClearOnStartMapTooltip);
            listingStandard.GapLine();

            string enableMechanoidRaids = "enableMechanoidRaids".Translate();
            string enableMechanoidRaidsTooltip = "enableMechanoidRaidsTooltip".Translate();
            listingStandard.CheckboxLabeled(enableMechanoidRaids, ref ModSet._enableMechanoidRaids, enableMechanoidRaidsTooltip);
            listingStandard.GapLine();

            string modifyMapWarningTimers = "modifyMapWarningTimers".Translate();
            string modifyMapWarningTimersTooltip = "modifyMapWarningTimersTooltip".Translate();
            string modifyMapWarningInPut = "modifyMapWarningInPut".Translate();
            string mwt = ModSet._mapWarningTimers.ToString();
            listingStandard.CheckboxLabeled(modifyMapWarningTimers, ref ModSet._modifyMapWarningTimers, modifyMapWarningTimersTooltip);
            listingStandard.TextFieldNumericLabeled(modifyMapWarningInPut, ref ModSet._mapWarningTimers, ref mwt, 0, 960000);
            listingStandard.GapLine();

            string modifyMapRaidTimers = "modifyMapRaidTimers".Translate();
            string modifyMapRaidTimersTooltip = "modifyMapRaidTimersTooltip".Translate();
            string modifyMapRaidInPut = "modifyMapRaidInPut".Translate();
            string mrt = ModSet._mapRaidTimers.ToString();
            listingStandard.CheckboxLabeled(modifyMapRaidTimers, ref ModSet._modifyMapRaidTimers, modifyMapRaidTimersTooltip);
            listingStandard.TextFieldNumericLabeled(modifyMapRaidInPut, ref ModSet._mapRaidTimers, ref mrt, 0, 2100000);
            listingStandard.GapLine();

            string modifyFireRaid = "modifyFireRaid".Translate();
            string modifyFireRaidTooltip = "modifyFireRaidTooltip".Translate();
            listingStandard.CheckboxLabeled(modifyFireRaid, ref ModSet._modifyFireRaid, modifyFireRaidTooltip);

            string modifyFireRaidThreatPoints = "modifyFireRaidThreatPoints".Translate();
            string mftp = ModSet._threatPoints.ToString();
            listingStandard.TextFieldNumericLabeled(modifyFireRaidThreatPoints, ref ModSet._threatPoints, ref mftp, 0, 1000000000);

            string modifyFireRaidThreatMultiplier = "modifyFireRaidThreatMultiplier".Translate();
            string mftm = ModSet._threatMultiplier.ToString();
            listingStandard.TextFieldNumericLabeled(modifyFireRaidThreatMultiplier, ref ModSet._threatMultiplier, ref mftm, 0.1f, 1000000000);
            listingStandard.GapLine();

            string modifyRaidIntervalTimer = "modifyRaidIntervalTimer".Translate();
            string modifyRaidIntervalTimerTooltip = "modifyRaidIntervalTimerTooltip".Translate();
            string modifyRaidIntervalInPut = "modifyRaidIntervalInPut".Translate();
            string mrit = ModSet._raidIntervalTimer.ToString();
            listingStandard.CheckboxLabeled(modifyRaidIntervalTimer, ref ModSet._modifyRaidIntervalTimer, modifyRaidIntervalTimerTooltip);
            listingStandard.TextFieldNumericLabeled(modifyRaidIntervalInPut, ref ModSet._raidIntervalTimer, ref mrit, 0, 960000);
            listingStandard.GapLine();

            string applyChangesAtNextTick = "applyChangesAtNextTick".Translate();
            listingStandard.Label(applyChangesAtNextTick);
            if (listingStandard.ButtonText("ApplyChanges".Translate()))
            {
                ModSet._applyChangesAtNextTick = true;
                Log.Message($"[OMRA] ApplyChanges button clicked. Changes will be applied at next tick.");
            }

            listingStandard.End();


            base.DoSettingsWindowContents(inRect);
        }

    }

}
