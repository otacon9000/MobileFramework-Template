using UnityEngine;
using MobileFramework.Core.Managers.Save;

namespace FlappyClone.Data
{
    /// <summary>
    /// Persistent data for the Flappy clone. This is the game's implementation of the
    /// Core save contract.
    ///
    /// WHY extend VersionedSaveData instead of implementing IGameSaveData directly:
    /// the base class already marks <see cref="SaveKey"/>/<see cref="DataVersion"/>
    /// with [JsonIgnore] (they live in the SaveSystem envelope, not in the payload)
    /// and gives MigrateFrom a no-op default. We only override what we actually need.
    ///
    /// The Core SaveSystem stores ONE file per <see cref="SaveKey"/>, protected by a
    /// SHA-256 checksum, and calls <see cref="MigrateFrom"/> automatically the first
    /// time it loads a file whose stored version is older than <see cref="DataVersion"/>.
    /// </summary>
    public sealed class FlappySaveData : VersionedSaveData
    {
        // Stable identity of the save file. NEVER change this string or players lose
        // their progress on update — bump DataVersion (below) for schema changes instead.
        public override string SaveKey => "flappy_clone";

        // Current schema version. Increment whenever the fields below change AND add
        // the matching recovery logic in MigrateFrom. This example is at version 2 to
        // demonstrate a real migration from a hypothetical version 1.
        public override int DataVersion => 2;

        public int bestScore;
        public int totalGames;
        public int lastScore;

        // Helper used only to read an old (version 1) payload. In v1 the best score
        // was stored under the field name "highScore"; v2 renamed it to "bestScore".
        // JsonUtility ships with UnityEngine, so reading the raw payload needs no
        // extra dependency in this assembly.
        [System.Serializable]
        private struct LegacyV1
        {
            public int highScore;
        }

        /// <summary>
        /// Upgrades an older payload to the current schema. Fields whose names did not
        /// change are already populated by the SaveSystem before this call; here we
        /// only recover the field that was renamed between v1 and v2.
        /// </summary>
        public override void MigrateFrom(int storedVersion, string rawJson)
        {
            if (storedVersion < 2)
            {
                var legacy = JsonUtility.FromJson<LegacyV1>(rawJson);
                if (legacy.highScore > bestScore)
                {
                    bestScore = legacy.highScore;
                }
            }
        }
    }
}
