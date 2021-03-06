using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Harmony;
using StardewModdingAPI.Framework.Patching;
using StardewValley;
using StardewValley.Locations;

namespace StardewModdingAPI.Patches
{
    /// <summary>A Harmony patch for <see cref="SaveGame"/> which prevents some errors due to broken save data.</summary>
    /// <remarks>Patch methods must be static for Harmony to work correctly. See the Harmony documentation before renaming patch arguments.</remarks>
    [SuppressMessage("ReSharper", "InconsistentNaming", Justification = "Argument names are defined by Harmony and methods are named for clarity.")]
    [SuppressMessage("ReSharper", "IdentifierTypo", Justification = "Argument names are defined by Harmony and methods are named for clarity.")]
    internal class LoadErrorPatch : IHarmonyPatch
    {
        /*********
        ** Fields
        *********/
        /// <summary>Writes messages to the console and log file.</summary>
        private static IMonitor Monitor;


        /*********
        ** Accessors
        *********/
        /// <summary>A unique name for this patch.</summary>
        public string Name => nameof(LoadErrorPatch);


        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        /// <param name="monitor">Writes messages to the console and log file.</param>
        public LoadErrorPatch(IMonitor monitor)
        {
            LoadErrorPatch.Monitor = monitor;
        }


        /// <summary>Apply the Harmony patch.</summary>
        /// <param name="harmony">The Harmony instance.</param>
        public void Apply(HarmonyInstance harmony)
        {
            harmony.Patch(
                original: AccessTools.Method(typeof(SaveGame), nameof(SaveGame.loadDataToLocations)),
                prefix: new HarmonyMethod(this.GetType(), nameof(LoadErrorPatch.Before_SaveGame_LoadDataToLocations))
            );
        }


        /*********
        ** Private methods
        *********/
        /// <summary>The method to call instead of <see cref="SaveGame.loadDataToLocations"/>.</summary>
        /// <param name="gamelocations">The game locations being loaded.</param>
        /// <returns>Returns whether to execute the original method.</returns>
        private static bool Before_SaveGame_LoadDataToLocations(List<GameLocation> gamelocations)
        {
            // get building interiors
            var interiors =
                (
                    from location in gamelocations.OfType<BuildableGameLocation>()
                    from building in location.buildings
                    where building.indoors.Value != null
                    select building.indoors.Value
                );

            // remove custom NPCs which no longer exist
            IDictionary<string, string> data = Game1.content.Load<Dictionary<string, string>>("Data\\NPCDispositions");
            foreach (GameLocation location in gamelocations.Concat(interiors))
            {
                foreach (NPC npc in location.characters.ToArray())
                {
                    if (npc.isVillager() && !data.ContainsKey(npc.Name))
                    {
                        try
                        {
                            npc.reloadSprite(); // this won't crash for special villagers like Bouncer
                        }
                        catch
                        {
                            LoadErrorPatch.Monitor.Log($"Removed invalid villager '{npc.Name}' to avoid a crash when loading save '{Constants.SaveFolderName}'. (Did you remove a custom NPC mod?)", LogLevel.Warn);
                            location.characters.Remove(npc);
                        }
                    }
                }
            }

            return true;
        }
    }
}
