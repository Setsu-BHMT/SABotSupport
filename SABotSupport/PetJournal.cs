using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using SABotSupport.ASSAStructures;

namespace SABotSupport
{
    internal readonly struct PetStats
    {
        public readonly uint Level;
        public readonly uint HP;
        public readonly uint Attack;
        public readonly uint Armor;
        public readonly uint Speed;

        public PetStats(Pet pet)
        {
            Level = pet.Level;
            HP = pet.MaxHP;
            Attack = pet.Attack;
            Armor = pet.Armor;
            Speed = pet.Speed;
        }

        public bool IsInitialized => Level > 0;
    }

    internal static class PetJournal
    {
        private class JournalEntry
        {
            public int ExpDifference { get; private set; } = 0;

            private (uint Level, int Exp, int NextExp) expCache = new();

            private readonly string name;
            private readonly uint reincarnation;
            private readonly SortedDictionary<uint, PetStats> levelUpHistory = new();
            private readonly object _syncRoot = new();

            internal JournalEntry(Pet pet)
            {
                name = pet.Name;
                reincarnation = pet.Reincarnation;
                levelUpHistory.Add(pet.Level, new PetStats(pet));
            }

            internal PetStats LastRecord
            {
                get
                {
                    lock (_syncRoot)
                    {
                        return levelUpHistory.Values.Last();
                    }
                }
            }

            //callers expect order to be lowest level first
            internal IReadOnlyList<PetStats> LevelUpHistory
            {
                get
                {
                    lock (_syncRoot)
                    {
                        return levelUpHistory.Values.ToList().AsReadOnly();
                    }
                }
            }

            internal void Update(Pet pet)
            {
                //note: if the pet goes through reincarnation, the level will be lower, but in that case a new object should be created.
                Debug.Assert(LastRecord.Level <= pet.Level, "pet level cannot be lower than history");

                //update experience tracking
                ExpDifference = PlayerInfo.CalculateExpDifference(expCache, ExpDifference, pet.Level, pet.EXP, 140);
                expCache = new(pet.Level, pet.EXP, pet.NextEXP);

                //don't update stats if either:
                //  1. last known level is higher than current (shouldn't happen)
                //  2. No stats have changed even though level has (only possible during boosting)
                if (LastRecord.Level > pet.Level || HasSameStatsAs(pet))
                    return;

                lock (_syncRoot)
                {
                    levelUpHistory[pet.Level] = new PetStats(pet);
                }
            }

            /// <remarks>
            /// This is not definitive and only best-guess.
            /// </remarks>
            internal bool IsRecording(Pet pet, bool matchExactLevel = true)
            {
                //pet evolution will have different names
                bool isEvolution = 
                    name == "伊格斯" && pet.Name == "霍克斯" ||
                    name == "波西索斯" && pet.Name == "芙伊瑟" ||
                    name == "傑洛奧" && pet.Name == "希納迪弗" ||
                    name == "傑洛克" && pet.Name == "希納迪亞" ||
                    name == "薩尼席" && pet.Name == "薩尼比席" ||
                    name == "薩爾魯" && pet.Name == "薩爾比魯";

                if (name != pet.Name && !isEvolution || reincarnation != pet.Reincarnation)
                    return false;

                PetStats lastState = LastRecord;

                if (matchExactLevel)
                {
                    return lastState.Level == pet.Level &&
                           lastState.HP == pet.MaxHP &&
                           lastState.Attack == pet.Attack &&
                           lastState.Armor == pet.Armor &&
                           lastState.Speed == pet.Speed;
                }
                else
                    return lastState.Level <= pet.Level;
            }

            internal bool HasSameStatsAs(Pet pet)
            {
                PetStats lastState = LastRecord;

                return lastState.HP == pet.MaxHP &&
                       lastState.Attack == pet.Attack &&
                       lastState.Armor == pet.Armor &&
                       lastState.Speed == pet.Speed;
            }
        }

        private readonly struct ActiveJournal : IEquatable<ActiveJournal>
        {
            public readonly int ProcessID;
            public readonly int Slot;
            public readonly JournalEntry Entry;

            public ActiveJournal(int processID, int slot, JournalEntry entry)
            {
                ProcessID = processID;
                Slot = slot;
                Entry = entry;
            }

            public bool IsInitialized => Entry != default;

            public bool Equals(ActiveJournal other)
                => ProcessID == other.ProcessID && Slot == other.Slot;

            public override bool Equals(object obj)
                => obj is ActiveJournal e && this == e;

            public override int GetHashCode()
                => ProcessID.GetHashCode() * 17 ^ Slot.GetHashCode();

            public static bool operator ==(ActiveJournal x, ActiveJournal y)
                => x.Equals(y);

            public static bool operator !=(ActiveJournal x, ActiveJournal y)
                => !(x == y);
        }

        private const int MAX_INACTIVE_ENTRIES = 100;

        private static readonly ConcurrentDictionary<ActiveJournal, byte> activeJournals = new();    //basically concurrent hashset
        private static readonly ConcurrentDictionary<JournalEntry, int> inactiveEntries = new();     //value used as unqiue id (increasing counter)
        private static int counter = int.MinValue;

        internal static IReadOnlyList<PetStats> GetAllStatsRecords(Pet pet)
            => (pet.Level == 1) ? default : GetJournalFromCollection(activeJournals.Keys, pet).Entry?.LevelUpHistory ?? default;
        
        internal static PetStats GetFirstStatsRecord(Pet pet)
        {
            var history = GetAllStatsRecords(pet);
            if (history == default || history.First().Level == pet.Level)
                return new();
            else
                return history.First();
        }

        internal static int GetExpDifference(Pet pet)
            => GetJournalFromCollection(activeJournals.Keys, pet).Entry?.ExpDifference ?? 0;
        
        internal static void Update(int processID, DataPackage instanceData)
        {
            //create a clone so that we can manipulate the base collection
            var instanceJournals = activeJournals.Keys.Where(x => x.ProcessID == processID).ToList();

            for (int i = 0; i < 5; i++)
            {
                var pet = instanceData.Pets[i];
                var journal = instanceJournals.FirstOrDefault(x => x.Slot == i);

                if (!pet.IsPresent)
                {   //the pet slot is now empty, archive if any active journals represents this slot
                    ArchiveJournal(journal);
                }
                else if (!journal.IsInitialized)
                {   //pet is not found in active journals, so either fish it from inactive or create a new one
                    SwapIfFoundInInactiveEntriesOrCreateNew(pet, default, processID, i);
                }
                else if (SwapIfFoundInActiveJournals(pet, journal, instanceJournals))
                {   //look for the same pet in another active pet slot and swap them
                    //do nothing if we succeeded
                }
                else if (journal.Entry.IsRecording(pet, matchExactLevel: false))
                {   //the pet is the same as what is recorded in active entry, so update if necessary
                    journal.Entry.Update(pet);   //let the updater decide if it needs to update
                }
                else
                {   //pet is different from what is recorded in active entry, so archive the old one and fish from inactive or create new
                    SwapIfFoundInInactiveEntriesOrCreateNew(pet, journal, processID, i);
                }
            }

            //check max inactive entry limit
            int numToRemove = inactiveEntries.Count - MAX_INACTIVE_ENTRIES;
            if (numToRemove > 0)
            {
                numToRemove += 10;  //remove more to avoid having to do this often

                var entriesToRemove = inactiveEntries.OrderBy(x => x.Value).Take(numToRemove);

                foreach (var entry in entriesToRemove)
                {
                    inactiveEntries.TryRemove(entry.Key, out var _);
                }
            }
        }

        private static ActiveJournal GetJournalFromCollection(IEnumerable<ActiveJournal> collection, Pet pet)
            => collection.FirstOrDefault(x => x.Entry.IsRecording(pet));

        private static void ArchiveJournal(ActiveJournal orphan)
        {
            if (!orphan.IsInitialized)
                return;

            inactiveEntries.TryAdd(orphan.Entry, Interlocked.Increment(ref counter));
            activeJournals.TryRemove(orphan, out var _);
        }

        private static bool SwapIfFoundInActiveJournals(Pet pet, ActiveJournal orphanedJournal, List<ActiveJournal> journalCollection)
        {
            var staleJournal = GetJournalFromCollection(journalCollection, pet);
            if (!staleJournal.IsInitialized || staleJournal == orphanedJournal)
                return false;

            //special exception for Lv1 pets that have higher chance of having same stats
            if (staleJournal.Entry.LastRecord.Level == 1)
                return false;

            activeJournals.TryAdd(new(staleJournal.ProcessID, staleJournal.Slot, orphanedJournal.Entry), byte.MinValue);
            activeJournals.TryRemove(orphanedJournal, out var _);
            activeJournals.TryAdd(new(orphanedJournal.ProcessID, orphanedJournal.Slot, staleJournal.Entry), byte.MinValue);
            activeJournals.TryRemove(staleJournal, out var _);

            journalCollection.Remove(staleJournal);

            return true;
        }

        private static void SwapIfFoundInInactiveEntriesOrCreateNew(Pet pet, ActiveJournal orphanedJournal, int processID, int slot)
        {
            //try to find an entry in inactive entries
            //note: we don't worry about lv1 collisions because it won't matter
            var entry = inactiveEntries.Keys.FirstOrDefault(x => x.IsRecording(pet)) ?? new(pet);

            activeJournals.TryAdd(new(processID, slot, entry), byte.MinValue);
            inactiveEntries.TryRemove(entry, out var _);

            ArchiveJournal(orphanedJournal);
        }
    }
}