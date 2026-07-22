using System.Collections.Generic;
using System.Linq;
using System.Windows;

using SABotSupport.ASSAStructures;

namespace SABotSupport
{
    internal static class Lv1EncountDatabase
    {
        private class Lv1EncountInfo
        {
            public uint MapID { get; private set; }
            public string MapName { get; private set; }
            public HashSet<Point> Locations { get; private set; } = new HashSet<Point>();
            public string PetName { get; private set; }
            public uint MinHPRange { get; set; }
            public uint MaxHPRange { get; set; }

            public Lv1EncountInfo(Map map, string petName, uint minHPRange, uint maxHPRange)
            {
                MapID = map.MapID;
                MapName = map.Name;
                Locations.Add(new Point(map.X, map.Y));
                PetName = petName;
                MinHPRange = minHPRange;
                MaxHPRange = maxHPRange;
            }
        }

        private static readonly Dictionary<string, Lv1EncountInfo> encounterDatabase = new();
        private static readonly Dictionary<int, int> encounterTracker = new();

        internal static void TrackEncounter(int processID, DataPackage instanceData)
        {
            //prevent encounters from being logged more than once
            if (encounterTracker.TryGetValue(processID, out int encounter) && instanceData.Encounter == encounter)
                return;

            encounterTracker[processID] = instanceData.Encounter;

            //handle case where we relogged in
            if (instanceData.Encounter == 0)
                return;

            //prevent tracking the encounter if we flew back to town
            if (instanceData.Map.MapID == 1000)
                return;

            foreach (var enemy in instanceData.Enemies.Where(x => x.Level == 1 && x.MaxHP > 0))
            {
                if (encounterDatabase.TryGetValue(instanceData.Map.Name + enemy.Name, out var encountInfo))
                {
                    if (encountInfo.MinHPRange > enemy.MaxHP)
                    {
                        encountInfo.MinHPRange = enemy.MaxHP;
                    }
                    else if (encountInfo.MaxHPRange < enemy.MaxHP)
                    {
                        encountInfo.MaxHPRange = enemy.MaxHP;
                    }

                    encountInfo.Locations.Add(new Point(instanceData.Map.X, instanceData.Map.Y));
                }
                else
                {
                    encounterDatabase.Add(instanceData.Map.Name + enemy.Name, 
                        new Lv1EncountInfo(instanceData.Map, enemy.Name, enemy.MaxHP, enemy.MaxHP));
                }
            }
        }

        internal static IEnumerable<(uint MapID, string MapName, HashSet<Point> Locations, string PetName, uint MinHPRange, uint MaxHPRange)> GetLv1EncountStats()
        {
            foreach (var pair in encounterDatabase)
            {
                yield return (pair.Value.MapID, pair.Value.MapName, pair.Value.Locations, pair.Value.PetName, pair.Value.MinHPRange, pair.Value.MaxHPRange);
            }
        }

        internal static void Clear()
        {
            encounterDatabase.Clear();
            encounterTracker.Clear();
        }
    }
}
