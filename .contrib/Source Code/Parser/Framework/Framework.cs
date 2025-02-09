﻿using NLua;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Text.RegularExpressions;
using static ATT.Export;
using static ATT.Framework;

namespace ATT
{
    /// <summary>
    /// The Framework class.
    /// </summary>
    public static partial class Framework
    {
        #region Database
        /// <summary>
        /// Whether or not Debug Mode is turned on.
        /// </summary>
        public static bool DebugMode = false;

        /// <summary>
        /// Used to represent a Lua object value which will be ignored by the Parser
        /// </summary>
        public static string IgnoredValue { get; set; }

        /// <summary>
        /// The CustomConfiguration for the Parser
        /// </summary>
        internal static CustomConfiguration Config { get; set; }

        /// <summary>
        /// All of the locales that we support.
        /// </summary>
        internal static List<string> SupportedLocales = new List<string>
        {
            "en", "es", "de", "fr", "it", "pt", "ru", "ko", "cn"
        };

        /// <summary>
        /// The very first Phase ID as indicated in _main.lua.
        /// </summary>
        public static readonly Dictionary<string, int> FIRST_EXPANSION_PHASE = new Dictionary<string, int>
        {
            // Key-Value Pair   // Classic Release Phase
            { "UNKNOWN", 0 },   // Unknown, invalid data.
            { "CLASSIC", 11 },  // PHASE_ONE
            { "TBC", 17 },      // TBC_PHASE_ONE
            { "WRATH", 30 },    // WRATH_PHASE_ONE
            { "CATA", 40 },     // CATA_PHASE_ONE
            { "MOP", 50 },      // MOP_PHASE_ONE
            { "WOD", 60 },      // WOD_PHASE_ONE
            { "TRANSMOG", 69 }, // Transmog came out sometime after WOD.
            { "LEGION", 70 },   // LEGION_PHASE_ONE
            { "BFA", 80 },      // BFA_PHASE_ONE
            { "SHADOWLANDS", 90 },      // SHADOWLANDS_PHASE_ONE
            { "DF", 100 },      // DF_PHASE_ONE
        };

        /// <summary>
        /// The very last Phase ID (inclusive) as indicated in _main.lua.
        /// </summary>
        public static readonly Dictionary<string, int> LAST_EXPANSION_PHASE = new Dictionary<string, int>
        {
            // Key-Value Pair   // Classic Release Phase
            { "UNKNOWN", 10 },   // Unknown, invalid data.
            { "CLASSIC", 16 },  // PHASE_SIX
            { "TBC", 29 },      // TBC_PHASE_SIX?
            { "WRATH", 39 },    // WRATH_PHASE_SIX?
            { "CATA", 49 },     // CATA_PHASE_SIX?
            { "MOP", 59 },      // MOP_PHASE_SIX?
            { "WOD", 69 },      // WOD_PHASE_SIX?
            { "TRANSMOG", 69 }, // Transmog came out sometime after WOD.
            { "LEGION", 79 },   // LEGION_PHASE_SIX?
            { "BFA", 89 },      // BFA_PHASE_SIX?
            { "SHADOWLANDS", 99 },      // SHADOWLANDS_PHASE_SIX?
            { "DF", 99 },      // DF_PHASE_SIX?
        };

        /// <summary>
        /// The very first patch used by each content expansion.
        /// https://wowpedia.fandom.com/wiki/Patch
        /// </summary>
        public static readonly Dictionary<string, int[]> FIRST_EXPANSION_PATCH = new Dictionary<string, int[]>
        {
            // Key-Value Pair   // Classic Release Version
            { "UNKNOWN", new int[] { 0, 0, 0, 0 } },        // Unknown, invalid data.
            { "CLASSIC", new int[] { 1, 0, 0, 22248 } },    // NOTE: Values for WoW-Classic
            { "TBC", new int[] { 2, 0, 1, 22248 } },        // NOTE: Values for TBC-Classic
            { "WRATH", new int[] { 3, 0, 2, 9056 } },       // NOTE: Values for Wrath-Classic
            { "CATA", new int[] { 4, 0, 1, 13164 } },
            { "MOP", new int[] { 5, 0, 4, 16015 } },
            { "WOD", new int[] { 6, 0, 2, 18764 } },
            { "LEGION", new int[] { 7, 0, 1, 20740 } },
            { "BFA", new int[] { 8, 0, 1, 27026 } },
            { "SHADOWLANDS", new int[] { 9, 0, 1, 36216 } },
            { "DF", new int[] { 10, 0, 0, 45335 } },
        };

        /// <summary>
        /// The very last patch (or current one for Retail) used by each content expansion.
        /// NOTE: Classic usually follows this build number.
        /// https://wowpedia.fandom.com/wiki/Patch
        /// </summary>
        public static readonly Dictionary<string, int[]> LAST_EXPANSION_PATCH = new Dictionary<string, int[]>
        {
            // Key-Value Pair   // Classic Release Version
            { "UNKNOWN", new int[] { 0, 0, 0, 22248 } },    // Unknown, invalid data.
            { "CLASSIC", new int[] { 1, 13, 7, 22248 } },   // NOTE: Values for WoW-Classic
            { "TBC", new int[] { 2, 5, 4, 22248 } },        // NOTE: Values for TBC-Classic
            { "WRATH", new int[] { 3, 4, 5, 22248 } },      // NOTE: Values for Wrath-Classic
            { "CATA", new int[] { 4, 3, 4, 15595 } },
            { "MOP", new int[] { 5, 4, 8, 18224 } },
            { "WOD", new int[] { 6, 2, 4, 21345 } },
            { "LEGION", new int[] { 7, 3, 5, 26365 } },
            { "BFA", new int[] { 8, 3, 7, 35249 } },
            { "SHADOWLANDS", new int[] { 9, 2, 7, 45745 } },
            { "DF", new int[] { 10, 1, 5, 50401 } },
        };

        public static string CURRENT_RELEASE_PHASE_NAME = "UNKNOWN";

        /// <summary>
        /// The current phase release ID of the current build type.
        /// </summary>
        public static int CURRENT_RELEASE_PHASE { get; private set; }

        /// <summary>
        /// The last patch version of the current build type. [Format: ABBCCFFFFFF]
        /// </summary>
        public static long CURRENT_RELEASE_VERSION { get; private set; }

        /// <summary>
        /// The first patch they added Transmog as something you could collect.
        /// </summary>
        private static readonly long ADDED_TRANSMOG_VERSION = FIRST_EXPANSION_PATCH["LEGION"].ConvertVersion();

        /// <summary>
        /// The maximum available Phase Identifier.
        /// </summary>
        public static long MAX_PHASE_ID = 99999999;

        // These get loaded from _main.lua now.
        public static List<object> ALLIANCE_ONLY;
        public static List<object> HORDE_ONLY;
        public static Dictionary<object, bool> ALLIANCE_ONLY_DICT;
        public static Dictionary<object, bool> HORDE_ONLY_DICT;

        /// <summary>
        /// All of the Category IDs that have been referenced somewhere in the database.
        /// </summary>
        private static Dictionary<long, bool> CATEGORIES_WITH_REFERENCES = new Dictionary<long, bool>();

        /// <summary>
        /// All of the Custom Header Constants listed by their constant name and id value.
        /// </summary>
        private static Dictionary<string, long> CUSTOM_HEADER_CONSTANTS = new Dictionary<string, long>();

        /// <summary>
        /// All of the Custom Header IDs that have been referenced somewhere in the database.
        /// </summary>
        private static Dictionary<long, bool> CUSTOM_HEADERS_WITH_REFERENCES = new Dictionary<long, bool>();

        /// <summary>
        /// All of the Filter IDs that have been referenced somewhere in the database.
        /// </summary>
        private static Dictionary<long, bool> FILTERS_WITH_REFERENCES = new Dictionary<long, bool>();

        /// <summary>
        /// All of the Flight Path IDs that have been referenced somewhere in the database.
        /// </summary>
        private static IDictionary<long, bool> FLIGHTPATHS_WITH_REFERENCES = new Dictionary<long, bool>();

        /// <summary>
        /// All of the NPC IDs that have been referenced somewhere in the database.
        /// </summary>
        private static IDictionary<long, bool> NPCS_WITH_REFERENCES = new Dictionary<long, bool>();

        /// <summary>
        /// All of the Object IDs that have been referenced somewhere in the database.
        /// </summary>
        private static IDictionary<long, bool> OBJECTS_WITH_REFERENCES = new Dictionary<long, bool>();

        /// <summary>
        /// All of the Quest IDs that have been referenced somewhere in the database.
        /// </summary>
        private static IDictionary<long, bool> QUESTS_WITH_REFERENCES = new Dictionary<long, bool>();

        /// <summary>
        /// All of the species that have been parsed sorted by Species ID.
        /// </summary>
        private static IDictionary<long, IDictionary<string, object>> SPECIES = new Dictionary<long, IDictionary<string, object>>();

        /// <summary>
        /// All of the quests that have been parsed sorted by Quest ID.
        /// </summary>
        private static IDictionary<long, IDictionary<string, object>> QUESTS = new Dictionary<long, IDictionary<string, object>>();

        /// <summary>
        /// All of the achievements that have been parsed sorted by Achievement ID.
        /// </summary>
        private static IDictionary<long, IDictionary<string, object>> ACHIEVEMENTS = new Dictionary<long, IDictionary<string, object>>();

        /// <summary>
        /// All of the names stored for each data type.
        /// </summary>
        private static IDictionary<string, Dictionary<long, object>> NAMES_BY_TYPE = new Dictionary<string, Dictionary<long, object>>();

        /// <summary>
        /// Represents the current parent group when processing the 'g' subgroup
        /// </summary>
        private static KeyValuePair<string, object>? CurrentParentGroup { get; set; }

        /// <summary>
        /// Represents the file currently being processed
        /// </summary>
        public static string CurrentFileName { get; set; }

        /// <summary>
        /// Represents the group which set the NestedDifficultyID
        /// </summary>
        private static object DifficultyRoot { get; set; }

        /// <summary>
        /// Represents the nested DifficultyID currently being processed
        /// </summary>
        private static long NestedDifficultyID { get; set; }

        /// <summary>
        /// Represents fields which can be consolidated upwards in heirarchy if all children groups have the same value for the field
        /// </summary>
        private static string[] HeirarchicalConsolidationFields = new string[]
        {
            "sourceIgnored",
        };

        /// <summary>
        /// Assign the custom headers to the Framework's internal reference.
        /// </summary>
        /// <param name="headers">The headers.</param>
        public static void AssignCustomHeaders(Dictionary<long, object> headers)
        {
            CustomHeaders = headers;
            Trace.WriteLine($"Found {headers.Count} Custom Headers...");
            foreach (var pair in headers)
            {
                if (pair.Value is IDictionary<string, object> header)
                {
                    if (header.TryGetValue("constant", out object value))
                    {
                        var constant = value.ToString();
                        CUSTOM_HEADER_CONSTANTS[constant] = pair.Key;
                        if (header.TryGetValue("export", out value) && (bool)value)
                        {
                            MarkCustomHeaderAsRequired(constant);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Mark the Custom Header as Required.
        /// This will force it to be included in the export if it exists as a constant.
        /// NOTE: Only headers with a constant defined can be explicitly marked.
        /// </summary>
        /// <param name="headerID">The header ID.</param>
        public static void MarkCustomHeaderAsRequired(long headerID)
        {
            if (headerID < 1)
            {
                CUSTOM_HEADERS_WITH_REFERENCES[headerID] = true;
            }
        }

        /// <summary>
        /// Mark the Custom Header as Required.
        /// This will force it to be included in the export if it exists as a constant.
        /// NOTE: Only headers with a constant defined can be explicitly marked.
        /// </summary>
        /// <param name="headerConstant">The header constant.</param>
        public static void MarkCustomHeaderAsRequired(string headerConstant)
        {
            if (CUSTOM_HEADER_CONSTANTS.TryGetValue(headerConstant, out long headerID))
            {
                CUSTOM_HEADERS_WITH_REFERENCES[headerID] = true;
            }
        }

        private static HashSet<string> _autoLocalizeTypes;
        private static bool AutoLocalizeType(string type)
        {
            if (_autoLocalizeTypes == null)
            {
                var types = Config["AutoLocalizeTypes"];
                if (types != null)
                {
                    _autoLocalizeTypes = new HashSet<string>((string[])Config["AutoLocalizeTypes"]);
                }
                else _autoLocalizeTypes = new HashSet<string>();
            }
            return _autoLocalizeTypes.Contains(type);
        }

        /// <summary>
        /// Represents that data will be merged into the base dictionaries.
        /// This should only be performed on the first processing pass, allowing the second processing pass to sync all Item info in nested group references
        /// </summary>
        private static bool MergeItemData => CurrentParseStage <= ParseStage.Validation;

        /// <summary>
        /// Whether the Parser is processing Merge data which is allowed to Merge certain fields to be shared among all Sources of a Thing
        /// </summary>
        public static bool ProcessingMergeData => CurrentParseStage == ParseStage.RawJsonMerge || CurrentParseStage == ParseStage.ConditionalData;

        private static ParseStage _stage;
        /// <summary>
        /// Represents the current Stage of Parsing. Certain data is not fully populated or accurate at certain Stages, so this can be used to ensure
        /// operations are performed at the correct Stage
        /// </summary>
        public static ParseStage CurrentParseStage
        {
            get
            {
                return _stage;
            }
            set
            {
                if (value <= _stage)
                    throw new InvalidOperationException($"Do not regress or stagnate in ParseStage tracking: {_stage} => {value}");

                _stage = value;
                Log(_stage.ToString() + "...");
            }
        }

        /// <summary>
        /// Represents whether we are currently processing the main Achievements Category
        /// </summary>
        private static bool ProcessingAchievementCategory { get; set; }

        /// <summary>
        /// Represents the valid values for the 'classes' / 'c' field of an object
        /// </summary>
        internal static readonly HashSet<long> Valid_Classes = new HashSet<long>() { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13 };

        /// <summary>
        /// A Dictionary of key-ID types and how many times each value of key-type has been referenced in the final DB
        /// </summary>
        public static Dictionary<string, Dictionary<decimal, int>> TypeUseCounts { get; } = new Dictionary<string, Dictionary<decimal, int>>()
        {
            { "questID", new Dictionary<decimal, int>() },
        };

        /// <summary>
        /// A Dictionary of key-ID types and how many times each value of key-type has been referenced in the final DB
        /// </summary>
        public static Dictionary<string, HashSet<decimal>> OutputSets { get; } = new Dictionary<string, HashSet<decimal>>();

        /// <summary>
        /// A Dictionary of key-ID types and the respective objects which contain the specified key which will be captured and output during Debug runs</para>
        /// NOTE: Each key name/value may contain multiple sets of data due to duplication of individual listings
        /// </summary>
        public static Dictionary<string, SortedDictionary<decimal, List<IDictionary<string, object>>>> DebugDBs { get; }
                = new Dictionary<string, SortedDictionary<decimal, List<IDictionary<string, object>>>>();

        /// <summary>
        /// A collection of named format strings for logging messages
        /// </summary>
        public static Dictionary<string, string> LogFormats = new Dictionary<string, string>
        {
            { "ItemRecipeFormat", "Add to ItemRecipes.lua: i({0}, {1}); -- {2}" },
        };

        /// <summary>
        /// All of the categories that have been loaded into the database.
        /// </summary>
        internal static Dictionary<long, Dictionary<string, object>> CategoryDB { get; private set; } = new Dictionary<long, Dictionary<string, object>>();

        /// <summary>
        /// Populated with a set of parsed Item Dictionary datas which will conditionally be merged following the DataValidation phase. This
        /// is useful to be able to define specific relationships on specific Items (Mount/Pet/etc.) and only incorporate the relationship if
        /// the Item is Sourced elsewhere for the specific ATT Build
        /// </summary>
        internal static List<IDictionary<string, object>> ConditionalItemData { get; } = new List<IDictionary<string, object>>();

        /// <summary>
        /// The CustomHeaders table from main.lua that is used to generate custom headers.
        /// </summary>
        internal static Dictionary<long, object> CustomHeaders { get; private set; }

        /// <summary>
        /// All of the filters that have been loaded into the database.
        /// NOTE: This is exclusively used for text localizations.
        /// </summary>
        internal static Dictionary<long, Dictionary<string, object>> FilterDB { get; private set; } = new Dictionary<long, Dictionary<string, object>>();

        /// <summary>
        /// All of the flight paths that have been loaded into the database.
        /// NOTE: This is exclusively used for text localizations.
        /// </summary>
        internal static Dictionary<long, Dictionary<string, object>> FlightPathDB { get; private set; } = new Dictionary<long, Dictionary<string, object>>();

        /// <summary>
        /// All of the objects that have been loaded into the database.
        /// </summary>
        internal static Dictionary<long, Dictionary<string, object>> ObjectDB { get; private set; } = new Dictionary<long, Dictionary<string, object>>();

        /// <summary>
        /// Contains two Keys for sets of field names relating to a 'trackable' nature within ATT
        /// Provided: fields whose data allows for in-game tracking capability
        /// Required: fields whose data only makes sense if the data allows in-game tracking
        /// </summary>
        internal static Dictionary<string, HashSet<string>> TrackableFields { get; } = new Dictionary<string, HashSet<string>>
        {
            { "Provided", new HashSet<string>
            {
                "achID",
                "azeriteEssenceID",
                "conduitID",
                "difficultyID",
                "factionID",
                "flightPathID",
                "followerID",
                "instanceID",
                "heirloomUnlockID",
                "heirloomLevelID",
                "questID",
                "questIDA",
                "questIDH",
                "runeforgePowerID",
                "spellID",
                "titleID",
            } },
            { "Required", new HashSet<string>
            {
                "isDaily",
                "isWeekly",
                "isMonthly",
                "isYearly",
                "repeatable"
            } }
        };

        public static bool HasConfig()
        {
            return Config != null;
        }

        /// <summary>
        /// Allows the optional Parser Config file to overwrite some built-in values for non-compile required manipulation of the Parser
        /// </summary>
        public static void InitConfigSettings(string filepath, bool replaceConfig=false)
        {
            if (Config == null || replaceConfig)
            {
                Log($"Using config: {filepath}");
                Config = new CustomConfiguration(filepath);
            }
            else
            {
                Log($"Added config: {filepath}");
                Config.ApplyFile(filepath);
            }
        }

        /// <summary>
        /// After multiple calls to InitConfigSettings have been completed, this method is used to apply the config settings into the Parser
        /// </summary>
        public static void ApplyConfigSettings()
        {
            CURRENT_RELEASE_PHASE_NAME = Config["DataPhase"] ?? "UNKNOWN";
            if(CURRENT_RELEASE_PHASE_NAME == "UNKNOWN")
            {
                Console.Write("CURRENT_RELEASE_PHASE_NAME is UNKNOWN. Please make sure to assign a data phase in your config file.");
                Console.ReadLine();
                throw new ArgumentNullException("DataPhase");
            }
            int[] configPatch = Config["DataPatch"];
            if (configPatch != null)
            {
                LAST_EXPANSION_PATCH[CURRENT_RELEASE_PHASE_NAME] = configPatch;
            }
            CURRENT_RELEASE_PHASE = FIRST_EXPANSION_PHASE[CURRENT_RELEASE_PHASE_NAME];
            CURRENT_RELEASE_VERSION = LAST_EXPANSION_PATCH[CURRENT_RELEASE_PHASE_NAME].ConvertVersion();
            if (CURRENT_RELEASE_VERSION < FIRST_EXPANSION_PATCH["LEGION"].ConvertVersion())
            {
                if (CURRENT_RELEASE_VERSION >= FIRST_EXPANSION_PATCH["WRATH"].ConvertVersion())
                {
                    ObjectHarvester.GameFlavors.Insert(0, "wotlk");
                }
                else if (CURRENT_RELEASE_VERSION >= FIRST_EXPANSION_PATCH["TBC"].ConvertVersion())
                {
                    ObjectHarvester.GameFlavors.Insert(0, "tbc");
                }
                else
                {
                    ObjectHarvester.GameFlavors.Insert(0, "classic");
                }
            }
            if (Program.PreProcessorTags.ContainsKey("PTR"))
            {
                ObjectHarvester.GameFlavors.Insert(0, "ptr");
            }
            if (Program.PreProcessorTags.ContainsKey("ANYCLASSIC"))
            {
                MAX_PHASE_ID = LAST_EXPANSION_PHASE[CURRENT_RELEASE_PHASE_NAME];
            }
            string[] configUseCounts = Config["TrackUseCounts"];
            if (configUseCounts != null)
            {
                foreach (string type in configUseCounts)
                {
                    TypeUseCounts[type] = new Dictionary<decimal, int>();
                }
            }
            HeirarchicalConsolidationFields = Config["HeirarchicalConsolidationFields"] ?? HeirarchicalConsolidationFields;
            string[] configDebugDBs = Config["DebugDB"];
            if (configDebugDBs != null)
            {
                foreach (string key in configDebugDBs)
                {
                    DebugDBs[key] = new SortedDictionary<decimal, List<IDictionary<string, object>>>();
                }
            }
            ImportConfiguredObjectTypes(Config["ObjectTypes"]);
        }

        private static void ImportConfiguredObjectTypes(CustomConfigurationNode objectTypesConfig)
        {
            if (objectTypesConfig?.CanEnumerate ?? false)
            {
                foreach (CustomConfigurationNode objectConfig in objectTypesConfig)
                {
                    ObjectData.Insert(objectConfig["objectType"], objectConfig["shortcut"], "_." + objectConfig["function"], objectConfig["convertedKey"], objectConfig["ignoredFields"]);
                }
            }
        }

        /// <summary>
        /// Merge the data into the database.
        /// </summary>
        /// <param name="listing">The listing.</param>
        public static void Merge(List<object> listing)
        {
            foreach (var o in listing)
            {
                if (o is IDictionary<string, object> entry)
                {
                    Items.Merge(entry);
                }
            }
        }

        /// <summary>
        /// Merge the data into the database.
        /// </summary>
        /// <param name="data">The data.</param>
        public static void Merge(IDictionary<string, object> data)
        {
            // Make use of this data and attempt to load it into the database.
            // First check to see if the JSON data is a container for a specific type of object.

            // Are we dealing with an Items Database section?
            if (data.TryGetValue("items", out List<object> listing))
            {
                Merge(listing);
            }

            // Are we dealing with a Mounts Database section?
            if (data.TryGetValue("mounts", out listing))
            {
                Merge(listing);
            }

            // Are we dealing with a Quests Database section?
            if (data.TryGetValue("questDB", out IDictionary<string, object> questDB))
            {
                foreach (var pair in questDB)
                {
                    if (pair.Value is IDictionary<string, object> dict)
                    {
                        long questID = Convert.ToInt64(pair.Key);
                        if (!QUESTS.TryGetValue(questID, out IDictionary<string, object> quest))
                        {
                            QUESTS[questID] = quest = new Dictionary<string, object>();
                        }
                        foreach (var key in dict)
                        {
                            if (key.Key == "text")
                            {
                                quest["_text"] = key.Value;
                            }
                            else
                            {
                                quest[key.Key] = key.Value;
                            }
                        }
                    }
                }
            }

            // Are we dealing with an API Quests Database section?
            if (data.TryGetValue("quests", out List<object> quests))
            {
                foreach (var quest in quests)
                {
                    if (quest is IDictionary<string, object> dict)
                    {
                        if (dict.TryGetValue("questID", out long questID))
                        {
                            if (!QUESTS.TryGetValue(questID, out IDictionary<string, object> cachedQuest))
                            {
                                QUESTS[questID] = cachedQuest = new Dictionary<string, object>();
                            }

                            foreach (var key in dict)
                            {
                                cachedQuest[key.Key] = key.Value;
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Process a data container.
        /// </summary>
        /// <param name="data">The data container.</param>
        /// <param name="modID">The modID.</param>
        /// <param name="minLevel">The minimum required level.</param>
        /// <returns>Whether or not the data is valid.</returns>
        private static bool Process(IDictionary<string, object> data, long modID, long minLevel)
        {
            // Check to make sure the data is valid.
            if (data == null) return false;

            if (MergeItemData)
            {
                if (!DataValidation(data, ref modID, ref minLevel))
                    return false;
            }
            else
            {
                if (!DataConsolidation(data))
                    return false;
            }

            // If this container has an aqd or hqd, then process those objects as well.
            if (data.TryGetValue("aqd", out IDictionary<string, object> qd)) Process(qd, modID, minLevel);
            if (data.TryGetValue("hqd", out qd)) Process(qd, modID, minLevel);

            // If this container has groups, then process those groups as well.
            if (data.TryGetValue("g", out List<object> groups))
            {
                var previousParent = CurrentParentGroup;
                if (ObjectData.TryGetMostSignificantObjectType(data, out ObjectData objectData, out object objKeyValue))
                    CurrentParentGroup = new KeyValuePair<string, object>(objectData.ObjectType, objKeyValue);
                var previousDifficultyRoot = DifficultyRoot;
                var previousDifficulty = NestedDifficultyID;

                Process(groups, modID, minLevel);

                // Parent field consolidation now that groups have been processed
                if (!MergeItemData)
                    ConsolidateHeirarchicalFields(data, groups);

                CurrentParentGroup = previousParent;
                DifficultyRoot = previousDifficultyRoot;
                NestedDifficultyID = previousDifficulty;
            }

            if (!MergeItemData)
            {
                if (DebugMode)
                {
                    // Capture references to specified Debug DB keys for Debug output
                    foreach (KeyValuePair<string, SortedDictionary<decimal, List<IDictionary<string, object>>>> dbKeyDatas in DebugDBs)
                    {
                        if (data.TryGetValue(dbKeyDatas.Key, out decimal keyValue))
                        {
                            if (!dbKeyDatas.Value.TryGetValue(keyValue, out List<IDictionary<string, object>> keyValueValues))
                                dbKeyDatas.Value[keyValue] = keyValueValues = new List<IDictionary<string, object>>();

                            keyValueValues.Add(data);
                        }
                    }
                }
            }

            return true;
        }

        /// <summary>
        /// Logic on the first pass of processing all the data:<para/>
        /// * Merging into global type dictionaries<para/>
        /// * Validation of raw data<para/>
        /// </summary>
        /// <param name="data"></param>
        private static bool DataValidation(IDictionary<string, object> data, ref long modID, ref long minLevel)
        {
            // Retail has no reason to include Objective groups since the in-game Quest system does not warrant ATT including all this extra information
            // Crieve wants objectives and doesn't agree with this, but will allow it outside of Classic Builds.
            if (data.ContainsKey("objectiveID") && Program.PreProcessorTags.ContainsKey("RETAIL")) return false;

            // verify the timeline data of Merged data (can prevent keeping the data in the data container)
            if (!CheckTimeline(data))
                return false;

            // If this item has an "unobtainable" flag on it, meaning for a different phase of content.
            if (data.TryGetValue("u", out long phase))
            {
                // u <= 0 is irrelevant and can be removed. this allows for assigning a u value in source that we know will be removed later, so as
                // to not need to delete the u value from a local variable which is wrapped in a bubbleDown function.
                if (phase <= 0)
                    data.Remove("u");

                if (phase > MAX_PHASE_ID && !(phase >= 1000 && (phase < (MAX_PHASE_ID + 1) * 100)))
                {
                    data.Remove("g");
                    //Trace.Write("Excluding ");
                    //Trace.WriteLine(ToJSON(data));
                    return false;
                }
            }

            // Get the filter for this Item
            Objects.Filters filter = Objects.Filters.Ignored;
            if (data.TryGetValue("f", out long f))
            {
                if (f >= 0)
                {
                    // Parse it!
                    filter = (Objects.Filters)f;
                    FILTERS_WITH_REFERENCES[f] = true;
                }
                // remove modID from things which shouldn't have it
                if (f >= 56 && data.Remove("modID"))
                {
                    //Trace.WriteLine("Removed bad modID", data.GetString("itemID"));
                    modID = 0;
                }
                // filterID -- should be a positive value, or removed
                else if (f <= 0)
                {
                    data.Remove("f");
                }

                // special handling for explicitly-defined filterIDs (i.e. not determined by Item data, but rather directly in Source)
                switch (filter)
                {
                    case Objects.Filters.Recipe:
                        // switch any existing spellID to recipeID
                        var item = Items.GetNull(data);
                        if (item != null && item.TryGetValue("spellID", out long spellID) && item.TryGetValue("itemID", out long recipeItemID))
                        {
                            // remove the spellID if existing
                            item.Remove("spellID");
                            data.Remove("spellID");
                            // set the recipeID in the item dictionary so it will merge back in later
                            item["recipeID"] = spellID;
                        }
                        break;
                }
            }

            // Apply the inherited modID for items which do not specify their own modID
            if (modID > 0 && data.ContainsKey("itemID") && !data.ContainsKey("modID"))
            {
                //Trace.WriteLine($"Applied inherited modID {modID} over {data.GetString("modID")} for item {data.GetString("itemID")}");
                data["modID"] = modID;
            }
            else if (data.ContainsKey("ignoreBonus"))
            {
                // will be removed later
                data["modID"] = 0;
                //Trace.WriteLine("Removed ignoreBonus modID", data.GetString("itemID"));
            }
            else if (data.TryGetValue("modID", out object objModID))
            {
                modID = Convert.ToInt64(objModID);
            }

            if (data.TryGetValue("categoryID", out long categoryID)) ProcessCategoryObject(data, categoryID);
            if (data.TryGetValue("creatureID", out long creatureID))
            {
                if (data.TryGetValue("npcID", out object dupeNpcID))
                {
                    LogError($"Both CreatureID {creatureID} and NPCID {dupeNpcID}?", data);
                }
                data["npcID"] = creatureID;
                NPCS_WITH_REFERENCES[creatureID] = true;
                MarkCustomHeaderAsRequired(creatureID);
            }
            if (data.TryGetValue("npcID", out creatureID))
            {
                NPCS_WITH_REFERENCES[creatureID] = true;
                MarkCustomHeaderAsRequired(creatureID);
            }
            if (data.TryGetValue("qg", out creatureID))
            {
                NPCS_WITH_REFERENCES[creatureID] = true;
                MarkCustomHeaderAsRequired(creatureID);
            }
            if (data.TryGetValue("qgs", out List<object> qgs))
            {
                foreach (var qg in qgs)
                {
                    var id = Convert.ToInt64(qg);
                    NPCS_WITH_REFERENCES[id] = true;
                    MarkCustomHeaderAsRequired(id);
                }
            }
            if (data.TryGetValue("crs", out qgs))
            {
                foreach (var qg in qgs)
                {
                    var id = Convert.ToInt64(qg);
                    NPCS_WITH_REFERENCES[id] = true;
                    MarkCustomHeaderAsRequired(id);
                }
            }
            if (data.TryGetValue("flightPathID", out long flightPathID)) FLIGHTPATHS_WITH_REFERENCES[flightPathID] = true;
            if (data.TryGetValue("objectID", out creatureID)) ProcessObjectInstance(data, creatureID);
            if (data.TryGetValue("artifactID", out creatureID) && !data.ContainsKey("s") && Objects.ArtifactSources.TryGetValue(creatureID, out Dictionary<string, long> sources))
            {
                // off-hand artifact source
                if (data.ContainsKey("isOffHand"))
                {
                    if (sources.TryGetValue("offHand", out long s))
                        data["s"] = s;
                }
                else
                {
                    if (sources.TryGetValue("mainHand", out long s))
                        data["s"] = s;
                }
            }

            if (data.TryGetValue("providers", out object objRef) && objRef is List<object> providers)
            {
                foreach (var providerRef in providers)
                {
                    if (providerRef is List<object> provider)
                    {
                        string providerType = provider[0]?.ToString();
                        long id = Convert.ToInt64(provider[1]);
                        if (providerType == "i")
                        {
                            if (Program.PreProcessorTags.ContainsKey("ANYCLASSIC"))
                            {
                                // if the provider is an item, we want that item to be listed as having been referenced to keep it out of Unsorted
                                Items.MarkItemAsReferenced(id);
                            }
                        }
                        else if (providerType == "n")
                        {
                            NPCS_WITH_REFERENCES[id] = true;
                            MarkCustomHeaderAsRequired(id);
                        }
                        else if (providerType == "o")
                        {
                            ProcessObjectInstance(data, id);
                        }
                    }
                }
            }

            // Verify 'classes' have acceptable values
            if (data.TryGetValue("c", out List<object> classes))
            {
                try
                {
                    if (classes.Any(c => !Valid_Classes.Contains(Convert.ToInt64(c))))
                        LogError($"Invalid 'classes' value", data);
                }
                catch
                {
                    LogError($"Invalid 'classes' value", data);
                }
            }

            Objects.AssignFactionID(data);

            Validate_Encounter(data);
            Validate_Achievement(data);
            Validate_Criteria(data);
            Validate_Quest(data);
            bool cloned = Validate_DataCloning(data);
            // specifically Achievement Criteria that is cloned to another location in the addon should not be maintained where it was cloned from
            if (cloned && data.ContainsKey("criteriaID"))
                return false;

            Validate_sym(data);

            // Track the hierarchy of difficultyID
            if (data.TryGetValue("difficultyID", out long d))
            {
                DifficultyRoot = data;
                NestedDifficultyID = d;
            }

            // Throw away automatic Spell ID assignments for certain filter types.
            if (data.TryGetValue("spellID", out f))
            {
                if (f < 1)
                {
                    data.Remove("spellID");
                }
                else
                {
                    switch (filter)
                    {
                        case Objects.Filters.Recipe:
                            data["recipeID"] = f;
                            break;
                            //default:
                            //    data.Remove("spellID");
                            //    break;
                    }
                }
            }

            if (data.TryGetValue("recipeID", out f))
            {
                if (f < 1)
                {
                    data.Remove("recipeID");
                }
                else if (DebugMode)
                {
                    var cachedItem = Items.GetNull(data);
                    if (cachedItem != null)
                    {
                        cachedItem.TryGetValue("itemID", out long cachedItemID);
                        cachedItem.TryGetValue("recipeID", out long spellID);
                        cachedItem.TryGetValue("name", out string itemName);
                        LogDebugFormatted(LogFormats["ItemRecipeFormat"], cachedItemID, spellID, itemName);
                    }
                }
            }

            if (data.TryGetValue("s", out f))
            {
                if (f < 1 || CURRENT_RELEASE_VERSION < ADDED_TRANSMOG_VERSION)
                {
                    data.Remove("s");
                }
            }

            minLevel = LevelConsolidation(data, minLevel);

            Validate_cost(data);
            Validate_providers(data);
            Validate_LocationData(data);

            // TODO: this is temporary until all Item-Recipes are mapped in ItemRecipes.lua, it should only be necessary in DataConsolidation after that point
            if (data.TryGetValue("requireSkill", out long requiredSkill))
            {
                if (Objects.SKILL_ID_CONVERSION_TABLE.TryGetValue(requiredSkill, out long newRequiredSkill))
                {
                    data["requireSkill"] = requiredSkill = newRequiredSkill;
                }
                else
                {
                    switch (requiredSkill)
                    {
                        // https://www.wowhead.com/skill=
                        case 40:    // Rogue Poisons
                        case 149:   // Wolf Riding
                        case 150:   // Tiger Riding
                        case 762:   // Riding
                        case 849:   // Warlock
                        case 0: // Explicitly ignoring.
                            {
                                // Ignore! (and remove!)
                                data.Remove("requireSkill");
                                requiredSkill = 0;
                                break;
                            }
                        default:
                            {
                                Log($"Missing Skill ID in Conversion Table: {requiredSkill}{Environment.NewLine}{ToJSON(data)}");
                                break;
                            }
                    }
                }

                // if this data has a recipeID, cache the information
                // TODO: this is temporary until all Item-Recipes are mapped in ItemRecipes.lua
                if (data.TryGetValue("recipeID", out long recipeID))
                {
                    Items.TryGetName(data, out string recipeName);
                    Objects.AddRecipe(requiredSkill, recipeName, recipeID);
                }
            }

            // Merge all relevant Item Data into the global dictionaries after being validated
            Items.Merge(data);
            Objects.Merge(data);

            // Mark this item as having a reference since it exists in a processed container
            Items.MarkItemAsReferenced(data);

            return true;
        }

        private static void Validate_cost(IDictionary<string, object> data)
        {
            if (!data.TryGetValue("cost", out object costRef))
                return;

            if (!(costRef is List<List<object>> costSets))
                return;

            // check each cost component for valid formatting/validation on the data
            foreach (List<object> c in costSets)
            {
                if (!c[1].TryConvert(out decimal costID))
                {
                    LogError($"Non-numeric cost-quantity used: {ToJSON(c)}", data);
                    continue;
                }

                switch (c[0].ToString())
                {
                    case "i":
                        // anything that costs Mark of Honor should have pvp tag
                        if (costID == 137642)
                        {
                            data["pvp"] = true;
                        }
                        break;
                    case "c":
                        if (costID == 1602 ||   // Conquest
                            costID == 1792)     // Honor
                        {
                            data["pvp"] = true;
                        }
                        break;
                    case "g":
                        break;

                    default:
                        LogError($"Unknown 'cost' type: {c[0]}", data);
                        break;
                }
            }
        }

        private static void Consolidate_cost(IDictionary<string, object> data)
        {
            if (!data.TryGetValue("cost", out object costRef))
                return;

            if (!(costRef is List<List<object>> costSets))
                return;

            // check each cost component for valid formatting/validation on the data
            for (int i = costSets.Count - 1; i >= 0; --i)
            {
                if (!costSets[i].TryConvert(out List<object> c))
                    continue;

                if (!c[1].TryConvert(out decimal costID))
                    continue;

                switch (c[0].ToString())
                {
                    case "i":
                        var item = Items.GetNull(costID);
                        if (item == null || !Items.IsItemReferenced(costID))
                        {
                            // The item isn't Sourced in Retail version
                            // Holy... there are actually a ton of these. Will Debug Log for now until they are cleaned up...
                            LogDebug($"WARN: Non-Sourced 'cost-item' {costID}", data);
                        }
                        else if (item.TryGetValue("u", out long u) && u == 1)
                        {
                            // The item was classified as never being implemented
                            LogDebug($"INFO: Removed NYI 'cost-item' {costID}", data);
                            costSets.RemoveAt(i);
                        }

                        // Single Cost Item on a Achieve/Criteria group should be represented as a Provider instead
                        if (data.TryGetValue("achID", out long _) ||
                            data.TryGetValue("criteriaID", out long _))
                        {
                            if (!data.TryGetValue("providers", out object _) &&
                                costSets.Count == 1 &&
                                c.Count > 2 &&
                                c[2].TryConvert(out long count) &&
                                count == 1)
                            {
                                LogDebug($"WARN: 'cost' = {ToJSON(c)} should be 'provider'", data);
                            }
                        }
                        break;
                }
            }
        }

        /// <summary>
        /// Validates that 'coord(s)' and 'maps' data is valid
        /// </summary>
        private static void Validate_LocationData(IDictionary<string, object> data)
        {
            // 'coord' is converted to 'coords' already
            if (data.TryGetValue("coords", out List<object> coordsList))
            {
                // check if any coord is not 3 parameters: [ X, Y, MapID ]
                foreach (object coord in coordsList)
                {
                    if (coord is List<object> coordList && coordList.Count != 3)
                    {
                        LogError($"'coord/s' value is not fully qualified: {ToJSON(coord)}", data);
                    }
                }
            }

            // maps & coords
            if (data.TryGetValue("maps", out List<object> maps))
            {
                if (coordsList != null)
                {
                    List<object> redundant = new List<object>();
                    // check if any coord has a mapID which matches a maps mapID
                    foreach (object coord in coordsList)
                    {
                        if (coord is List<object> coordList && coordList.Count > 2)
                        {
                            var coordMapID = coordList[2];
                            if (maps.TrySmartContains(coordMapID, out object mapsValue))
                            {
                                if (maps.Remove(mapsValue))
                                {
                                    redundant.Add(mapsValue);
                                }
                            }
                        }
                    }

                    // remove the key itself if no mapID values remain
                    if (maps.Count == 0)
                    {
                        data.Remove("maps");
                    }

                    if (redundant.Count > 0)
                    {
                        Log($"WARN: Redundant 'maps' values removed: {ToJSON(redundant)}", data);
                    }
                }

                // single 'maps' for Achievements Sourced under 'Achievements', should be sourced in that specific map directly instead
                if (ProcessingAchievementCategory && maps.Count == 1 && data.TryGetValue("achID", out long achID))
                {
                    Log($"WARN: Single 'maps' value used within Achievement {achID}. It should be Sourced directly in the Location.", data);
                }
            }
        }

        private static void Validate_providers(IDictionary<string, object> data)
        {
            if (!data.TryGetValue("providers", out object providers))
                return;

            if (!providers.TryConvert(out List<object> providersList))
            {
                LogError("Invalid Data Format: provider(s)", data);
                return;
            }

            for (int i = providersList.Count - 1; i >= 0; i--)
            {
                var provider = providersList[i];
                if (!provider.TryConvert(out List<object> providerList) || providerList.Count != 2)
                {
                    LogError($"Invalid Data Format: provider {ToJSON(provider)}", data);
                    continue;
                }

                if (!providerList[0].TryConvert(out string pType))
                {
                    LogError($"Invalid Data Format: provider-type: {providerList[0]}", data);
                    continue;
                }

                if (!providerList[1].TryConvert(out decimal pID))
                {
                    LogError($"Invalid Data Format: provider-id {providerList[1]}", data);
                    continue;
                }
            }
        }

        private static void Consolidate_providers(IDictionary<string, object> data)
        {
            if (!data.TryGetValue("providers", out object providers))
                return;

            if (!providers.TryConvert(out List<object> providersList))
                return;

            for (int i = providersList.Count - 1; i >= 0; i--)
            {
                var provider = providersList[i];
                if (!provider.TryConvert(out List<object> providerList) || providerList.Count != 2)
                    continue;

                if (!providerList[0].TryConvert(out string pType))
                    continue;

                if (!providerList[1].TryConvert(out decimal pID))
                    continue;

                // validate that the referenced ID exists in this version of the addon
                switch (pType)
                {
                    case "i":
                        var item = Items.GetNull(pID);
                        if (!Program.PreProcessorTags.ContainsKey("ANYCLASSIC"))
                        {
                            // Crieve doesn't want this. Sometimes the only valid source is the provider, which is fine for quest items.
                            if (item == null || !Items.IsItemReferenced(pID))
                            {
                                // The item isn't Sourced in Retail version
                                // Holy... there are actually a ton of these. Will Debug Log for now until they are cleaned up...
                                LogDebug($"WARN: Non-Sourced 'provider-item' {pID}", data);
                            }
                            else if (item.TryGetValue("u", out long u) && u == 1)
                            {
                                // The item was classified as never being implemented
                                LogDebug($"INFO: Removed NYI 'provider-item' {pID}", data);
                                providersList.RemoveAt(i);
                            }
                        }
                        break;
                    case "n":
                    case "o":
                        // maybe something for NPCs, Objects... ?
                        break;
                    default:
                        LogError($"Invalid Data Value: provider-type {pType}", data);
                        break;
                }
            }
        }

        private static void Validate_sym(IDictionary<string, object> data)
        {
            if (!data.TryGetValue("sym", out List<object> symObject))
                return;

            string previousType = null;
            // some logic to check for duplicate 'select' commands of the same type
            foreach (object cmdObj in symObject)
            {
                if (!cmdObj.TryConvert(out List<object> command))
                {
                    LogError($"Incorrect 'sym' command structure encountered: {ToJSON(cmdObj)}", data);
                    break;
                }

                // check various commands
                if (command.Count > 0 && command[0].TryConvert(out string commandName))
                {
                    if (commandName == "select")
                    {
                        if (command.Count > 1 && command[1].TryConvert(out string commandType))
                        {
                            if (previousType == commandType)
                            {
                                LogDebug($"WARN: 'sym-select' can be cleaned up", data);
                                break;
                            }
                            else
                            {
                                List<object> selections = command.Skip(2).ToList();
                                List<decimal> selectionValues = selections.AsTypedEnumerable<decimal>().ToList();

                                // verify all select values are decimals
                                if (selections.Count != selectionValues.Count)
                                {
                                    LogError($"'sym-select' contains non-numeric selection values", data);
                                }
                            }

                            previousType = commandType;
                        }
                    }
                    else
                    {
                        previousType = null;
                    }
                }
            }
        }

        private static void Consolidate_sourceQuests(IDictionary<string, object> data)
        {
            if (!data.TryGetValue("sourceQuests", out List<object> sourceQuests))
                return;

            foreach (var sourceQuestRef in sourceQuests)
            {
                if (!sourceQuestRef.TryConvert(out long sourceQuestID))
                {
                    LogError($"Non-number 'sourceQuests' value used: {sourceQuestRef}");
                    continue;
                }

                if (!Objects.AllQuests.TryGetValue(sourceQuestID, out IDictionary<string, object> sourceQuest))
                {
                    // Source Quest not in database
                    LogError($"Referenced Source Quest {sourceQuestID} has not been Sourced");
                    continue;
                }

                // source quest of this data is considered a breadcrumb, so note in the source quest it has a specific follow up
                if (sourceQuest.TryGetValue("isBreadcrumb", out bool isBreadcrumb) && isBreadcrumb)
                {
                    // Source Quest is a breadcrumb, add current quest into breadcrumb's next quests list
                    if (!sourceQuest.TryGetValue("nextQuests", out List<object> nextQuests))
                    {
                        sourceQuest.Add("nextQuests", nextQuests = new List<object>());
                    }

                    if (data.TryGetValue("questID", out long questID) && !nextQuests.Contains(questID))
                    {
                        nextQuests.Add(questID);
                    }
                }
            }
        }

        private static void Consolidate_altQuests(IDictionary<string, object> data)
        {
            if (!data.TryGetValue("altQuests", out List<object> altQuests))
                return;

            foreach (var altQuestRef in altQuests)
            {
                if (!altQuestRef.TryConvert(out long altQuestID))
                {
                    LogError($"Non-number 'altQuests' value used: {altQuestRef}");
                    continue;
                }

                if (!Objects.AllQuests.TryGetValue(altQuestID, out IDictionary<string, object> altQuest))
                {
                    // Source Quest not in database
                    LogDebug($"WARN: Referenced Alternate Quest {altQuestID} has not been Sourced");
                }
            }
        }

        private static void Validate_Encounter(IDictionary<string, object> data)
        {
            if (!data.TryGetValue("encounterID", out long encounterID))
                return;

            // Hash the Encounter for MergeIntos if needed
            data["_encounterHash"] = encounterID + NestedDifficultyID / 100M;

            // Clean up Encounters which only have a single npcID assigned via 'crs'
            if (!data.ContainsKey("npcID") && data.TryGetValue("crs", out List<object> crs) && crs.Count == 1 && crs[0].TryConvert(out long crID))
            {
                data["npcID"] = crID;
                data.Remove("crs");
            }

            // Warn about Encounters with no NPCID assignment
            if (!data.ContainsKey("npcID") && !data.ContainsKey("crs"))
            {
                switch (encounterID)
                {
                    // weird encounters that are one encounter but drops are organized by NPCs in the encounter
                    case 1547:  // Silithid Royalty (AQ40)
                    case 1549:  // Twin Emperors (AQ40)
                    case 1552:  // Servant's Quarters (Kara)
                        break;
                    default:
                        LogError($"Encounter {encounterID} is missing an NPC assignment! (Could lead to unassigned Achievement data)");
                        break;
                }
            }
        }

        private static void Validate_Criteria(IDictionary<string, object> data)
        {
            if (!data.TryGetValue("criteriaID", out long criteriaID))
                return;

            if (CurrentParentGroup == null)
                return;

            // due to AchievementDB using 'HQT' questIDs for some Criterias, let's just tell Parser to ignore moving them based on AchievementDB until we think of a better solution...
            if (data.ContainsKey("_noautomation"))
                return;

            var parent = CurrentParentGroup.Value;
            long achID = 0;

            // use parent group to find AchID
            if (parent.Key == "achID")
            {
                parent.Value.TryConvert(out achID);
            }

            // Grab AchievementDB info
            if (!ACHIEVEMENTS.TryGetValue(achID, out IDictionary<string, object> achInfo))
                return;

            // Look for matching Criteria data
            if (!(achInfo.TryGetValue("g", out object criteriaObject) && criteriaObject is IList<object> criteriaList))
                return;

            // Grab matching Criteria data
            var criteriaData = criteriaList.AsTypedEnumerable<IDictionary<string, object>>().FirstOrDefault(c => c.TryGetValue("criteriaID", out long criteriaInfoID) && criteriaInfoID == criteriaID);
            if (criteriaData == null)
                return;

            // Check for criteria DB data that is useful for parsing
            // SourceQuest can convert to _quests for criteria cloning
            if (criteriaData.TryGetValue("sourceQuest", out long questID))
            {
                if (data.TryGetValue("_quests", out object quests))
                {
                    LogDebug($"WARN: Remove _quests {ToJSON(quests)} from Criteria {achID}:{criteriaID}. AchievementDB contains sourceQuest: {questID}");
                }
                else
                {
                    LogDebug($"INFO: Added _quests to Criteria {achID}:{criteriaID} with sourceQuest: {questID}");
                }

                data["_quests"] = new List<long> { questID };

                // Criteria moved under a Quest should not have a cost/provider, but rather their destination should have that data
                // if (data.ContainsKey("cost") || data.ContainsKey("providers"))
                // {
                //     LogDebug($"WARN: Move cost/provider from Criteria {achID}:{criteriaID} to its SourceQuest {questID} if applicable");
                // }
                // can remove 'sourceQuests' from the criteria in Retail since it's going to be sourced under the required quest
                data.Remove("sourceQuests");
            }

            // TODO: can do this later when adding some way to verify that the criteria WAS actually moved under the NPC
            // currently it will try to move under certain NPCs which are not sourced and basically disappear
            // n-provider can convert to _npcs for criteria cloning
            //if (criteriaData.TryGetValue("provider", out object providerObject) && providerObject is IList<object> objectList)
            //{
            //    var type = objectList[0] as string;
            //    objectList[1].TryConvert(out long id);
            //    if (id > 0 && NPCS_WITH_REFERENCES[id])
            //    {
            //        if (type == "n")
            //        {
            //            if (data.TryGetValue("_npcs", out object quests))
            //            {
            //                LogDebug($"INFO: Remove _npcs from Criteria {achID}:{criteriaID}. AchievementDB contains n-provider: {id}");
            //            }
            //            else
            //            {
            //                LogDebug($"INFO: Added _npcs to Criteria {achID}:{criteriaID} with NPCID: {id}");
            //            }

            //            data["_npcs"] = new List<long> { id };
            //        }
            //    }
            //}
        }

        private static void Validate_Quest(IDictionary<string, object> data)
        {
            // Mark the quest as referenced
            if (!data.TryGetValue("questID", out long questID))
                return;

            // Merge quest entry to AllQuest collection
            Objects.MergeQuestData(data);

            // Classic-only AQD/HQD quest datas
            if (data.TryGetValue("aqd", out IDictionary<string, object> aqd))
            {
                Objects.MergeQuestData(aqd);
            }
            if (data.TryGetValue("hqd", out IDictionary<string, object> hqd))
            {
                Objects.MergeQuestData(hqd);
            }

            // Remove itself from the list of altQuests
            if (data.TryGetValue("altQuests", out List<object> altQuests) && altQuests != null && altQuests.Count > 0)
            {
                altQuests.Remove(questID);
            }

            // Convert any 'n' providers into 'qgs' for data simplicity
            if (data.TryGetValue("providers", out List<object> providers))
            {
                List<object> quest_qgs = new List<object>(providers.Count);
                for (int p = providers.Count - 1; p >= 0; p--)
                {
                    object provider = providers[p];
                    // { "n", ### }
                    if (provider is List<object> providerItems && providerItems.Count == 2 && providerItems[0].ToString() == "n")
                    {
                        quest_qgs.Add(providerItems[1]);
                        providers.RemoveAt(p);
                        //LogDebug($"Quest {questID} provider 'n', {providerItems[1]} converted to 'qgs'");
                    }
                }

                // remove 'providers' if it is now empty
                if (providers.Count == 0)
                    data.Remove("providers");

                // merge the 'qgs' back into the data if anything was converted
                if (quest_qgs.Count > 0)
                    Objects.Merge(data, "qgs", quest_qgs);
            }
        }

        private static bool Validate_DataCloning(IDictionary<string, object> data)
        {
            bool cloned = false;

            if (data.TryGetValue("_quests", out object quests))
            {
                // don't duplicate achievements in this way
                if (data.TryGetValue("achID", out long achID))
                {
                    Log($"Do not use '_quests' on Achievements ({achID}). Source within the Quest group, or use 'maps' & 'altQuests' if there are multiple related Locations / Quests.");
                }
                else
                {
                    DuplicateDataIntoGroups(data, quests, "questID");
                    cloned = true;
                }
            }
            if (data.TryGetValue("_items", out object items))
            {
                // don't duplicate achievements in this way
                if (data.TryGetValue("criteriaID", out long criteriaID))
                {
                    data.TryGetValue("achID", out long achID);
                    Log($"Do not use '_items' on Criteria ({achID}:{criteriaID}). Use 'provider' instead when an Item grants credit for an Achievement Criteria.");
                }
                else
                {
                    DuplicateDataIntoGroups(data, items, "itemID");
                    cloned = true;
                }
            }
            if (data.TryGetValue("_npcs", out object npcs))
            {
                // TODO: consolidate when creature/npc are the same... if that ever happens
                //DuplicateDataIntoGroups(data, npcs, "creatureID");
                DuplicateDataIntoGroups(data, npcs, "npcID");
                cloned = true;
            }
            if (data.TryGetValue("_objects", out object objects))
            {
                DuplicateDataIntoGroups(data, objects, "objectID");
                cloned = true;
            }
            if (data.TryGetValue("_achievements", out object achievements))
            {
                DuplicateDataIntoGroups(data, achievements, "achID");
                cloned = true;
            }
            if (data.TryGetValue("_factions", out object factions))
            {
                DuplicateDataIntoGroups(data, factions, "factionID");
                cloned = true;
            }
            if (data.TryGetValue("_encounter", out object encounterData))
            {
                var encounterListData = Objects.CompressToList<long>(encounterData);
                decimal encounterHash = Convert.ToDecimal(encounterListData[0])
                    + (encounterListData.Count > 1 ? Convert.ToDecimal(encounterListData[1]) : 0M) / 100M;
                DuplicateDataIntoGroups(data, encounterHash, "_encounterHash");
                cloned = true;
            }

            return cloned;
        }

        private static void Validate_Achievement(IDictionary<string, object> data)
        {
            // Mark the achievement as referenced
            if (!data.TryGetValue("achID", out long achID)) return;

            // Grab AchievementDB info
            ACHIEVEMENTS.TryGetValue(achID, out IDictionary<string, object> achInfo);

            // Remove itself from the list of altAchievements
            if (data.TryGetValue("altAchievements", out List<object> altAchievements) && altAchievements != null && altAchievements.Count > 0)
            {
                altAchievements.Remove(achID);
            }

            // Guild Achievements are not collectible
            if (achInfo.TryGetValue("isGuild", out bool isGuild) && isGuild)
            {
                data["collectible"] = false;
            }

            // If not processing the Main Achievement Category, then any encountered non-guild Achievements (which are not Criteria) should be duplicated into the Main Achievement Category
            if (!ProcessingAchievementCategory && !isGuild && !data.ContainsKey("criteriaID"))
            {
                if (achInfo.TryGetValue("parentCategoryID", out long achCatID))
                {
                    DuplicateDataIntoGroups(data, achCatID, "achievementCategoryID");
                    //LogDebug($"Duplicated Achievement {achID} into Achievement Category");
                }
            }
        }

        private static long GetAchievementCrtieriaUID(Tuple<long, long> achCrit)
        {
            long achID = achCrit.Item1;
            long crit = achCrit.Item2;
            if (!ACHIEVEMENTS.TryGetValue(achID, out var achData))
            {
                LogError($"Failed to find AchievementDB data for Achievement {achID}");
                return 0;
            }

            if (!achData.TryGetValue("g", out List<object> criterias))
            {
                LogError($"Failed to find AchievementDB 'criteria' for Achievement {achID}");
                return 0;
            }

            foreach (var critObj in criterias)
            {
                if (critObj is IDictionary<string, object> critDict)
                {
                    if (!critDict.TryGetValue("criteriaID", out long critVal))
                        continue;

                    if (crit == critVal && critDict.TryGetValue("criteriaUID", out long critUID) && critUID != 0)
                        return critUID;
                }
            }

            LogError($"Could not determine CriteriaUID from Achievement {achID} Criteria {crit}.");
            return 0;
        }

        private static Tuple<long, long> GetAchCritIDs(decimal headerID)
        {
            long achID = (long)headerID;

            long crit = 0;
            while ((headerID -= decimal.Truncate(headerID)) != 0)
            {
                crit *= 10;
                crit += (long)(headerID *= 10);
            }

            return new Tuple<long, long>(achID, crit);
        }

        /// <summary>
        /// Logic on the second pass of processing all the data.<para/>
        /// * Consolidation of dictionary information into sourced data
        /// </summary>
        /// <param name="data"></param>
        private static bool DataConsolidation(IDictionary<string, object> data)
        {
            // Merge all relevant dictionary info into the data
            Items.MergeInto(data);
            Objects.MergeInto(data);

            // Finally post-merge anything which is supposed to merge into this group now that it (and its children) have been fully validated
            Objects.PostProcessMergeInto(data);

            // verify the timeline data of Merged data (can prevent keeping the data in the data container)
            if (!CheckTimeline(data))
                return false;

            data.TryGetValue("g", out List<object> g);
            int subGroupCount = g?.Count ?? 0;
            // no sub-groups, remove the g field
            if (subGroupCount == 0)
            {
                data.Remove("g");
                // certain types with empty groups shouldn't be included
                if (data.ContainsKey("achievementCategoryID"))
                {
                    Log($"Sourced Achievement Category {data["achievementCategoryID"]} contained no content after Parsing");
                    return false;
                }
            }

            Consolidate_cost(data);
            Consolidate_providers(data);
            Consolidate_sourceQuests(data);
            Consolidate_altQuests(data);

            // since early 2020, the API no longer associates recipe Items with their corresponding Spell... because Blizzard hates us
            // so try to automatically associate the matching recipeID from the requiredSkill profession list to the matching item...
            TryFindRecipeID(data);
            CheckRequireSkill(data);
            CheckHeirloom(data);
            CheckTrackableFields(data);
            CheckRequiredDataRelationships(data);
            Items.DetermineSourceID(data);
            CheckObjectConversion(data);

            //VerifyListContentOrdering(data);

            // when consolidating data, check for duplicate objects (instead of when merging)
            foreach (string key in TypeUseCounts.Keys)
            {
                if (data.TryGetValue(key, out decimal id))
                {
                    IncrementTypeUseCount(key, id);
                }
                else if (key == "questID")
                {
                    if (data.TryGetValue("questIDA", out id))
                        IncrementTypeUseCount(key, id);
                    if (data.TryGetValue("questIDH", out id))
                        IncrementTypeUseCount(key, id);
                }
            }

            // only clean the name after other processing is complete
            if (data.TryGetValue("name", out string name))
            {
                // Determine the Most-Significant ID Type (itemID, questID, npcID, etc)
                if (ObjectData.TryGetMostSignificantObjectType(data, out ObjectData objectData, out object objKeyValue))
                {
                    long id = Convert.ToInt64(objKeyValue);
                    // Store the name of this object (or whatever it is) in our table.
                    if (!NAMES_BY_TYPE.TryGetValue(objectData.ObjectType, out Dictionary<long, object> names))
                    {
                        NAMES_BY_TYPE[objectData.ObjectType] = names = new Dictionary<long, object>();
                    }
                    names[id] = name;

                    // only certain types we will auto-localize, so remove the raw 'name' field
                    if (AutoLocalizeType(objectData.ObjectType))
                    {
                        data.Remove("name");
                    }
                }
            }

            // clean out any temporary 'type' fields which do not yet have a corresponding conversion in parser.config
            if (data.TryGetValue("type", out string type) && type == "TODO")
            {
                data.Remove("type");
            }


            if (data.TryGetValue("f", out long f))
            {
                FILTERS_WITH_REFERENCES[f] = true;
            }

            // clean up any Parser metadata tags
            List<string> removeKeys = new List<string>();

            foreach (KeyValuePair<string, object> dataKvp in data)
            {
                // Remove any fields which contain 'empty' lists
                if (dataKvp.Value is IEnumerable<object> list && !list.Any())
                {
                    removeKeys.Add(dataKvp.Key);
                }
            }

            foreach (string key in removeKeys)
            {
                data.Remove(key);
            }

            return true;
        }

        /// <summary>
        /// Checks the data for any required data relationships based on existing fields
        /// </summary>
        private static void CheckRequiredDataRelationships(IDictionary<string, object> data)
        {
            // Criteria groups need to know their associated Achievement
            if (data.TryGetValue("criteriaID", out decimal criteriaID))
            {
                if (!data.ContainsKey("achID") && CurrentParentGroup.Value.Key != "achID")
                {
                    LogError($"'criteriaID' {criteriaID} missing 'achID' under non-Achievement group [{CurrentParentGroup.Value.Key}:{CurrentParentGroup.Value.Value}]", data);
                }
            }

            // Explicitly-marked 'non-collectible' Headers should not be necessary and can be warned to convert to Automatic Header type
            if (data.TryGetValue("collectible", out bool collectible) && !collectible && data.ContainsKey("g"))
            {
                LogDebug($"WARN: Explicitly Non-Collectible Header defined. Convert to Automatic Header or adjust as needed", data);
            }
        }

        private static void CheckTrackableFields(IDictionary<string, object> data)
        {
            // This logic is fine, but might be intentional in some cases to have tooltips indicate 'daily' etc.
            // even when the data itself has no way to actually 'track' completion. Maybe add this at some other time

            //if (data.ContainsAnyKey(TrackableFields["Provided"]))
            //{
            //    // currently nothing to handle concerning trackable data
            //}
            //else
            //{
            //    string[] trackingRequiredKeys = data.Keys.Where(k => TrackableFields["Required"].Contains(k)).ToArray();
            //    if (trackingRequiredKeys.Any())
            //    {
            //        LogDebug($"WARN: Tracking fields {ToJSON(trackingRequiredKeys)} removed from non-tracking data:", data);
            //        foreach (string field in trackingRequiredKeys)
            //        {
            //            data.Remove(field);
            //        }
            //    }
            //}
        }

        private static void CheckObjectConversion(IDictionary<string, object> data)
        {
            if (ObjectData.TryFindObjectConversion(data, out ObjectData conversionObject, out object convertValue))
            {
                LogDebug($"INFO: Type Conversion {conversionObject.ConvertedKey}=>{conversionObject.ObjectType} ({convertValue})");
                data.Remove("type");
                data.Remove(conversionObject.ConvertedKey);
                data[conversionObject.ObjectType] = convertValue;
            }
        }

        /// <summary>
        /// Checks the data for any list-based content and attempts to order that content in a consistent way so that output remains identical for identical data
        /// </summary>
        /// <param name="data"></param>
        private static void VerifyListContentOrdering(IDictionary<string, object> data)
        {
            foreach (KeyValuePair<string, object> entry in data)
            {
                // only certain fields are agnostic to the parsed order
                switch (entry.Key)
                {
                    case "c":
                    case "specs":
                    case "races":
                    case "sourceQuests":
                    case "altAchievements":
                    case "altQuests":
                    case "customCollect":
                    case "cost":
                    case "difficulties":
                    case "maps":
                    case "qgs":
                    case "crs":
                    case "providers":
                    case "coords":
                        // is it a list of objects?
                        if (entry.Value is List<object> valList)
                        {
                            AttemptSortingGenericList(valList);
                        }
                        break;

                }
            }
        }

        private static void AttemptSortingGenericList(List<object> list)
        {
            if ((list?.Count ?? 0) < 2)
                return;

            list.Sort(delegate (object a, object b)
            {
                unchecked
                {
                    return a.GetHashCode() - b.GetHashCode();
                }
            });
        }

        private static void IncrementTypeUseCount(string key, decimal id)
        {
            Dictionary<decimal, int> idCounts = TypeUseCounts[key];
            idCounts.TryGetValue(id, out int count);
            count += 1;
            idCounts[id] = count;
        }

        /// <summary>
        /// Returns whether the data meets the current parser 'timeline' expectations
        /// </summary>
        private static bool CheckTimeline(IDictionary<string, object> data)
        {
            // Check to see what patch this data was made relevant for.
            if (data.TryGetValue("timeline", out object timelineRef) && timelineRef is List<object> timeline)
            {
                // 2.0.1 or older items.
                int removed = 0;
                var index = 0;
                long lastVersion = 0;
                long addedPatch = 0;
                long removedPatch = 0;
                foreach (var entry in timeline)
                {
                    var commandSplit = Convert.ToString(entry).Split(' ');
                    var version = commandSplit[1].Split('.').ConvertVersion();
                    if (version > lastVersion) lastVersion = version;
                    switch (commandSplit[0])
                    {
                        // Note: Adding command options here requires adjusting the filter Regex for 'timeline' entries during MergeStringArrayData
                        case "created":
                            {
                                if (CURRENT_RELEASE_VERSION < version) return false;    // Invalid
                                else removed = 1;
                                break;
                            }
                        case "added":
                            {
                                // If this is the first patch the thing was added.
                                if (index == 0)
                                {
                                    if (CURRENT_RELEASE_VERSION < version)
                                    {
                                        return false;    // Invalid
                                    }
                                    else removed = 0;
                                }
                                else
                                {
                                    if (CURRENT_RELEASE_VERSION >= version)
                                    {
                                        removed = 0;
                                        addedPatch = 0;
                                    }
                                    else if (removed == 4 || removed == 2 || removed == 1)
                                    {
                                        // Mark the first patch this comes back on.
                                        if (addedPatch == 0) addedPatch = version;
                                    }
                                }
                                break;
                            }
                        case "deleted":
                            {
                                if (CURRENT_RELEASE_VERSION >= version) removed = 4;
                                else
                                {
                                    // Mark the first patch this was removed on. (the upcoming patch)
                                    if (removedPatch == 0) removedPatch = version;
                                }
                                break;
                            }
                        case "removed":
                            {
                                if (CURRENT_RELEASE_VERSION >= version) removed = 2;
                                else
                                {
                                    // Mark the first patch this was removed on. (the upcoming patch)
                                    if (removedPatch == 0) removedPatch = version;
                                }
                                break;
                            }
                        case "blackmarket":
                            {
                                if (CURRENT_RELEASE_VERSION >= version) removed = 3;
                                else if (removed == 4 || removed == 2)
                                {
                                    // Mark the first patch this comes back on.
                                    if (addedPatch == 0) addedPatch = version;
                                }
                                break;
                            }
                        case "timewalking":
                            {
                                if (CURRENT_RELEASE_VERSION >= version) removed = 5;
                                else if (removed == 4 || removed == 2)
                                {
                                    // Mark the first patch this comes back on.
                                    if (addedPatch == 0) addedPatch = version;
                                }
                                break;
                            }
                    }
                    ++index;
                }

                // final removed type for the current parser patch
                switch (removed)
                {
                    // Never Implemented
                    case 1:
                        data["u"] = 1;
                        break;
                    // Black Market
                    case 3:
                        data["u"] = 9;
                        break;
                    // Timewalking re-implemented
                    case 5:
                        data["e"] = 1271;
                        break;
                    // Deleted
                    case 4:
                        data["u"] = 2;
                        break;
                    // Removed From Game
                    case 2:
                        data["u"] = 2;
                        break;
                }

                // Future Returning Item
                if (addedPatch != 0)
                {
                    data["awp"] = addedPatch.ConvertToGameVersion(); // "Added With Patch"
                }

                // Future Unobtainable
                if (removedPatch != 0)
                {
                    data["rwp"] = removedPatch.ConvertToGameVersion(); // "Removed With Patch"
                }
            }

            return true;
        }

        private static void ConsolidateHeirarchicalFields(IDictionary<string, object> parentGroup, List<object> groups)
        {
            if ((groups?.Count ?? 0) == 0) return;

            HashSet<object> fieldValues = new HashSet<object>();
            foreach (string field in HeirarchicalConsolidationFields)
            {
                foreach (object group in groups)
                {
                    if (group is IDictionary<string, object> data && data.TryGetValue(field, out object value))
                    {
                        fieldValues.Add(value);
                    }
                    else
                    {
                        fieldValues.Clear();
                        break;
                    }
                }

                // exactly 1 unique value across all groups, set it on the parent and remove it from all groups
                if (fieldValues.Count == 1)
                {
                    parentGroup[field] = fieldValues.First();

                    foreach (object group in groups)
                    {
                        if (group is IDictionary<string, object> data)
                        {
                            data.Remove(field);
                        }
                    }
                }

                fieldValues.Clear();
            }
        }

        /// <summary>
        /// Process the Category Object.
        /// </summary>
        /// <param name="data">The Category data.</param>
        /// <param name="categoryID">The Category ID.</param>
        private static void ProcessCategoryObject(IDictionary<string, object> data, long categoryID)
        {
            CATEGORIES_WITH_REFERENCES[categoryID] = true;
            if (!CategoryDB.TryGetValue(categoryID, out Dictionary<string, object> categoryData))
            {
                categoryData = new Dictionary<string, object>();
                LogWarn($"CATEGORY MISSING FOR {categoryID}!", data);
                if (data.TryGetValue("icon", out string icon))
                {
                    if (!categoryData.ContainsKey("icon"))
                    {
                        // Assign the icon and then inform the engineer.
                        categoryData["icon"] = icon.Replace("\\", "/");
                        LogWarn($"CATEGORY ICON MISSING FOR {categoryID} : ASSIGNED {icon} FROM SOURCE.");
                    }
                    else
                    {
                        LogDebug($"CATEGORY ICON ALREADY IN DATABASE FOR {categoryID}: You can probably delete it from the source file.");
                    }
                }
                else
                {
                    // Ignore that the icon is missing... for now.
                }
                if (data.TryGetValue("name", out string name))
                {
                    if (!categoryData.ContainsKey("readable"))
                    {
                        // Assign the readable and then inform the engineer.
                        categoryData["readable"] = name;
                        LogWarn($"CATEGORY READABLE MISSING FOR {categoryID} : ASSIGNED {name} FROM SOURCE.");
                    }
                    else
                    {
                        LogDebug($"CATEGORY READABLE ALREADY IN DATABASE FOR {categoryID}: You can probably delete it from the source file.");
                    }

                    if (!categoryData.ContainsKey("text"))
                    {
                        // Assign the text and then inform the engineer.
                        categoryData["text"] = new Dictionary<string, object> { { "en", name } };
                        LogWarn($"CATEGORY TEXT MISSING FOR {categoryID} : ASSIGNED {name} FROM SOURCE.");
                    }
                    else
                    {
                        LogDebug($"CATEGORY TEXT ALREADY IN DATABASE FOR {categoryID}: You can probably delete it from the source file.");
                    }
                }
                else
                {
                    // Ignore that the readable is missing... for now.
                }

                if (categoryData.Any())
                {
                    CategoryDB[categoryID] = categoryData;
                    if (!DebugMode)
                    {
                        Trace.WriteLine("Activating Debug Mode! (Press Enter to continue...)");
                        Trace.WriteLine("Update CategoryDB.lua from the Debugging folder.");
                        DebugMode = true;
                        Console.ReadLine();
                    }
                }
            }
        }

        /// <summary>
        /// Process the Object Instance.
        /// </summary>
        /// <param name="data">The Object data.</param>
        /// <param name="objectID">The Object ID.</param>
        private static void ProcessObjectInstance(IDictionary<string, object> data, long objectID)
        {
            OBJECTS_WITH_REFERENCES[objectID] = true;
            if (!ObjectDB.TryGetValue(objectID, out Dictionary<string, object> objectData))
            {
                objectData = new Dictionary<string, object>();
                LogWarn($"OBJECT MISSING FOR {objectID}!", data);
                if (data.TryGetValue("icon", out string icon))
                {
                    if (!objectData.ContainsKey("icon"))
                    {
                        // Assign the icon and then inform the engineer.
                        objectData["icon"] = icon.Replace("\\", "/");
                        LogWarn($"OBJECT ICON MISSING FOR {objectID} : ASSIGNED {icon} FROM SOURCE.");
                        if (!DebugMode)
                        {
                            Trace.WriteLine("Activating Debug Mode! (Press Enter to continue...)");
                            Trace.WriteLine("Update ObjectDB.lua from the Debugging folder.");
                            DebugMode = true;
                            Console.ReadLine();
                        }
                    }
                    else
                    {
                        LogDebug($"OBJECT ICON ALREADY IN DATABASE FOR {objectID}: You can probably delete it from the source file.");
                    }
                }
                else
                {
                    // Ignore that the icon is missing... for now.
                }
                if (data.TryGetValue("model", out object model))
                {
                    if (!objectData.ContainsKey("model"))
                    {
                        // Assign the model and then inform the engineer.
                        objectData["model"] = model;
                        LogWarn($"OBJECT MODEL MISSING FOR {objectID} : ASSIGNED {model} FROM SOURCE.");
                        if (!DebugMode)
                        {
                            Trace.WriteLine("Activating Debug Mode! (Press Enter to continue...)");
                            Trace.WriteLine("Update ObjectDB.lua from the Debugging folder.");
                            DebugMode = true;
                            Console.ReadLine();
                        }
                    }
                    else
                    {
                        LogDebug($"OBJECT MODEL ALREADY IN DATABASE FOR {objectID}: You can probably delete it from the source file.");
                    }
                }
                else
                {
                    // Ignore that the model is missing... for now.
                }
                if (data.TryGetValue("name", out string name))
                {
                    if (!objectData.ContainsKey("readable"))
                    {
                        // Assign the readable and then inform the engineer.
                        objectData["readable"] = name;
                        LogWarn($"OBJECT READABLE MISSING FOR {objectID} : ASSIGNED {name} FROM SOURCE.");
                        if (!DebugMode)
                        {
                            Trace.WriteLine("Activating Debug Mode! (Press Enter to continue...)");
                            Trace.WriteLine("Update ObjectDB.lua from the Debugging folder.");
                            DebugMode = true;
                            Console.ReadLine();
                        }
                    }
                    else
                    {
                        LogDebug($"OBJECT READABLE ALREADY IN DATABASE FOR {objectID}: You can probably delete it from the source file.");
                    }

                    if (!objectData.ContainsKey("text"))
                    {
                        // Assign the text and then inform the engineer.
                        objectData["text"] = new Dictionary<string, object> { { "en", name } };
                        LogWarn($"OBJECT TEXT MISSING FOR {objectID} : ASSIGNED {name} FROM SOURCE.");
                        if (!DebugMode)
                        {
                            Trace.WriteLine("Activating Debug Mode! (Press Enter to continue...)");
                            Trace.WriteLine("Update ObjectDB.lua from the Debugging folder.");
                            DebugMode = true;
                            Console.ReadLine();
                        }
                    }
                    else
                    {
                        LogDebug($"OBJECT TEXT ALREADY IN DATABASE FOR {objectID}: You can probably delete it from the source file.");
                    }
                }
                else
                {
                    // Ignore that the readable is missing... for now.
                }

                if(objectData.Any()) ObjectDB[objectID] = objectData;
            }
        }

        /// <summary>
        /// Attempts to find the recipe ID in the already parsed data which corresponds to this item.... by name
        /// TODO: this is temporary until all Item-Recipes are mapped in ItemRecipes.lua
        /// </summary>
        /// <param name="data"></param>
        private static void TryFindRecipeID(IDictionary<string, object> data)
        {
            // don't apply a recipeID to data which is not an item or is a Toy or has a questID (Reaves Modules... argghhh)
            if (!data.ContainsKey("itemID") || data.ContainsKey("questID"))
                return;

            // all recipes require a skill
            if (!data.TryGetValue("requireSkill", out long requiredSkill))
                return;

            // see if a matching recipe name exists for this skill, and use that recipeID
            if (Objects.FindRecipeForData(requiredSkill, data, out long recipeID))
            {
                data["recipeID"] = recipeID;
            }
            else if (recipeID == 0)
            {
                if (!data.TryGetValue("u", out long u) || (u != 1 && u != 2))
                {
                    // this can always be reported because it should always be actual, available in-game recipes which have no associated RecipeID
                    Items.TryGetName(data, out string name);
                    Log($"Failed to find RecipeID for '{name}' with data: {ToJSON(data)}");
                }
            }
        }

        /// <summary>
        /// Converts the Specific 'requireSkill' field of the data to the General 'requireSkill'
        /// </summary>
        /// <param name="data"></param>
        private static void CheckRequireSkill(IDictionary<string, object> data)
        {
            if (data.TryGetValue("requireSkill", out long requiredSkill))
            {
                if (Objects.SKILL_ID_CONVERSION_TABLE.TryGetValue(requiredSkill, out long newRequiredSkill))
                {
                    data["requireSkill"] = newRequiredSkill;
                }
                else
                {
                    switch (requiredSkill)
                    {
                        // https://www.wowhead.com/skill=
                        case 40:    // Rogue Poisons
                        case 149:   // Wolf Riding
                        case 150:   // Tiger Riding
                        case 762:   // Riding
                        case 849:   // Warlock
                        case 0: // Explicitly ignoring.
                                // Ignore! (and remove!)
                            data.Remove("requireSkill");
                            break;
                        default:
                            Log($"Missing Skill ID in Conversion Table: {requiredSkill}{Environment.NewLine}{ToJSON(data)}");
                            break;
                    }
                }
            }
        }

        /// <summary>
        /// Checks to assign an heirloomID to the data if it meets the criteria of being an heirloom
        /// </summary>
        /// <param name="data"></param>
        private static void CheckHeirloom(IDictionary<string, object> data)
        {
            if (data.TryGetValue("q", out long quality))
            {
                if (quality == 7 && data.TryGetValue("itemID", out object itemID))
                {
                    // Get the filter for this Item
                    Objects.Filters filter = Objects.Filters.Ignored;
                    if (data.TryGetValue("f", out long f))
                    {
                        if (f >= 0)
                        {
                            // Parse it!
                            filter = (Objects.Filters)f;
                        }
                    }

                    // Heirlooms quality for non-equippable Items are not really Heirlooms
                    switch (filter)
                    {
                        case Objects.Filters.Ignored:
                        case Objects.Filters.Consumable:
                        case Objects.Filters.Faction:
                        case Objects.Filters.Toy:
                        case Objects.Filters.Quest:
                        case Objects.Filters.Recipe:
                            return;
                    }

                    //LogDebugFormatted("ItemID:{0} Marked as Heirloom. Filter: {1}", itemID, filter.ToString());
                    data["heirloomID"] = itemID;
                    if (data.ContainsKey("ignoreSource"))
                    {
                        Log($"WTF WHY IS THIS HEIRLOOM {itemID} IGNORING SOURCE IDS?!");
                        Console.ReadLine();
                    }
                    else if (data.ContainsKey("ignoreBonus"))
                    {
                        Log($"WTF WHY IS THIS HEIRLOOM {itemID} IGNORING BONUS IDS?!");
                        Console.ReadLine();
                    }
                }
            }
        }

        /// <summary>
        /// Verifies the 'lvl' tag within the data confines to the already-determined minLevel for the scope of this data
        /// </summary>
        /// <param name="data"></param>
        /// <param name="minLevel"></param>
        /// <returns></returns>
        private static long LevelConsolidation(IDictionary<string, object> data, long minLevel)
        {
            // If the level of this object is less than the current minimum level, we can safely remove it.
            if (data.TryGetValue("lvl", out object lvlRef))
            {
                if (lvlRef is List<object> lvls)
                {
                    // only remove the lvl reqs if it's not a range
                    if (lvls.Count < 2)
                    {
                        var level = Convert.ToInt64(lvls[0]);
                        if (level <= minLevel) data.Remove("lvl");
                        else
                        {
                            // replace the single value list with the single value to save on memory
                            data["lvl"] = level;
                            minLevel = level;
                        }
                    }
                }
                else
                {
                    var level = Convert.ToInt64(lvlRef);
                    if (level <= minLevel) data.Remove("lvl");
                    else minLevel = level;
                }
            }

            return minLevel;
        }

        /// <summary>
        /// Returns the minimum level requirement for this data
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        private static long? GetDataMinLevel(IDictionary<string, object> data)
        {
            // If the level of this object is less than the current minimum level, we can safely remove it.
            if (data.TryGetValue("lvl", out object lvlRef))
            {
                if (lvlRef is List<object> lvls && lvls.Count > 0)
                    return Convert.ToInt64(lvls[0]);
                else if (lvlRef is int)
                    return Convert.ToInt64(lvlRef);
            }
            return null;
        }

        private static void DuplicateDataIntoGroups(IDictionary<string, object> data, object groups, string type)
        {
            // only need to setup the merge data on the first pass
            if (!MergeItemData) return;

            var groupIDs = Objects.CompressToList(groups) ?? new List<object> { groups };
            if (groupIDs != null && ObjectData.TryGetMostSignificantObjectType(data, out ObjectData objectData, out object _))
            {
                switch (objectData.ObjectType)
                {
                    case "criteriaID":
                        if (CurrentParentGroup != null)
                        {
                            var parent = CurrentParentGroup.Value;
                            // duplicate from an achID/criteriaID source
                            if (parent.Key == "achID")
                            {
                                if (!data.ContainsKey(parent.Key))
                                {
                                    data.Add(parent.Key, parent.Value);
                                }
                                else
                                {
                                    // child already contains the parent key value? weird but replace anyway
                                    data[parent.Key] = parent.Value;
                                }
                            }
                        }

                        // verify the criteria has the achieve information before duplicating
                        if (data.ContainsKey("achID"))
                        {
                            DuplicateGroupListIntoObjects(groupIDs, data, type);
                        }
                        else
                        {
                            Log("Failed to duplicate criteria object due to missing 'achID': " + ToJSON(data));
                        }
                        break;
                    case "achID":
                        // duplicated achievements should be ignored for their progress
                        IDictionary<string, object> cloned = new Dictionary<string, object>(data)
                        {
                            ["sourceIgnored"] = true
                        };
                        // verify that random other stuff contained within Achievements is not duplicated.... (like Raid Encounters...)
                        cloned.Remove("g");
                        //if (cloned.TryGetValue("g", out List<object> achGroups))
                        //{
                        //    List<object> cleanedGroups = new List<object>();
                        //    foreach (object achGroup in achGroups)
                        //    {
                        //        // something inside the achievement that contains its own things... don't duplicate that
                        //        if (achGroup is IDictionary<string, object> groupInfo && !groupInfo.ContainsKey("g"))
                        //            cleanedGroups.Add(achGroup);
                        //    }
                        //    cloned["g"] = cleanedGroups;
                        //}
                        DuplicateGroupListIntoObjects(groupIDs, cloned, type);
                        break;
                    case "objectiveID":
                        if (CurrentParentGroup != null)
                        {
                            var parent = CurrentParentGroup.Value;
                            // duplicate from an achID/criteriaID source
                            if (parent.Key == "questID")
                            {
                                if (!data.ContainsKey(parent.Key))
                                {
                                    data.Add(parent.Key, parent.Value);
                                }
                                else
                                {
                                    // child already contains the parent key value? weird but replace anyway
                                    data[parent.Key] = parent.Value;
                                }
                            }
                        }

                        // verify the criteria has the achieve information before duplicating
                        if (data.ContainsKey("questID"))
                        {
                            DuplicateGroupListIntoObjects(groupIDs, data, type);
                        }
                        else
                        {
                            Log("Failed to duplicate criteria object due to missing 'questID': " + ToJSON(data));
                        }
                        break;
                        // handle other types of duplication sources if necessary
                }
            }
        }

        /// <summary>
        /// Duplicates a list of group objects into the group lists under the associated groupIDs of a given type (quest/item/npc/...)
        /// </summary>
        /// <param name="groupIDs"></param>
        /// <param name="groupList"></param>
        /// <param name="type"></param>
        private static void DuplicateGroupListIntoObjects(List<object> groupIDs, IDictionary<string, object> data, string type)
        {
            // duplicate the data into the sourced data by type
            foreach (object dupeGroupID in groupIDs)
            {
                if (dupeGroupID.TryConvert(out decimal groupID))
                {
                    Objects.PostProcessMerge(type, groupID, data);
                }
                else
                {
                    Log($"WARN: Trying to Post-Process Merge using a non-numeric key: {dupeGroupID} for type {type}");
                }
            }
        }

        /// <summary>
        /// Process a list of data containers.
        /// </summary>
        /// <param name="list">The data container list.</param>
        /// <param name="modID">The modID.</param>
        /// <param name="minLevel">The minimum required level.</param>
        private static void Process(List<object> list, long modID, long minLevel)
        {
            // Check to make sure the data is valid.
            if (list == null) return;

            // Iterate through the list and process all of the relative data dictionaries.
            for (int i = list.Count - 1; i >= 0; --i)
            {
                if (!Process(list[i] as IDictionary<string, object>, modID, minLevel)) list.RemoveAt(i);
            }
        }

        /// <summary>
        /// Process all of the data loaded into the database.
        /// </summary>
        public static void Process()
        {
            // Go through all of the items in the database and calculate the Filter ID
            // if the Filter ID is not already assigned. (manual assignment should always override this)
            foreach (var data in Items.AllItems)
            {
                Objects.AssignFilterID(data);
                Objects.AssignFactionID(data);

                // verify that no source is included for items which should explicitly ignoreSource
                if (data.TryGetValue("ignoreSource", out bool ig) && ig)
                {
                    data.Remove("s");
                    data.Remove("modIDs");
                    data.Remove("modID");
                }
            }

            // Merge the Item Data into the Containers.
            CurrentParseStage = ParseStage.Validation;
            foreach (var container in Objects.AllContainers)
            {
                ProcessContainer(container);
            }

            // Merge the Item Data into the Containers again, this time syncing Item data into nested Item groups
            CurrentParseStage = ParseStage.ConditionalData;
            AdditionalProcessing();

            CurrentParseStage = ParseStage.Consolidation;
            foreach (var container in Objects.AllContainers)
            {
                ProcessContainer(container);
            }

            // Sort World Drops by Name
            var worldDrops = Objects.GetNull("WorldDrops");
            if (worldDrops != null) SortByName(worldDrops);

            // Build the Unsorted Container.
            CurrentParseStage = ParseStage.UnsortedGeneration;
            List<object> listing;
            long requireSkill;
            if (!Objects.AllContainers.TryGetValue("Unsorted", out List<object> unsorted))
            {
                unsorted = new List<object>();
                Objects.AllContainers["Unsorted"] = unsorted;
            }
            var tierLists = new Dictionary<int, TierList>();
            int maxTierID = 10;// LAST_EXPANSION_PATCH[CURRENT_RELEASE_PHASE_NAME][0];
            for (int tierID = 1; tierID <= maxTierID; ++tierID)
            {
                // ensure the tier group exists
                Objects.Merge(unsorted, new Dictionary<string, object>
                    {
                        { "tierID", tierID },
                        { "g", new List<object>() },
                    });
                // grab the resulting tier group 'g' list
                unsorted.FindObject("tierID", tierID).TryGetValue("g", out listing);
                // create a new TierList object tracking the specified g listing
                tierLists[tierID] = new TierList
                {
                    Groups = listing
                };
            }
            TierList tier = tierLists[1];
            var moreThanOne = tierLists.Count > 1;
            foreach (var item in Items.AllItemsWithoutReferences)
            {
                if (moreThanOne)
                {
                    var level = GetDataMinLevel(item);
                    // try to sort by itemID
                    if (item.TryGetValue("itemID", out long itemID))
                    {
                        if (itemID < 22727) tier = tierLists[1]; // Classic
                        else if (itemID < 29205) tier = tierLists[2];   // Burning Crusade
                        else if (itemID < 37649) tier = tierLists[3];   // Wrath of the Lich King
                        else if (itemID < 72019) tier = tierLists[4];   // Cataclysm
                        else if (itemID < 100855) tier = tierLists[5];   // Mists of Pandaria
                        else if (itemID < 130731) tier = tierLists[6];   // Warlords of Draenor
                        else if (itemID < 156823) tier = tierLists[7];   // Legion
                        else if (itemID < 174366) tier = tierLists[8];   // Battle For Azeroth
                        else if (itemID < 190311) tier = tierLists[9];   // Shadowlands
                        else tier = tierLists[10];   // Dragonflight
                    }
                    // sort by level into tier if not an item
                    else if (level.HasValue)
                    {
                        if (level <= 25) tier = tierLists[1]; // Classic
                        else if (level <= 27) tier = tierLists[2];   // Burning Crusade
                        else if (level <= 30) tier = tierLists[3];   // Wrath of the Lich King
                        else if (level <= 32) tier = tierLists[4];   // Cataclysm
                        else if (level <= 35) tier = tierLists[5];   // Mists of Pandaria
                        else if (level <= 40) tier = tierLists[6];   // Warlords of Draenor
                        else if (level <= 45) tier = tierLists[7];   // Legion
                        else if (level <= 50) tier = tierLists[8];   // Battle For Azeroth
                        else if (level <= 60) tier = tierLists[9];   // Shadowlands
                        else tier = tierLists[10];   // Dragonflight
                    }
                    // default tier assignment
                    else tier = tierLists[1];
                }

                if (item.TryGetValue("f", out long filterID) && filterID >= 0 && (filterID < 56 || filterID > 90))
                {
                    Objects.Filters filter = (Objects.Filters)filterID;
                    item.TryGetValue("q", out long quality);
                    switch (filter)
                    {
                        case Objects.Filters.Invalid:
                        case Objects.Filters.Ignored:
                        case Objects.Filters.Quest:
                        case Objects.Filters.Holiday:
                            // specific types we don't really care to Source unless they are actually determined to be useful
                            break;
                        case Objects.Filters.Recipe:
                            {
                                if (!tier.FilteredLists.TryGetValue(filterID, out listing))
                                {
                                    tier.Groups.Add(new Dictionary<string, object>
                                    {
                                        { "f", filterID },
                                        { "g", listing = tier.FilteredLists[filterID] = new List<object>() }
                                    });
                                }
                                if (item.TryGetValue("requireSkill", out object requireSkillRef))
                                {
                                    requireSkill = Convert.ToInt64(requireSkillRef);
                                    if (!tier.ProfessionLists.TryGetValue(requireSkill, out List<object> sublisting))
                                    {
                                        listing.Add(new Dictionary<string, object>
                                        {
                                            {"professionID", requireSkill },
                                            { "g", listing = tier.ProfessionLists[requireSkill] = new List<object>() }
                                        });
                                    }
                                    else
                                    {
                                        listing = sublisting;
                                    }
                                }
                                else
                                {
                                    if (!tier.ProfessionLists.TryGetValue(-1, out List<object> sublisting))
                                    {
                                        listing.Add(new Dictionary<string, object>
                                        {
                                            { "f", (int)Objects.Filters.Miscellaneous },
                                            { "g", listing = tier.ProfessionLists[-1] = new List<object>() }
                                        });
                                    }
                                    else
                                    {
                                        listing = sublisting;
                                    }
                                }

                                if (item.TryGetValue("itemID", out long itemID))
                                {
                                    var newItem = new Dictionary<string, object>
                                    {
                                        {"itemID", itemID },
                                    };
                                    Items.MergeInto(itemID, item, newItem);
                                    Items.DetermineSourceID(newItem);
                                    listing.Add(newItem);
                                }
                                break;
                            }
                        default:
                            {
                                switch (filter)
                                {
                                    case Objects.Filters.Consumable:
                                        // ignore white/grey consumables from going into unsorted
                                        if (quality < 2)
                                            continue;
                                        break;
                                }
                                item.Remove("spellID");
                                if (!tier.FilteredLists.TryGetValue(filterID, out listing))
                                {
                                    tier.Groups.Add(new Dictionary<string, object>
                                    {
                                        { "f", filterID },
                                        { "g", listing = tier.FilteredLists[filterID] = new List<object>() }
                                    });
                                }

                                if (item.TryGetValue("itemID", out long itemID))
                                {
                                    var newItem = new Dictionary<string, object>
                                    {
                                        {"itemID", itemID },
                                    };
                                    Items.MergeInto(itemID, item, newItem);
                                    Items.DetermineSourceID(newItem);
                                    listing.Add(newItem);
                                }
                                break;
                            }
                    }
                }
            }

            // Remove empty Data Phase tiers.
            //int dataPhase = LAST_EXPANSION_PATCH[CURRENT_RELEASE_PHASE_NAME][0];
            for (int i = unsorted.Count - 1; i >= 0; --i)
            {
                var o = unsorted[i] as IDictionary<string, object>;
                if (o == null) continue;
                if (o.TryGetValue("g", out List<object> list) && list.Count == 0)
                {
                    unsorted.RemoveAt(i);
                }
                // Data Phase doesn't include the current Unsorted Tier
                //if (dataPhase < i)
                //{
                //    unsorted.RemoveAt(i);
                //}
            }
            if (unsorted.Count == 1)
            {
                var o = unsorted[0] as IDictionary<string, object>;
                if (o != null && o.TryGetValue("g", out List<object> list))
                {
                    Objects.AllContainers["Unsorted"] = list;
                }
            }

            CurrentParseStage = ParseStage.DataIntegrityAnalysis;
            // Include in breadcrumb quests the list of next quests that may make the breadcrumb unable to complete
            //bool isBreadcrumb;
            HashSet<decimal> orphanedBreadcrumbs = new HashSet<decimal>();
            OutputSets.Add("Orphaned Breadcrumbs", orphanedBreadcrumbs);

            // check for orphaned breadcrumbs
            foreach (var pair in Objects.AllQuests)
            {
                if (pair.Value.TryGetValue("isBreadcrumb", out bool isBreadcrumb)
                    && isBreadcrumb
                    && !pair.Value.TryGetValue("nextQuests", out object nextQuests))
                {
                    // Breadcrumb quest without next quests information
                    orphanedBreadcrumbs.Add(pair.Key);
                }
            }

            if (QUESTS.Any())
            {
                var unsortedQuests = new List<object>();
                long maxQuestID = QUESTS.Max(x => x.Key);
                for (int i = 1; i <= maxQuestID; i++)
                {
                    // add any quest information which is not referenced but includes more than just a questID into the Unsorted category
                    if (!QUESTS_WITH_REFERENCES.ContainsKey(i) && QUESTS.TryGetValue(i, out IDictionary<string, object> questRef))
                    {
                        var entry = new Dictionary<string, object>() { { "questID", i } };

                        // put some API metadata as a Description (since no description tag will exist for unsorted quests) to help identify the quest source/purpose
                        questRef.TryGetValue("_type", out string qType);
                        questRef.TryGetValue("_area", out string qArea);
                        questRef.TryGetValue("_category", out string qCategory);
                        questRef.TryGetValue("_text", out string qText);

                        List<string> metaData = new List<string>();
                        if (qText != null)
                        {
                            if (!entry.ContainsKey("name"))
                                entry["name"] = qText;

                            metaData.Add("Name: |cFFf09f26" + qText + "|r");
                        }
                        if (qType != null)
                            metaData.Add("Type: |cFFf09f26" + qType + "|r");
                        if (qArea != null)
                            metaData.Add("Area: |cFFf09f26" + qArea + "|r");
                        if (qCategory != null)
                            metaData.Add("Category: |cFFf09f26" + qCategory + "|r");

                        if (metaData.Any())
                            questRef["description"] = string.Join("\n", metaData);

                        // merge any quest information from the quest DB so that field names in the questRef are accurate
                        Objects.Merge(entry, questRef);
                        // dont bother adding quests which literally have nothing useful in them
                        if (entry.Count > 1)
                        {
                            LevelConsolidation(entry, 1);
                            unsortedQuests.Add(entry);
                        }
                    }
                }
                if (unsortedQuests.Count > 0)
                {
                    if (CUSTOM_HEADER_CONSTANTS.TryGetValue("QUESTS", out long value))
                    {
                        Objects.Merge(unsorted, new Dictionary<string, object>
                        {
                            { "npcID", value },
                            { "g", unsortedQuests },
                        });
                    }
                    else
                    {
                        Trace.WriteLine("ERROR: COULD NOT FIND CONSTANT VALUE FOR 'QUESTS'!");
                        Objects.Merge(unsorted, unsortedQuests);
                    }
                }
            }

            // Notify of Post-Process Merge data which failed to merge...
            Objects.NotifyPostProcessMergeFailures();
        }

        private static void ProcessContainer(KeyValuePair<string, List<object>> container)
        {
            switch (container.Key)
            {
                // don't process uncollectibles in the normal way
                case "Uncollectible":
                    return;
                default:
                    break;
            }

            ProcessingAchievementCategory = container.Key.Contains("Achievement");
            Process(container.Value, 0, 1);
        }

        /// <summary>
        /// Does additional processing after the first pass of processing has completed
        /// </summary>
        private static void AdditionalProcessing()
        {
            // Mark uncollectibles & warn if Sourced
            if (Objects.AllContainers.TryGetValue("Uncollectible", out List<object> objects))
            {
                foreach (object itemObj in objects)
                {
                    if (itemObj is IDictionary<string, object> item)
                    {
                        decimal itemID = Items.GetSpecificItemID(item);
                        if (Items.IsItemReferenced(itemID))
                        {
                            LogDebug($"INFO: Item {itemID} is referenced and also included in Uncollectible.lua");
                        }
                        else
                        {
                            Items.MarkItemAsReferenced(itemID);
                        }
                    }
                }

                Objects.AllContainers.Remove("Uncollectible");
            }

            // Clean out any temporary containers
            string[] temporaryKeys = Objects.AllContainers.Keys.Where(k => k.StartsWith("_")).ToArray();
            temporaryKeys.All(k => Objects.AllContainers.Remove(k));

            // Merge conditional data
            foreach (var data in ConditionalItemData)
            {
                Items.Merge(data, true);
            }

            // Go through and merge all of the item species data into the item containers.
            foreach (var pair in Items.AllItemsWithSpecies)
            {
                var item = Items.GetNull(pair.Key);
                if (item != null) Items.MergeInto(pair.Key, pair.Value, item);
            }

            // Go through and merge all of the mount data into the item containers.
            foreach (var pair in Items.AllMounts)
            {
                var item = Items.GetNull(pair.Key);
                if (item != null) Items.MergeInto(pair.Key, new Dictionary<string, object> { { "mountID", pair.Value } }, item);
            }
        }

        private class TierList
        {
            public Dictionary<long, List<object>> FilteredLists = new Dictionary<long, List<object>>();
            public Dictionary<long, List<object>> ProfessionLists = new Dictionary<long, List<object>>();
            public List<object> Groups = new List<object>();

        }

        /// <summary>
        /// Attempt to sort the list by the name field.
        /// </summary>
        /// <param name="list">The list of objects.</param>
        public static void SortByName(List<object> list)
        {
            // If the list is null, then return immediately.
            if (list == null) return;

            // Sort the List by Name / Bonus ID / Mod ID
            list.Sort(SortByName);

            // Check to see if the list of objects has a relative g field.
            foreach (var objRef in list)
            {
                SortByName(objRef as IDictionary<string, object>);
            }
        }

        /// <summary>
        /// Sort the dictionary by its name field.
        /// </summary>
        /// <param name="a">Object Dictionary A.</param>
        public static void SortByName(IDictionary<string, object> a)
        {
            // If a is null, return immediately.
            if (a == null) return;

            // If a contains relative groups, then try to sort them.
            if (a.TryGetValue("g", out object aRef))
            {
                SortByName(aRef as List<object>);
            }
        }

        /// <summary>
        /// Sort two objects by their name field.
        /// </summary>
        /// <param name="a">Object Dictionary A.</param>
        /// <param name="b">Object Dictionary B.</param>
        /// <returns>Whether a is greater than b.</returns>
        public static int SortByName(object a, object b)
        {
            return SortByName(a as IDictionary<string, object>, b as IDictionary<string, object>);
        }

        /// <summary>
        /// Sort two dictionaries by their name field.
        /// </summary>
        /// <param name="a">Object Dictionary A.</param>
        /// <param name="b">Object Dictionary B.</param>
        /// <returns>Whether a is greater than b.</returns>
        public static int SortByName(IDictionary<string, object> a, IDictionary<string, object> b)
        {
            // If a is null,
            if (a == null)
            {
                // If b is also null, they are the same.
                if (b == null) return 0;

                // If not, then b is greater.
                return -1;
            }

            // If b is null, that means a is greater.
            if (b == null) return 1;

            // If a contains a name, then try to get it.
            if (a.ContainsKey("itemID") && Items.Get(a).TryGetValue("name", out string aRef))
            {
                // If b contains a name, then try to get it.
                if (b.ContainsKey("itemID") && Items.Get(b).TryGetValue("name", out string bRef))
                {
                    // Both have a name, compare them!
                    var first = aRef.ToString().CompareTo(bRef);
                    if (first == 0)
                    {
                        // If they have the same name, then sort by BonusID/ModID.
                        // If a contains a bonusID, then try to get it.
                        if (a.TryGetValue("bonusID", out aRef))
                        {
                            // If b contains a bonusID, then try to get it.
                            if (b.TryGetValue("bonusID", out bRef))
                            {
                                // Both have a bonusID, compare them!
                                return Convert.ToInt64(aRef).CompareTo(bRef);
                            }
                        }

                        // If a contains a modID, then try to get it.
                        if (a.TryGetValue("modID", out aRef))
                        {
                            // If b contains a modID, then try to get it.
                            if (b.TryGetValue("modID", out bRef))
                            {
                                // Both have a modID, compare them!
                                return Convert.ToInt64(aRef).CompareTo(bRef);
                            }
                        }

                        // If a contains a cost, then try to get it.
                        if (a.TryGetValue("cost", out aRef))
                        {
                            // If b contains a cost, then try to get it.
                            if (b.TryGetValue("cost", out bRef))
                            {
                                // Both have a cost, compare them!
                                return Convert.ToInt64(aRef).CompareTo(bRef);
                            }
                        }
                    }
                    return first;
                }
            }

            // If neither has a name, then they are equal.
            return 0;
        }
        #endregion
        #region Field Conversion
        /// <summary>
        /// Convert the field name to a standardized field name.
        /// This helps prevent inconsistent naming conventions from breaking things.
        /// </summary>
        /// <param name="field">The original field name.</param>
        /// <returns>The standardized field name.</returns>
        public static string ConvertFieldName(string field)
        {
            // Field Name Conversions
            switch (field)
            {
                case "g":
                case "group":
                case "groups":
                case "criteria":
                    {
                        return "g";
                    }

                case "bonus":
                case "bonusID":
                    {
                        return "bonusID";
                    }

                case "modID":
                case "itemModID":
                    {
                        return "modID";
                    }

                case "artifactId":
                case "artifactID":
                    {
                        return "artifactID";
                    }

                case "categoryId":
                case "categoryID":
                    {
                        return "categoryID";
                    }

                case "c":
                case "classes":
                case "classIDs":
                    {
                        return "c";
                    }

                case "coord":
                case "coordID":
                    {
                        return "coord";
                    }

                case "coords":
                case "coordIDs":
                    {
                        return "coords";
                    }

                case "explorationId":
                case "explorationID":
                    {
                        return "explorationID";
                    }

                case "explorationhash":
                case "explorationmap":
                case "maphash":
                    {
                        return "maphash";
                    }

                case "illusionId":
                case "illusionID":
                    {
                        return "illusionID";
                    }

                case "itemId":
                case "itemID":
                    {
                        return "itemID";
                    }

                case "toyId":
                case "toyID":
                    {
                        return "toyID";
                    }

                case "creatureId":
                case "creatureID":
                    //case "npcID": // TODO: eventually can we consolidate both of these into just one?
                    {
                        return "creatureID";
                    }

                case "s":
                case "sourceID":
                    {
                        return "s";
                    }
                /*
                case "dr":
                case "droprate":
                case "dropRate":
                    {
                        return "dr";
                    }
                */
                case "requireSkill":
                case "requiredSkill":
                    {
                        return "requireSkill";
                    }

                case "b":
                case "bind":
                case "binding":
                case "bindType":
                    {
                        return "b";
                    }

                case "e":
                case "ev":
                case "event":
                    {
                        return "e";
                    }

                case "f":
                case "filter":
                case "filterID":
                    {
                        return "f";
                    }
                case "fForRWP":
                case "filterForRWP":
                case "filterIDForRWP":
                    {
                        return "filterForRWP";
                    }

                case "ilvl":
                case "iLvl":
                case "ilevel":
                case "iLevel":
                    {
                        return "ilvl";
                    }

                case "lvl":
                case "Lvl":
                case "LvL":
                case "level":
                case "Level":
                case "requiredLevel":
                case "levelRequirement":
                case "reqlvl":
                case "reqlvls":
                case "reqLvl":
                case "reqLvls":
                    {
                        return "lvl";
                    }

                case "rank":
                case "azeriteRank":
                    {
                        return "rank";
                    }

                case "isRepeatable":
                case "repeatable":
                    {
                        return "repeatable";
                    }
                case "isLimited":
                    {
                        return "isLimited";
                    }

                case "isDaily":
                case "daily":
                case "dailyQuest":
                    {
                        return "isDaily";
                    }

                case "isWeekly":
                case "weekly":
                case "weeklyQuest":
                    {
                        return "isWeekly";
                    }

                case "isMonthly":
                case "monthly":
                case "monthlyQuest":
                    {
                        return "isMonthly";
                    }

                case "isYearly":
                case "yearly":
                case "yearlyQuest":
                    {
                        return "isYearly";
                    }

                case "isLockoutShared":
                case "isSharedLockout":
                case "sharedLockout":
                    {
                        return "isLockoutShared";
                    }

                case "q":
                case "quality":
                case "qualityId":
                case "qualityID":
                case "itemQuality":
                    {
                        return "q";
                    }

                case "mountId":
                case "mountID":
                    {
                        return "mountID";
                    }

                case "recipeId":
                case "recipeID":
                    {
                        return "recipeID";
                    }

                case "spellId":
                case "spellID":
                    {
                        return "spellID";
                    }

                case "speciesID":
                case "petID":
                case "species":
                    {
                        return "speciesID";
                    }

                case "specs":
                case "specializations":
                case "specializationRequirements":
                case "requiredSpecs":
                case "requiredSpecializations":
                    {
                        return "specs";
                    }

                case "u":
                case "un":
                case "unobtainable":
                    {
                        return "u";
                    }

                case "v":
                case "variants":
                case "bonuses":
                case "bonusIds":
                case "bonusIDs":
                    {
                        return "bonusIDs";
                    }

                case "m":
                case "mods":
                case "modIds":
                case "modIDs":
                    {
                        return "modIDs";
                    }

                case "sourceQuests":
                case "sourceQuestID":
                case "sourceQuestIDs":
                    {
                        return "sourceQuests";
                    }

                case "altAchieves":
                case "altAchievements":
                case "alternateAchieves":
                case "alternateAchievements":
                    {
                        return "altAchievements";
                    }

                case "altQuests":
                case "alternateQuests":
                case "exclusiveQuests":
                case "exclusiveWithQuests":
                    {
                        return "altQuests";
                    }

                case "altQuestID":
                    return "altQuestID";
                case "questID":
                    return "questID";
                case "aQuestID":
                case "allyQuestID":
                case "allianceQuestID":
                    return "questIDA";
                case "hQuestID":
                case "hordeQuestID":
                    return "questIDH";
                case "lc":
                case "lockCriteria":
                    return "lc";

                case "aqd":
                case "allianceQuestData":
                    {
                        return "aqd";
                    }

                case "hqd":
                case "hordeQuestData":
                    {
                        return "hqd";
                    }

                case "altSpeciesID":
                    {
                        return "altSpeciesID";
                    }

                case "altAchID":
                case "altAchievementID":
                case "hAchievementID":
                case "hordeAchievementID":
                    {
                        return "altAchID";
                    }

                case "achID":
                case "achievementID":
                case "aAchievementID":
                case "allyAchievementID":
                case "allianceAchievementID":
                    {
                        return "achID";
                    }

                case "achCatID":
                case "achCategoryID":
                case "achievementCategoryID":
                    {
                        return "achievementCategoryID";
                    }

                case "minRep":
                case "minReputation":
                    {
                        return "minReputation";
                    }
                case "maxRep":
                case "maxReputation":
                    {
                        return "maxReputation";
                    }

                case "availability":
                case "tl":
                case "timeline":
                    {
                        return "timeline";
                    }

                case "sourceQuestNumRequired":
                case "sqreq":
                    return "sqreq";

                // tags which are accurate already
                case "azeriteEssenceID":
                case "buildingID":
                case "class":
                case "classID":
                case "collectible":
                case "cost":
                case "cr":
                case "criteriaID":
                case "crs":
                case "currencyID":
                case "description":
                case "difficulties":
                case "difficultyID":
                case "DisablePartySync":
                case "displayID":
                case "encounterID":
                case "equippable":
                case "eventID":
                case "factionID":
                case "flightPathID":
                case "followerID":
                case "heirloomID":
                case "hideText":
                case "icon":
                case "ignoreBonus":
                case "ignoreSource":
                case "instanceID":
                case "inventoryType":
                case "isAquatic":
                case "isBreadcrumb":
                case "isFlying":
                case "isGround":
                case "isJumping":
                case "isOffHand":
                case "isRaid":
                case "isWorldQuest":
                case "lore":
                case "mapID":
                case "maps":
                case "missionID":
                case "model":
                case "modelID":
                case "modelRotation":
                case "modelScale":
                case "musicRollID":
                case "name":
                case "nextRecipeID":
                case "nomerge":
                case "npcID": // TODO: eventually consolidate with creatureID
                case "objectID":
                case "order":
                case "ordered":
                case "parentCategoryID":
                case "petAbilityID":
                case "previousRecipeID":
                case "professionID":
                case "provider":
                case "providers":
                case "pvp":
                case "qg":
                case "qgs":
                case "r": // horde/alliance faction
                case "races":
                case "runeforgePowerID":
                case "raceID":
                case "conduitID":
                case "customCollect":
                case "setHeaderID":
                case "setSubHeaderID":
                case "setID":
                case "sins":
                case "sort":
                case "sourceQuest":
                case "sourceText":
                case "style":
                case "subclass":
                case "sym":
                case "talentID":
                case "title":
                case "titleID":
                case "titleIDs":
                case "text":
                case "tierID":
                case "vignetteID":
                case "visualID":

                // metadata parser tags
                case "_area":
                case "_category":
                case "_drop":
                case "_npcs":
                case "_quests":
                case "_objects":
                case "_achievements":
                case "_factions":
                case "_encounter":
                case "_text":
                case "_type":

                    return field;

                // Probably not a known tag? will get mentioned in the object/item merge method
                default:
                    return field;
            }
        }
        #endregion
        #region JSON Conversion
        /// <summary>
        /// Convert the JSON string to a Dictionary with string,object pairs.
        /// </summary>
        /// <param name="jsonString">The JSON string.</param>
        /// <returns>The dictionary.</returns>
        public static IDictionary<string, object> ToDictionary(string jsonString)
        {
            return ToObject(jsonString) is IDictionary<string, object> obj ? obj : null;
        }

        /// <summary>
        /// Convert the Dictionary to JSON using Mini JSON.
        /// </summary>
        public static string ToJSON(IDictionary<string, object> data)
        {
            // typically we don't want to serialize the 'g' content of a given 'data' object
            // bit clunky but minijson doesn't seem to have much functionality... hence 'mini'
            return MiniJSON.Json.Serialize(data.AsEnumerable().Where(kvp => kvp.Key != "g").ToDictionary(kvp => kvp.Key, kvp => kvp.Value));
        }

        /// <summary>
        /// Convert the object to JSON using Mini JSON.
        /// </summary>
        /// <param name="obj">The object.</param>
        /// <returns>The JSON string.</returns>
        public static string ToJSON(object obj)
        {
            return MiniJSON.Json.Serialize(obj);
        }

        /// <summary>
        /// Convert the JSON string to an object.
        /// </summary>
        /// <param name="jsonString">The JSON String.</param>
        /// <returns>The object.</returns>
        public static object ToObject(string jsonString)
        {
            return MiniJSON.Json.Deserialize(jsonString);
        }
        #endregion
        #region Lua Conversion
        /// <summary>
        /// Merge the contents of the lua table into the database.
        /// If the keys are whitelisted, then the data will be added.
        /// </summary>
        /// <param name="lua">The lua context.</param>
        /// <param name="table">The raw lua table.</param>
        public static void Merge(LuaTable table)
        {
            // Parse the contents of the table into a generic object.
            var dict = ParseAsStringDictionary(table);
            if (dict == null) return;

            // Iterate through the pairs and determine what goes where.
            foreach (var pair in dict)
            {
                switch (pair.Key)
                {
                    case "IllusionDB":
                        {
                            // The format of the Illusions DB is a list of generic objects.
                            // This means that it becomes really easy to merge into the database.
                            if (pair.Value is List<object> illusionDB)
                            {
                                foreach (var o in illusionDB)
                                {
                                    if (o is IDictionary<string, object> illusion)
                                    {
                                        ConditionalItemData.Add(illusion);
                                    }
                                }
                            }
                            else
                            {
                                LogError("IllusionDB not in the correct format!");
                                Console.WriteLine(CurrentFileName);
                                Console.ReadLine();
                            }
                            break;
                        }
                    case "ItemDB":
                        {
                            // The format of the Item DB is a dictionary of item ID -> Values.
                            // This is slightly more annoying to parse, but it works okay.
                            if (pair.Value is Dictionary<long, object> itemDB)
                            {
                                foreach (var itemValuePair in itemDB)
                                {
                                    if (itemValuePair.Value is IDictionary<string, object> item)
                                    {
                                        item["itemID"] = itemValuePair.Key;
                                        Items.Merge(item);
                                    }
                                    else
                                    {
                                        LogError("ItemDB not in the correct format!");
                                        Console.WriteLine(CurrentFileName);
                                        Console.WriteLine(ToJSON(itemValuePair.Value));
                                        Console.ReadLine();
                                    }
                                }
                            }
                            else if (pair.Value is List<object> items)
                            {
                                foreach (var o in items)
                                {
                                    if (o is IDictionary<string, object> item)
                                    {
                                        Items.Merge(item);
                                    }
                                    else
                                    {
                                        LogError("ItemDB not in the correct format!");
                                        Console.WriteLine(CurrentFileName);
                                        Console.WriteLine(ToJSON(o));
                                        Console.ReadLine();
                                    }
                                }
                            }
                            else
                            {
                                LogError("ItemDB not in the correct format!");
                                Console.WriteLine(CurrentFileName);
                                Console.ReadLine();
                            }
                        }
                        break;
                    case "ItemDBConditional":
                        {
                            // The format of the Item DB is a dictionary of item ID -> Values.
                            // This is slightly more annoying to parse, but it works okay.
                            if (pair.Value is Dictionary<long, object> itemDB)
                            {
                                foreach (var itemValuePair in itemDB)
                                {
                                    if (itemValuePair.Value is IDictionary<string, object> item)
                                    {
                                        item["itemID"] = itemValuePair.Key;
                                        ConditionalItemData.Add(item);
                                    }
                                    else
                                    {
                                        LogError("ItemDB not in the correct format!");
                                        Console.WriteLine(CurrentFileName);
                                        Console.WriteLine(ToJSON(itemValuePair.Value));
                                        Console.ReadLine();
                                    }
                                }
                            }
                            else if (pair.Value is List<object> items)
                            {
                                foreach (var o in items)
                                {
                                    if (o is IDictionary<string, object> item)
                                    {
                                        ConditionalItemData.Add(item);
                                    }
                                    else
                                    {
                                        LogError("ItemDB not in the correct format!");
                                        Console.WriteLine(CurrentFileName);
                                        Console.WriteLine(ToJSON(o));
                                        Console.ReadLine();
                                    }
                                }
                            }
                            else
                            {
                                LogError("ItemDB not in the correct format!");
                                Console.WriteLine(CurrentFileName);
                                Console.ReadLine();
                            }
                        }
                        break;
                    case "Items.SOURCES":
                        {
                            if (pair.Value is Dictionary<decimal, object> db)
                            {
                                db.AsParallel().ForAll(Items.AddItemSourceID);
                            }
                            else
                            {
                                LogError($"{pair.Key} not in the correct format!");
                                Console.WriteLine(CurrentFileName);
                                Console.ReadLine();
                            }
                        }
                        break;
                    case "RecipeDB":
                        {
                            // The format of the RecipeDB is a dictionary of RecipeID -> Values.
                            if (pair.Value is Dictionary<long, object> recipeDB)
                            {
                                foreach (var recipeValuePair in recipeDB)
                                {
                                    if (recipeValuePair.Value is IDictionary<string, object> recipe)
                                    {
                                        recipe["recipeID"] = recipeValuePair.Key;
                                        Objects.Merge(recipe);
                                    }
                                    else
                                    {
                                        LogError("RecipeDB not in the correct format!");
                                        Console.WriteLine(CurrentFileName);
                                        Console.WriteLine(ToJSON(recipeValuePair.Value));
                                        Console.ReadLine();
                                    }
                                }
                            }
                            else if (pair.Value is List<object> recipes)
                            {
                                foreach (var o in recipes)
                                {
                                    if (o is IDictionary<string, object> recipe)
                                    {
                                        Objects.Merge(recipe);
                                    }
                                    else
                                    {
                                        LogError("ItemDB not in the correct format!");
                                        Console.WriteLine(CurrentFileName);
                                        Console.WriteLine(ToJSON(o));
                                        Console.ReadLine();
                                    }
                                }
                            }
                            else
                            {
                                LogError("ItemDB not in the correct format!");
                                Console.WriteLine(CurrentFileName);
                                Console.ReadLine();
                            }
                        }
                        break;
                    case "ItemMountDB":
                        {
                            // The format of the Item Mount DB is a dictionary of item ID -> Spell ID.
                            // This is slightly more annoying to parse, but it works okay.
                            if (pair.Value is Dictionary<long, object> itemMountDB)
                            {
                                foreach (var itemValuePair in itemMountDB)
                                {
                                    if (itemValuePair.Value is object o)
                                    {
                                        Items.SetMountSpellID(itemValuePair.Key, Convert.ToInt64(o));
                                    }
                                    else
                                    {
                                        LogError("ItemMountDB not in the correct format!");
                                        Console.WriteLine(CurrentFileName);
                                        Console.WriteLine(ToJSON(itemValuePair.Value));
                                        Console.ReadLine();
                                    }
                                }
                            }
                            else
                            {
                                LogError("ItemMountDB not in the correct format!");
                                Console.WriteLine(CurrentFileName);
                                Console.ReadLine();
                            }
                            break;
                        }
                    case "ItemSpeciesDB":
                        {
                            // The format of the Item Species DB is a dictionary of item ID -> Values.
                            // This is slightly more annoying to parse, but it works okay.
                            if (pair.Value is Dictionary<long, object> itemDB)
                            {
                                foreach (var itemValuePair in itemDB)
                                {
                                    if (itemValuePair.Value is IDictionary<string, object> item)
                                    {
                                        var itemSpecies = Items.GetWithSpecies(itemValuePair.Key);
                                        foreach (var p in item) Items.Merge(itemSpecies, p.Key, p.Value);
                                    }
                                    else
                                    {
                                        LogError("ItemSpeciesDB not in the correct format!");
                                        Console.WriteLine(CurrentFileName);
                                        Console.WriteLine(ToJSON(itemValuePair.Value));
                                        Console.ReadLine();
                                    }
                                }
                            }
                            else
                            {
                                LogError("ItemSpeciesDB not in the correct format!");
                                Console.WriteLine(CurrentFileName);
                                Console.ReadLine();
                            }
                            break;
                        }
                    case "ItemToyDB":
                        {
                            LogError("ItemToyDB not supported. Please use 'ItemDBConditional' and parser.config to assign Toy objects.");
                            Console.WriteLine(CurrentFileName);
                            Console.ReadLine();
                            break;
                        }
                    case "Artifacts":
                        {
                            if (pair.Value is Dictionary<long, object> artifactDB)
                            {
                                foreach (var itemValuePair in artifactDB)
                                {
                                    if (itemValuePair.Value is IDictionary<string, object> artifact)
                                    {
                                        long artifactID = itemValuePair.Key;
                                        if (!Objects.ArtifactSources.TryGetValue(artifactID, out Dictionary<string, long> artifactInfo))
                                            Objects.ArtifactSources[artifactID] = artifactInfo = new Dictionary<string, long>();

                                        foreach (var hand in artifact)
                                        {
                                            artifactInfo[ATT.Export.ToString(hand.Key)] = Convert.ToInt64(hand.Value);
                                        }
                                    }
                                }
                            }
                            else if (pair.Value is List<object> artifacts)
                            {
                                var artifactID = 0;
                                foreach (var o in artifacts)
                                {
                                    ++artifactID;
                                    if (o is IDictionary<string, object> artifact)
                                    {
                                        if (!Objects.ArtifactSources.TryGetValue(artifactID, out Dictionary<string, long> artifactInfo))
                                            Objects.ArtifactSources[artifactID] = artifactInfo = new Dictionary<string, long>();

                                        foreach (var hand in artifact)
                                        {
                                            artifactInfo[ATT.Export.ToString(hand.Key)] = Convert.ToInt64(hand.Value);
                                        }
                                    }
                                }
                            }
                            break;
                        }
                    case "CategoryDB":
                        {
                            // The format of the Category DB is a dictionary of Category ID <-> Category pairs.
                            if (pair.Value is Dictionary<long, object> db)
                            {
                                foreach (var keyValuePair in db)
                                {
                                    if (keyValuePair.Value is Dictionary<string, object> data)
                                    {
                                        if (!CategoryDB.TryGetValue(keyValuePair.Key, out Dictionary<string, object> dataEntry))
                                        {
                                            CategoryDB[keyValuePair.Key] = dataEntry = new Dictionary<string, object>();
                                        }

                                        // Merge over the complex text data.
                                        if (data.TryGetValue("text", out object textObject))
                                        {
                                            data.Remove("text");
                                            if (textObject is Dictionary<string, object> textLocales)
                                            {
                                                if (dataEntry.TryGetValue("text", out textObject) && textObject is Dictionary<string, object> currentTextLocales)
                                                {
                                                    foreach (var textObjectPair in textLocales)
                                                    {
                                                        currentTextLocales[textObjectPair.Key] = textObjectPair.Value;
                                                    }
                                                }
                                                else dataEntry["text"] = textLocales;
                                            }
                                            else
                                            {
                                                dataEntry["text"] = new Dictionary<string, object>
                                                {
                                                    { "en", textObject }
                                                };
                                            }
                                        }

                                        // Merge over simple data. (a simple replace is fine)
                                        foreach (var dataPair in data)
                                        {
                                            dataEntry[dataPair.Key] = dataPair.Value;
                                        }
                                    }
                                    else
                                    {
                                        LogError($"CategoryDB {keyValuePair.Key} not in the correct format!");
                                        Console.WriteLine(CurrentFileName);
                                        Console.ReadLine();
                                    }
                                }
                            }
                            else
                            {
                                LogError("CategoryDB not in the correct format!");
                                Console.WriteLine(CurrentFileName);
                                Console.ReadLine();
                            }
                            break;
                        }
                    case "CategoryIcons":   // Deprecated
                        {
                            // The format of the Category Icons DB is a dictionary of Category ID <-> Icon pairs.
                            if (pair.Value is Dictionary<long, object> CategoryIcons)
                            {
                                foreach (var categoryPair in CategoryIcons)
                                {
                                    // KEY: Category ID, VALUE: Icon
                                    if (categoryPair.Value is string icon)
                                    {
                                        if (CategoryDB.TryGetValue(categoryPair.Key, out Dictionary<string, object> categoryData))
                                        {
                                            categoryData["icon"] = icon;
                                        }
                                        else CategoryDB[categoryPair.Key] = new Dictionary<string, object>
                                        {
                                            { "icon", icon }
                                        };
                                    }
                                }
                            }
                            break;
                        }
                    case "CategoryNames":   // Deprecated
                        {
                            // The format of the Category Names DB is a dictionary of Category ID <-> Name pairs.
                            if (pair.Value is Dictionary<long, object> CategoryNames)
                            {
                                foreach (var categoryPair in CategoryNames)
                                {
                                    // KEY: Category ID, VALUE: Text
                                    if (categoryPair.Value is string name)
                                    {
                                        if (CategoryDB.TryGetValue(categoryPair.Key, out Dictionary<string, object> categoryData))
                                        {
                                            if (!categoryData.ContainsKey("readable")) categoryData["readable"] = name;
                                            if (categoryData.TryGetValue("text", out object textObject))
                                            {
                                                if (textObject is Dictionary<string, object> locale && !locale.ContainsKey("en"))
                                                {
                                                    locale["en"] = name;
                                                }
                                            }
                                            else
                                            {
                                                categoryData["text"] = new Dictionary<string, object>
                                                {
                                                    { "en", name }
                                                };
                                            }
                                        }
                                        else CategoryDB[categoryPair.Key] = new Dictionary<string, object>
                                        {
                                            { "readable", name },
                                            { "text",
                                                new Dictionary<string, object> {
                                                    { "en", name }
                                                }
                                            }
                                        };
                                    }
                                }
                            }
                            break;
                        }
                    case "FilterDB":
                        {
                            // The format of the Filter DB is a dictionary of Filter ID <-> Filter pairs.
                            if (pair.Value is Dictionary<long, object> db)
                            {
                                foreach (var keyValuePair in db)
                                {
                                    if (keyValuePair.Value is Dictionary<string, object> data)
                                    {
                                        if (!FilterDB.TryGetValue(keyValuePair.Key, out Dictionary<string, object> dataEntry))
                                        {
                                            FilterDB[keyValuePair.Key] = dataEntry = new Dictionary<string, object>();
                                        }

                                        // Merge over the complex text data.
                                        if (data.TryGetValue("text", out object textObject))
                                        {
                                            data.Remove("text");
                                            if (textObject is Dictionary<string, object> textLocales)
                                            {
                                                if (dataEntry.TryGetValue("text", out textObject) && textObject is Dictionary<string, object> currentTextLocales)
                                                {
                                                    foreach (var textObjectPair in textLocales)
                                                    {
                                                        currentTextLocales[textObjectPair.Key] = textObjectPair.Value;
                                                    }
                                                }
                                                else dataEntry["text"] = textLocales;
                                            }
                                            else
                                            {
                                                dataEntry["text"] = textLocales = new Dictionary<string, object>
                                                {
                                                    { "en", textObject }
                                                };
                                            }
                                        }

                                        // Explicitly mark a thing as an export
                                        if (data.TryGetValue("export", out object value) && (bool)value)
                                        {
                                            FILTERS_WITH_REFERENCES[keyValuePair.Key] = true;
                                        }

                                        // Merge over simple data. (a simple replace is fine)
                                        foreach (var dataPair in data)
                                        {
                                            dataEntry[dataPair.Key] = dataPair.Value;
                                        }
                                    }
                                    else
                                    {
                                        LogError($"FilterDB {keyValuePair.Key} not in the correct format!");
                                        Console.WriteLine(CurrentFileName);
                                        Console.ReadLine();
                                    }
                                }
                            }
                            else
                            {
                                LogError("FilterDB not in the correct format!");
                                Console.WriteLine(CurrentFileName);
                                Console.ReadLine();
                            }
                            break;
                        }
                    case "FlightPathDB":
                        {
                            // The format of the Object DB is a dictionary of Flight Path ID <-> Flight Path pairs.
                            if (pair.Value is Dictionary<long, object> db)
                            {
                                foreach (var keyValuePair in db)
                                {
                                    if (keyValuePair.Value is Dictionary<string, object> data)
                                    {
                                        if (!FlightPathDB.TryGetValue(keyValuePair.Key, out Dictionary<string, object> dataEntry))
                                        {
                                            FlightPathDB[keyValuePair.Key] = dataEntry = new Dictionary<string, object>();
                                        }

                                        // Merge over the complex text data.
                                        if (data.TryGetValue("text", out object textObject))
                                        {
                                            data.Remove("text");
                                            if (textObject is Dictionary<string, object> textLocales)
                                            {
                                                if (dataEntry.TryGetValue("text", out textObject) && textObject is Dictionary<string, object> currentTextLocales)
                                                {
                                                    foreach (var textObjectPair in textLocales)
                                                    {
                                                        currentTextLocales[textObjectPair.Key] = textObjectPair.Value;
                                                    }
                                                }
                                                else dataEntry["text"] = textLocales;
                                            }
                                            else
                                            {
                                                dataEntry["text"] = new Dictionary<string, object>
                                                {
                                                    { "en", textObject }
                                                };
                                            }
                                        }

                                        // Merge over simple data. (a simple replace is fine)
                                        foreach (var dataPair in data)
                                        {
                                            dataEntry[dataPair.Key] = dataPair.Value;
                                        }
                                    }
                                    else
                                    {
                                        LogError($"FlightPathDB {keyValuePair.Key} not in the correct format!");
                                        Console.WriteLine(CurrentFileName);
                                        Console.ReadLine();
                                    }
                                }
                            }
                            else
                            {
                                LogError("FlightPathDB not in the correct format!");
                                Console.WriteLine(CurrentFileName);
                                Console.ReadLine();
                            }
                            break;
                        }
                    case "ObjectDB":
                        {
                            // The format of the Object DB is a dictionary of Object ID <-> Object pairs.
                            if (pair.Value is Dictionary<long, object> db)
                            {
                                foreach (var keyValuePair in db)
                                {
                                    if (keyValuePair.Value is Dictionary<string, object> data)
                                    {
                                        if (!ObjectDB.TryGetValue(keyValuePair.Key, out Dictionary<string, object> dataEntry))
                                        {
                                            ObjectDB[keyValuePair.Key] = dataEntry = new Dictionary<string, object>();
                                        }

                                        // Merge over the complex text data.
                                        if (data.TryGetValue("text", out object textObject))
                                        {
                                            data.Remove("text");
                                            if (textObject is Dictionary<string, object> textLocales)
                                            {
                                                if (dataEntry.TryGetValue("text", out textObject) && textObject is Dictionary<string, object> currentTextLocales)
                                                {
                                                    foreach (var textObjectPair in textLocales)
                                                    {
                                                        currentTextLocales[textObjectPair.Key] = textObjectPair.Value;
                                                    }
                                                }
                                                else dataEntry["text"] = textLocales;
                                            }
                                            else
                                            {
                                                dataEntry["text"] = new Dictionary<string, object>
                                                {
                                                    { "en", textObject }
                                                };
                                            }
                                        }

                                        // Merge over simple data. (a simple replace is fine)
                                        foreach (var dataPair in data)
                                        {
                                            dataEntry[dataPair.Key] = dataPair.Value;
                                        }
                                    }
                                    else
                                    {
                                        LogError($"ObjectDB {keyValuePair.Key} not in the correct format!");
                                        Console.WriteLine(CurrentFileName);
                                        Console.ReadLine();
                                    }
                                }
                            }
                            else
                            {
                                LogError("ObjectDB not in the correct format!");
                                Console.WriteLine(CurrentFileName);
                                Console.ReadLine();
                            }
                            break;
                        }
                    case "ObjectIcons":   // Deprecated
                        {
                            // The format of the Object Icons DB is a dictionary of Object ID <-> Icon pairs.
                            if (pair.Value is Dictionary<long, object> ObjectIcons)
                            {
                                foreach (var categoryPair in ObjectIcons)
                                {
                                    // KEY: Object ID, VALUE: Icon
                                    if (categoryPair.Value is string name)
                                    {
                                        if (ObjectDB.TryGetValue(categoryPair.Key, out Dictionary<string, object> objectData))
                                        {
                                            objectData["icon"] = name;
                                        }
                                        else ObjectDB[categoryPair.Key] = new Dictionary<string, object>
                                        {
                                            { "icon", name }
                                        };
                                    }
                                }
                            }
                            break;
                        }
                    case "ObjectModels":   // Deprecated
                        {
                            // The format of the Object Models DB is a dictionary of Object ID <-> Model ID pairs.
                            if (pair.Value is Dictionary<long, object> ObjectModels)
                            {
                                foreach (var categoryPair in ObjectModels)
                                {
                                    // KEY: Object ID, VALUE: Model ID
                                    if (categoryPair.Value is long modelID)
                                    {
                                        if (ObjectDB.TryGetValue(categoryPair.Key, out Dictionary<string, object> objectData))
                                        {
                                            objectData["model"] = modelID;
                                        }
                                        else ObjectDB[categoryPair.Key] = new Dictionary<string, object>
                                        {
                                            { "model", modelID }
                                        };
                                    }
                                }
                            }
                            break;
                        }
                    case "ObjectNames":   // Deprecated
                        {
                            // The format of the Object Names DB is a dictionary of Object ID <-> Name pairs.
                            if (pair.Value is Dictionary<long, object> ObjectNames)
                            {
                                foreach (var categoryPair in ObjectNames)
                                {
                                    // KEY: Object ID, VALUE: Name
                                    if (categoryPair.Value is string name)
                                    {
                                        if (ObjectDB.TryGetValue(categoryPair.Key, out Dictionary<string, object> objectData))
                                        {
                                            if (!objectData.ContainsKey("readable")) objectData["readable"] = name;
                                            if (objectData.TryGetValue("text", out object textObject))
                                            {
                                                if (textObject is Dictionary<string, object> locale && !locale.ContainsKey("en"))
                                                {
                                                    locale["en"] = name;
                                                }
                                            }
                                            else
                                            {
                                                objectData["text"] = new Dictionary<string, object>
                                                {
                                                    { "en", name }
                                                };
                                            }
                                        }
                                        else ObjectDB[categoryPair.Key] = new Dictionary<string, object>
                                        {
                                            { "readable", name },
                                            { "text",
                                                new Dictionary<string, object> {
                                                    { "en", name }
                                                }
                                            }
                                        };
                                    }
                                }
                            }
                            break;
                        }
                    case "ObjectNamesForLocales":   // Deprecated
                        {
                            // The format of the Object Names DB is a dictionary of Locale <-> { Object ID <-> Name pairs }.
                            if (pair.Value is Dictionary<string, object> ObjectNamesForLocales)
                            {
                                foreach (var localePair in ObjectNamesForLocales)
                                {
                                    if (localePair.Value is Dictionary<long, object> ObjectNames)
                                    {
                                        foreach (var categoryPair in ObjectNames)
                                        {
                                            // KEY: Object ID, VALUE: Name
                                            if (categoryPair.Value is string name)
                                            {
                                                if (ObjectDB.TryGetValue(categoryPair.Key, out Dictionary<string, object> objectData))
                                                {
                                                    if (objectData.TryGetValue("text", out object textObject))
                                                    {
                                                        if (textObject is Dictionary<string, object> locale && !locale.ContainsKey(localePair.Key))
                                                        {
                                                            locale[localePair.Key] = name;
                                                        }
                                                    }
                                                    else
                                                    {
                                                        objectData["text"] = new Dictionary<string, object>
                                                        {
                                                            { localePair.Key, name }
                                                        };
                                                    }
                                                }
                                                else ObjectDB[categoryPair.Key] = new Dictionary<string, object>
                                                {
                                                    { "text",
                                                        new Dictionary<string, object> {
                                                            { localePair.Key, name }
                                                        }
                                                    }
                                                };
                                            }
                                        }
                                    }
                                }
                            }
                            break;
                        }
                    case "AchievementDB":
                        // The format of the Achievement DB is a dictionary of Achievement ID <-> Name pairs.
                        var dataList = Objects.CompressToList<object>(pair.Value);
                        if (dataList == null)
                        {
                            LogError("Failed to Parse AchievementDB");
                        }
                        else
                        {
                            foreach (var achieveInfo in dataList)
                            {
                                // KEY: Achievement ID, VALUE: Dictionary
                                if (achieveInfo is IDictionary<string, object> info && info.TryGetValue("achID", out long achID))
                                {
                                    if (ACHIEVEMENTS.TryGetValue(achID, out IDictionary<string, object> existingData))
                                    {
                                        foreach (var pair2 in info) Objects.Merge(existingData, pair2.Key, pair2.Value);
                                    }
                                    else ACHIEVEMENTS[achID] = info;
                                }
                            }
                        }
                        break;
                    default:
                        {
                            // Get the object container for this section.
                            List<object> list = null;
                            if (pair.Value is List<object> list2)
                            {
                                list = list2;
                            }
                            else if (pair.Value is IDictionary<string, object> data)
                            {
                                list = data.Values.ToList();
                            }
                            else if (pair.Value is Dictionary<long, object> ignoredKeys)
                            {
                                list = ignoredKeys.Values.ToList();
                            }
                            if (list != null)
                            {
                                if (list.Any()) Objects.Merge(Objects.Get(ATT.Export.ToString(pair.Key)), list);
                            }
                            else
                            {
                                Console.Write("Invalid Container format: ");
                                Console.WriteLine(pair.Key);
                                Console.ReadLine();
                                Console.WriteLine(ToJSON(pair.Value));
                                Console.ReadLine();
                                throw new Exception("Invalid Container format!");
                            }
                            break;
                        }
                }
            }
        }

        static object ParseAsObject(LuaTable table)
        {
            if (table.Keys.Count > 0)
            {
                // Determine most-necessary key type
                bool useDecimal = false, useLong = false;
                var keyList = new List<object>();
                foreach (var key in table.Keys)
                {
                    var keyType = key.GetType();
                    if (keyType == typeof(string))
                    {
                        if (table[key].GetType() == typeof(LuaFunction)) continue;
                        return ParseAsStringDictionary(table);
                    }
                    else if (!useDecimal && keyType.IsDecimal())
                    {
                        useDecimal = true;
                    }
                    else if (!useDecimal && !useLong && keyType.IsNumeric())
                    {
                        useLong = true;
                    }
                    keyList.Add(key);
                }
                //keyList.Sort();

                // Determine if we're dealing with a <long,object> dictionary.
                for (int i = 1; i <= keyList.Count; ++i)
                {
                    var key = keyList[i - 1];
                    if (key.TryConvert(out int index) && (index != i || index > keyList.Count))
                    {
                        if (useDecimal)
                        {
                            return ParseAsDictionary<decimal>(table);
                        }
                        else
                        {
                            return ParseAsDictionary<long>(table);
                        }
                    }
                }

                // Create an ordered list from the table.
                var list = new List<object>();
                foreach (var key in keyList)
                {
                    list.Add(ParseObject(table[key]));
                }
                return list;
            }

            return new List<object>();
        }

        public static Dictionary<TKey, object> ParseAsDictionary<TKey>(LuaTable table)
        {
            var dict = new Dictionary<TKey, object>();
            foreach (var key in table.Keys)
            {
                if (key.TryConvert(out TKey tKey))
                {
                    dict[tKey] = ParseObject(table[key]);
                }
                else
                {
                    LogError($"Failed to convert {key} to type {typeof(TKey).GetType().Name}");
                }
            }
            return dict;
        }

        static IDictionary<string, object> ParseAsStringDictionary(LuaTable table)
        {
            var dict = new Dictionary<string, object>();
            foreach (var key in table.Keys) dict[ConvertFieldName(key.ToString())] = ParseObject(table[key]);
            return dict;
        }

        static object ParseObject(object data)
        {
            switch (data.GetType().ToString())
            {
                case "NLua.LuaTable":
                    {
                        return ParseAsObject(data as LuaTable);
                    }
                case "System.Boolean":
                case "System.Double":
                case "System.Int32":
                case "System.Int64":
                case "System.String":
                    {
                        return data;
                    }
                case "NLua.LuaFunction":
                    {
                        Trace.Write(" (");
                        Trace.Write(data.GetType().ToString());
                        Trace.Write("): ");
                        Trace.WriteLine(data);
                        Trace.WriteLine("Functions are not directly supported at this time. Please use a [[ ]] surrounded string.");
                        Console.ReadLine();
                        break;
                    }
                default:
                    {
                        Trace.Write(" (");
                        Trace.Write(data.GetType().ToString());
                        Trace.Write("): ");
                        Trace.WriteLine(data);
                        Console.ReadLine();
                        break;
                    }
            }
            return null;
        }

        static StringBuilder ExportObjectKeyValue(StringBuilder builder, object key, object value)
        {
            return builder.Append("\t[").Append(key).Append("] = ").Append(value).Append(",");
        }

        static StringBuilder ExportStringValue(StringBuilder builder, string value)
        {
            value = value.Replace("\n", "\\n").Replace("\r", "\\r");
            if (value.StartsWith("~"))
            {
                return builder.Append(value.Substring(1));
            }
            else if (value.StartsWith("GetSpellInfo") || value.StartsWith("GetItem") || value.StartsWith("select(") || value.StartsWith("C_")
                || value.StartsWith("_."))
            {
                return builder.Append(value);
            }
            return builder.Append("\"").Append(value.Replace("\"", "\\\"")).Append("\"");
        }

        static StringBuilder ExportStringKeyValue(StringBuilder builder, object key, string value)
        {
            builder.Append("\t[").Append(key).Append("] = ");
            return ExportStringValue(builder, value).Append(",");
        }

        static StringBuilder ExportStringKeyFieldValue(StringBuilder builder, object key, string field, string value)
        {
            builder.Append("[").Append(key).Append("]").Append(field).Append(" = ");
            return ExportStringValue(builder, value);
        }

        static StringBuilder ExportReadableConstantComment(StringBuilder builder, string readable, string constant)
        {
            if (string.IsNullOrEmpty(readable))
            {
                builder.Append("\t-- (MISSING 'readable')");
            }
            else
            {
                builder.Append("\t-- ").Append(readable);
            }
            if (!string.IsNullOrEmpty(constant))
            {
                return builder.Append(" [").Append(constant).Append("]");
            }
            return builder;
        }
        #endregion

        #region Export (Clean)
        /// <summary>
        /// Export the data to the builder in a clean, longhand format.
        /// Standardized formatting without newlines applies here.
        /// </summary>
        /// <param name="builder">The builder.</param>
        /// <param name="data">The undetermined object data.</param>
        /// <param name="indent">The string to prefix before each line. (indenting)</param>
        public static void ExportClean(StringBuilder builder, object data)
        {
            // Firstly, we need to know the type of object we're working with.
            if (data is bool b) builder.Append(b ? "1" : "false");  // NOTE: 0 in lua is evaluated as true, not false. So we can't shorten it. (rip)
            else if (data is List<object> list) ExportClean(builder, list);
            else if (data is IDictionary<string, object> dict) ExportClean(builder, dict);
            else if (data is string str) builder.Append('"').Append(str.Replace("\"", "\\\"")).Append('"');
            else if (data is Dictionary<long, object> longdict) ExportClean(builder, longdict);
            else if (data is Dictionary<long, long> longlongdict) ExportClean(builder, longlongdict);
            else if (data is Dictionary<string, List<object>> listdict) ExportClean(builder, listdict);
            else if (data is List<List<object>> listOLists) ExportClean(builder, listOLists);
            else
            {
                // Default: Write it as a String. Best of luck.
                builder.Append(ATT.Export.ToString(data));
            }
        }

        /// <summary>
        /// Export the contents of the dictionary to the builder in a clean, longhand format.
        /// Every field will be written. Standardized formatting without newlines applies here.
        /// </summary>
        /// <typeparam name="KEY">The key value type of the dictionary.</typeparam>
        /// <typeparam name="VALUE">The value type of the dictionary.</typeparam>
        /// <param name="builder">The builder.</param>
        /// <param name="data">The data dictionary.</param>
        public static void ExportClean<KEY, VALUE>(StringBuilder builder, Dictionary<KEY, VALUE> data)
        {
            // If the dictionary doesn't have any content, then return immediately.
            if (data.Count == 0)
            {
                builder.Append("{}");
                return;
            }

            // Open Bracket for beginning of the Dictionary.
            builder.Append('{');

            // Clone this and calculate most significant.
            bool hasG = false;
            VALUE g = default(VALUE);    // Look for the G Field.
            var data2 = new Dictionary<object, object>();
            var keys = data.Keys.ToList();
            for (int i = 0, count = keys.Count; i < count; ++i)
            {
                if (keys[i].ToString() == "g")
                {
                    g = data[keys[i]];
                    keys.RemoveAt(i);
                    hasG = true;
                    break;
                }
            }
            keys.Sort();
            foreach (var key in keys) data2[key] = data[key];

            // Export Fields
            int fieldCount = 0;
            foreach (var pair in data2)
            {
                // If this is NOT the first field, append a comma.
                if (fieldCount++ > 0) builder.Append(',');

                // Append the Field and its Value
                builder.Append(pair.Key).Append('=');
                ExportClean(builder, pair.Value);
            }

            // We wanted to move this to the bottom of the hierarchy.
            if (hasG)
            {
                // If this is NOT the first field, append a comma.
                if (fieldCount++ > 0) builder.Append(',');

                // Append the Field and its Value
                builder.Append("g=");
                ExportClean(builder, g);
            }

            // Close Bracket for the end of the Dictionary.
            builder.Append('}');
        }

        /// <summary>
        /// Export the contents of the list to the builder in a clean, longhand format.
        /// Every element will be written. Standardized formatting without newlines applies here.
        /// </summary>
        /// <param name="builder">The builder.</param>
        /// <param name="list">The list of data.</param>
        public static void ExportClean<VALUE>(StringBuilder builder, List<VALUE> list)
        {
            // If the list doesn't have any content, then return immediately.
            var count = list.Count;
            if (count == 0)
            {
                builder.Append("{}");
                return;
            }

            // Open Bracket for beginning of the List.
            builder.Append('{');

            // Export Fields
            for (int i = 0; i < count; ++i)
            {
                // If this is NOT the first field, append a comma.
                if (i > 0) builder.Append(',');

                // Append the undetermined object's format to the builder.
                ExportClean(builder, list[i]);
            }

            // Close Bracket for the end of the List.
            builder.Append('}');
        }

        /// <summary>
        /// Export the data to the builder in a clean, longhand format.
        /// Standardized formatting without newlines applies here.
        /// </summary>
        /// <param name="data">The undetermined object data.</param>
        /// <returns>A built string containing the information.</returns>
        public static StringBuilder ExportClean(object data)
        {
            var builder = new StringBuilder();
            ExportClean(builder, data);
            return builder;
        }

        /// <summary>
        /// Export the contents of the dictionary to the builder in a clean, longhand format.
        /// Every field will be written. Standardized formatting without newlines applies here.
        /// </summary>
        /// <typeparam name="KEY">The key value type of the dictionary.</typeparam>
        /// <typeparam name="VALUE">The value type of the dictionary.</typeparam>
        /// <param name="data">The data dictionary.</param>
        /// <returns>A built string containing the information.</returns>
        public static StringBuilder ExportClean<KEY, VALUE>(Dictionary<KEY, VALUE> data)
        {
            var builder = new StringBuilder();
            ExportClean(builder, data);
            return builder;
        }

        /// <summary>
        /// Export the contents of the list to the builder in a clean, longhand format.
        /// Every element will be written. Standardized formatting without newlines applies here.
        /// </summary>
        /// <param name="list">The list of data.</param>
        /// <returns>A built string containing the information.</returns>
        public static StringBuilder ExportClean<T>(List<T> list)
        {
            var builder = new StringBuilder();
            ExportClean(builder, list);
            return builder;
        }
        #endregion
		
		private static string GetBaseDBRootFolder() {
#if DF
            return "Dragonflight/";
#elif SHADOWLANDS
            return "Shadowlands/";
#elif BFA
            return "BFA/";
#elif LEGION
            return "Legion/";
#elif WOD
            return "WOD/";
#elif MOP
            return "MOP/";
#elif CATA
            return "Cata/";
#elif WRATH
            return "Wrath/";
#elif TBC
            return "TBC/";
#else
            return "Classic/";
#endif
		}
		
        /// <summary>
        /// Export the database.
        /// This also exports for debugging as well.
        /// </summary>
        public static void Export()
        {
            // Default is relative to where the executable is. (.contrib/Parser)
            string addonRootFolder = Config["root-addon"] ?? "../..";
            string dbRootFolder = Config["db-relative"] ?? GetBaseDBRootFolder();

            // Setup the output folder (/db)
            var outputFolder = Directory.CreateDirectory($"{addonRootFolder}/db/{dbRootFolder}");
            if (outputFolder.Exists)
            {

                // DEBUGGING: Output Parsed Data
                if (DebugMode)
                {
                    CurrentParseStage = ParseStage.ExportDebugData;

                    ATT.Export.DebugMode = true;
                    var debugFolder = Directory.CreateDirectory($"{addonRootFolder}/.contrib/Debugging");
                    if (debugFolder.Exists)
                    {
                        // Export various debug information to the Debugging folder.
                        Items.ExportDebug(debugFolder.FullName);
                        Objects.ExportDebug(debugFolder.FullName);
                        Objects.ExportDB(debugFolder.FullName);

                        // Export custom Debug DB data to the Debugging folder. (as JSON for simplicity)
                        foreach (KeyValuePair<string, SortedDictionary<decimal, List<IDictionary<string, object>>>> dbKeyDatas in DebugDBs)
                        {
                            File.WriteAllText(Path.Combine(debugFolder.FullName, dbKeyDatas.Key + "_DebugDB.json"), ToJSON(dbKeyDatas.Value), Encoding.UTF8);
                        }

                        // Export the Category DB file.
                        if (CategoryDB.Any())
                        {
                            var builder = new StringBuilder("---------------------------------------------------------\n--   C A T E G O R Y   D A T A B A S E   M O D U L E   --\n---------------------------------------------------------\n");
                            var keys = CategoryDB.Keys.ToList();
                            keys.Sort();
                            builder.Append("local CategoryDB = CategoryDB; for categoryID,categoryData in pairs({").AppendLine();
                            foreach (var key in keys)
                            {
                                Dictionary<string, object> categoryData = CategoryDB[key];
                                builder.Append("\t[").Append(key).AppendLine("] = {");

                                // Attempt to get the text locale data object.
                                categoryData.TryGetValue("text", out object textLocaleObject);
                                Dictionary<string, object> textLocales = textLocaleObject as Dictionary<string, object>;

                                // Export the "readable" field. (database only, not exported to game)
                                if (categoryData.TryGetValue("readable", out string treadable))
                                {
                                    builder.Append("\t\treadable = ");
                                    ExportStringValue(builder, treadable).AppendLine(",");
                                }
                                else if (textLocales != null && textLocales.TryGetValue("en", out string name))
                                {
                                    builder.Append("\t\treadable = ");
                                    ExportStringValue(builder, name).AppendLine(",");
                                }

                                // Export the "icon" field.
                                if (categoryData.TryGetValue("icon", out string icon))
                                {
                                    builder.Append("\t\ticon = ");
                                    ExportStringValue(builder, icon.Replace("\\", "/")).AppendLine(",");
                                }

                                // Export the complex "text" locales field.
                                if (textLocales != null)
                                {
                                    // Sort and then ensure es comes after en, to match previous convention.
                                    var supportedLocales = textLocales.Keys.ToList();
                                    supportedLocales.Sort();
                                    if (supportedLocales.Contains("es"))
                                    {
                                        supportedLocales.Remove("es");
                                        supportedLocales.Insert(0, "es");
                                    }
                                    if (supportedLocales.Contains("en"))
                                    {
                                        supportedLocales.Remove("en");
                                        supportedLocales.Insert(0, "en");
                                    }
                                    if (supportedLocales.Contains("ko"))
                                    {
                                        supportedLocales.Remove("ko");
                                        supportedLocales.Add("ko");
                                    }
                                    if (supportedLocales.Contains("cn"))
                                    {
                                        supportedLocales.Remove("cn");
                                        supportedLocales.Add("cn");
                                    }

                                    builder.AppendLine("\t\ttext = {");
                                    foreach (var localeKey in supportedLocales)
                                    {
                                        builder.Append("\t\t\t").Append(localeKey).Append(" = ");
                                        ExportStringValue(builder, textLocales[localeKey].ToString()).AppendLine(",");
                                    }
                                    builder.AppendLine("\t\t},");
                                }
                                builder.AppendLine("\t},");
                            }
                            builder.AppendLine("})").AppendLine("do CategoryDB[categoryID] = categoryData; end");
                            File.WriteAllText(Path.Combine(debugFolder.FullName, "CategoryDB.lua"), builder.ToString(), Encoding.UTF8);
                        }

                        // Export the Custom Headers file.
                        if (CustomHeaders != null && CustomHeaders.Any())
                        {
                            // Now export it based on what we know.
                            var builder = new StringBuilder("-------------------------------------------------------\n--   C U S T O M   H E A D E R S   M O D U L E   --\n-------------------------------------------------------\n")
                                .AppendLine("local headers = CustomHeaders or {};");
                            var subbuilder = new StringBuilder();
                            var keys = new List<long>();
                            var icons = new Dictionary<long, string>();
                            var constants = new Dictionary<long, string>();
                            var localizationForText = new Dictionary<string, Dictionary<long, string>>();
                            var localizationForLore = new Dictionary<string, Dictionary<long, string>>();
                            var localizationForDescriptions = new Dictionary<string, Dictionary<long, string>>();
                            foreach (var key in CustomHeaders.Keys)
                            {
                                if (CustomHeaders.TryGetValue(key, out object o) && o is IDictionary<string, object> header)
                                {
                                    keys.Add(key);
                                    subbuilder.Clear();
                                    string readable = null, filepath = null, icon = null, constant = null;
                                    if (header.TryGetValue("readable", out object value))
                                    {
                                        readable = value.ToString();
                                    }
                                    else
                                    {
                                        subbuilder.Append("headers[").Append(key).Append("].readable = \"\";\t-- MISSING 'readable'! This is required!").AppendLine();
                                    }
                                    if (header.TryGetValue("constant", out value))
                                    {
                                        constant = value.ToString();
                                    }
                                    if (header.TryGetValue("filepath", out value))
                                    {
                                        filepath = value.ToString();

                                    }
                                    if (header.TryGetValue("icon", out value))
                                    {
                                        icon = value.ToString().Replace("\\", "/");
                                    }
                                    else
                                    {
                                        subbuilder.Append("headers");
                                        ExportStringKeyFieldValue(subbuilder, key, ".icon", "Interface/Icons/inv_misc_questionmark");
                                        subbuilder.Append(";");
                                        ExportReadableConstantComment(subbuilder, readable, constant).AppendLine();
                                    }
                                    if (header.TryGetValue("text", out value))
                                    {
                                        if (!(value is IDictionary<string, object> localeData))
                                        {
                                            localeData = new Dictionary<string, object>
                                            {
                                                ["en"] = value
                                            };
                                        }
                                        if (!localeData.TryGetValue("en", out string enString))
                                        {
                                            enString = readable;
                                            subbuilder.Append("headers");
                                            ExportStringKeyFieldValue(subbuilder, key, ".text.en", enString);
                                            subbuilder.Append(";");
                                            ExportReadableConstantComment(subbuilder, readable, constant).AppendLine(" - You MUST supply an 'en' localization!");
                                            localeData["en"] = enString;    // This will prevent it from getting written twice
                                        }
                                        if (!enString.Contains("~"))
                                        {
                                            foreach (var locale in SupportedLocales)
                                            {
                                                if (!localeData.TryGetValue(locale, out value))
                                                {
                                                    subbuilder.Append("headers");
                                                    ExportStringKeyFieldValue(subbuilder, key, $".text.{locale}", enString);
                                                    subbuilder.Append(";");
                                                    ExportReadableConstantComment(subbuilder, readable, constant).AppendLine();
                                                }
                                            }
                                        }
                                    }
                                    if (header.TryGetValue("description", out value))
                                    {
                                        if (!(value is IDictionary<string, object> localeData))
                                        {
                                            localeData = new Dictionary<string, object>
                                            {
                                                ["en"] = value
                                            };
                                        }
                                        if (!localeData.TryGetValue("en", out string enString))
                                        {
                                            enString = readable;
                                            subbuilder.Append("headers");
                                            ExportStringKeyFieldValue(subbuilder, key, ".description.en", enString);
                                            subbuilder.Append(";");
                                            ExportReadableConstantComment(subbuilder, readable, constant).AppendLine(" - You MUST supply an 'en' localization!");
                                            localeData["en"] = enString;    // This will prevent it from getting written twice
                                        }
                                        if (!enString.Contains("~"))
                                        {
                                            foreach (var locale in SupportedLocales)
                                            {
                                                if (!localeData.TryGetValue(locale, out value))
                                                {
                                                    subbuilder.Append("headers");
                                                    ExportStringKeyFieldValue(subbuilder, key, $".description.{locale}", enString);
                                                    subbuilder.Append(";");
                                                    ExportReadableConstantComment(subbuilder, readable, constant).AppendLine();
                                                }
                                            }
                                        }
                                    }
                                    if (header.TryGetValue("lore", out value))
                                    {
                                        if (!(value is IDictionary<string, object> localeData))
                                        {
                                            localeData = new Dictionary<string, object>
                                            {
                                                ["en"] = value
                                            };
                                        }
                                        if (!localeData.TryGetValue("en", out string enString))
                                        {
                                            enString = readable;
                                            subbuilder.Append("headers");
                                            ExportStringKeyFieldValue(subbuilder, key, ".lore.en", enString);
                                            subbuilder.Append(";");
                                            ExportReadableConstantComment(subbuilder, readable, constant).AppendLine(" - You MUST supply an 'en' localization!");
                                            localeData["en"] = enString;    // This will prevent it from getting written twice
                                        }
                                        if (!enString.Contains("~"))
                                        {
                                            foreach (var locale in SupportedLocales)
                                            {
                                                if (!localeData.TryGetValue(locale, out value))
                                                {
                                                    subbuilder.Append("headers");
                                                    ExportStringKeyFieldValue(subbuilder, key, $".lore.{locale}", enString);
                                                    subbuilder.Append(";");
                                                    ExportReadableConstantComment(subbuilder, readable, constant).AppendLine();
                                                }
                                            }
                                        }
                                    }

                                    if (subbuilder.Length > 0)
                                    {
                                        builder.Append("-- ").AppendLine(Path.GetFullPath(filepath));
                                        builder.Append(subbuilder.ToString());
                                    }
                                }
                            }
                            keys.Sort(new Comparison<long>((i1, i2) => i2.CompareTo(i1)));

                            // Check to make sure the content is different since Diff tools are dumb as hell.
                            File.WriteAllText(Path.Combine(debugFolder.FullName, "Custom Headers.lua"), builder.ToString().Replace("\r\n", "\n").Trim(), Encoding.UTF8);
                        }

                        // Export the FilterDB file.
                        if (FilterDB.Any())
                        {
                            // Export the new format.
                            var builder = new StringBuilder("-----------------------------------------------------\n--   F I L T E R   D A T A B A S E   M O D U L E   --\n-----------------------------------------------------\n");
                            var keys = FilterDB.Keys.ToList();
                            keys.Sort();
                            builder.Append("local FilterDB = FilterDB; for key,value in pairs({").AppendLine();
                            foreach (var key in keys)
                            {
                                Dictionary<string, object> filterData = FilterDB[key];
                                builder.Append("\t[").Append(key).AppendLine("] = {");

                                // Attempt to get the text locale data object.
                                filterData.TryGetValue("text", out object textLocaleObject);
                                Dictionary<string, object> textLocales = textLocaleObject as Dictionary<string, object>;

                                // Export the "readable" field. (database only, not exported to game)
                                if (filterData.TryGetValue("readable", out string treadable))
                                {
                                    builder.Append("\t\treadable = ");
                                    ExportStringValue(builder, treadable).AppendLine(",");
                                }
                                else if (textLocales != null && textLocales.TryGetValue("en", out string name))
                                {
                                    builder.Append("\t\treadable = ");
                                    ExportStringValue(builder, name).AppendLine(",");
                                }

                                // Export the "icon" field.
                                if (filterData.TryGetValue("icon", out string icon))
                                {
                                    builder.Append("\t\ticon = ");
                                    ExportStringValue(builder, icon.Replace("\\", "/")).AppendLine(",");
                                }

                                // Export the complex "text" locales field.
                                if (textLocales != null)
                                {
                                    // Sort and then ensure es comes after en, to match previous convention.
                                    var supportedLocales = textLocales.Keys.ToList();
                                    supportedLocales.Sort();
                                    if (supportedLocales.Contains("es"))
                                    {
                                        supportedLocales.Remove("es");
                                        supportedLocales.Insert(0, "es");
                                    }
                                    if (supportedLocales.Contains("en"))
                                    {
                                        supportedLocales.Remove("en");
                                        supportedLocales.Insert(0, "en");
                                    }
                                    if (supportedLocales.Contains("ko"))
                                    {
                                        supportedLocales.Remove("ko");
                                        supportedLocales.Add("ko");
                                    }
                                    if (supportedLocales.Contains("cn"))
                                    {
                                        supportedLocales.Remove("cn");
                                        supportedLocales.Add("cn");
                                    }

                                    builder.AppendLine("\t\ttext = {");
                                    foreach (var localeKey in supportedLocales)
                                    {
                                        builder.Append("\t\t\t").Append(localeKey).Append(" = ");
                                        ExportStringValue(builder, textLocales[localeKey].ToString()).AppendLine(",");
                                    }
                                    builder.AppendLine("\t\t},");
                                }
                                builder.AppendLine("\t},");
                            }
                            builder.AppendLine("})").AppendLine("do FilterDB[key] = value; end");
                            File.WriteAllText(Path.Combine(debugFolder.FullName, "FilterDB.lua"), builder.ToString(), Encoding.UTF8);
                        }

                        // Export the Flight Paths DB file.
                        if (FlightPathDB.Any())
                        {
                            // Export the new format.
                            var builder = new StringBuilder("-----------------------------------------------------\n--   F L I G H T   P A T H S   D A T A B A S E   M O D U L E   --\n-----------------------------------------------------\n");
                            var keys = FlightPathDB.Keys.ToList();
                            keys.Sort();
                            builder.Append("local FlightPathDB = FlightPathDB; for key,value in pairs({").AppendLine();
                            foreach (var key in keys)
                            {
                                Dictionary<string, object> flightPathData = FlightPathDB[key];
                                builder.Append("\t[").Append(key).AppendLine("] = {");

                                // Attempt to get the text locale data object.
                                flightPathData.TryGetValue("text", out object textLocaleObject);
                                Dictionary<string, object> textLocales = textLocaleObject as Dictionary<string, object>;

                                // Export the complex "text" locales field.
                                if (textLocales != null)
                                {
                                    // Sort and then ensure es comes after en, to match previous convention.
                                    var supportedLocales = textLocales.Keys.ToList();
                                    supportedLocales.Sort();
                                    if (supportedLocales.Contains("es"))
                                    {
                                        supportedLocales.Remove("es");
                                        supportedLocales.Insert(0, "es");
                                    }
                                    if (supportedLocales.Contains("en"))
                                    {
                                        supportedLocales.Remove("en");
                                        supportedLocales.Insert(0, "en");
                                    }
                                    if (supportedLocales.Contains("ko"))
                                    {
                                        supportedLocales.Remove("ko");
                                        supportedLocales.Add("ko");
                                    }
                                    if (supportedLocales.Contains("cn"))
                                    {
                                        supportedLocales.Remove("cn");
                                        supportedLocales.Add("cn");
                                    }

                                    builder.AppendLine("\t\ttext = {");
                                    foreach (var localeKey in supportedLocales)
                                    {
                                        builder.Append("\t\t\t").Append(localeKey).Append(" = ");
                                        ExportStringValue(builder, textLocales[localeKey].ToString()).AppendLine(",");
                                    }
                                    builder.AppendLine("\t\t},");
                                }
                                builder.AppendLine("\t},");
                            }
                            builder.AppendLine("})").AppendLine("do FlightPathDB[key] = value; end");
                            File.WriteAllText(Path.Combine(debugFolder.FullName, "FlightPathDB.lua"), builder.ToString(), Encoding.UTF8);
                        }

                        // Export the Object DB file.
                        if (ObjectDB.Any())
                        {
                            // Export the new format.
                            var builder = new StringBuilder("-----------------------------------------------------\n--   O B J E C T   D A T A B A S E   M O D U L E   --\n-----------------------------------------------------\n");
                            var keys = ObjectDB.Keys.ToList();
                            keys.Sort();
                            builder.Append("local ObjectDB = ObjectDB; for objectID,objectData in pairs({").AppendLine();
                            foreach (var key in keys)
                            {
                                Dictionary<string, object> objectData = ObjectDB[key];
                                builder.Append("\t[").Append(key).AppendLine("] = {");

                                // Attempt to get the text locale data object.
                                objectData.TryGetValue("text", out object textLocaleObject);
                                Dictionary<string, object> textLocales = textLocaleObject as Dictionary<string, object>;

                                // Export the "readable" field. (database only, not exported to game)
                                if (objectData.TryGetValue("readable", out string treadable))
                                {
                                    builder.Append("\t\treadable = ");
                                    ExportStringValue(builder, treadable).AppendLine(",");
                                }
                                else if (textLocales != null && textLocales.TryGetValue("en", out string name))
                                {
                                    builder.Append("\t\treadable = ");
                                    ExportStringValue(builder, name).AppendLine(",");
                                }

                                // Export the "icon" field.
                                if (objectData.TryGetValue("icon", out string icon))
                                {
                                    builder.Append("\t\ticon = ");
                                    ExportStringValue(builder, icon.Replace("\\", "/")).AppendLine(",");
                                }

                                // Export the "model" field.
                                if (objectData.TryGetValue("model", out long model))
                                {
                                    builder.Append("\t\tmodel = ").Append(model).AppendLine(",");
                                }

                                // Export the complex "text" locales field.
                                if (textLocales != null)
                                {
                                    // Sort and then ensure es comes after en, to match previous convention.
                                    var supportedLocales = textLocales.Keys.ToList();
                                    supportedLocales.Sort();
                                    if (supportedLocales.Contains("es"))
                                    {
                                        supportedLocales.Remove("es");
                                        supportedLocales.Insert(0, "es");
                                    }
                                    if (supportedLocales.Contains("en"))
                                    {
                                        supportedLocales.Remove("en");
                                        supportedLocales.Insert(0, "en");
                                    }
                                    if (supportedLocales.Contains("ko"))
                                    {
                                        supportedLocales.Remove("ko");
                                        supportedLocales.Add("ko");
                                    }
                                    if (supportedLocales.Contains("cn"))
                                    {
                                        supportedLocales.Remove("cn");
                                        supportedLocales.Add("cn");
                                    }

                                    builder.AppendLine("\t\ttext = {");
                                    foreach (var localeKey in supportedLocales)
                                    {
                                        builder.Append("\t\t\t").Append(localeKey).Append(" = ");
                                        ExportStringValue(builder, textLocales[localeKey].ToString()).AppendLine(",");
                                    }
                                    builder.AppendLine("\t\t},");
                                }
                                builder.AppendLine("\t},");
                            }
                            builder.AppendLine("})").AppendLine("do ObjectDB[objectID] = objectData; end");
                            File.WriteAllText(Path.Combine(debugFolder.FullName, "ObjectDB.lua"), builder.ToString(), Encoding.UTF8);
                        }

                        // Export the Mount DB file.
                        var mounts = Items.AllIDs;
                        if (mounts.Any())
                        {
                            var builder = new StringBuilder("-----------------------------------------------------\n--   M O U N T   D A T A B A S E   M O D U L E   --\n-----------------------------------------------------\n");
                            var keys = mounts.ToList();
                            keys.Sort();
                            foreach (var itemID in keys)
                            {
                                var item = Items.GetNull(itemID);
                                if (item != null)
                                {
                                    if (item.TryGetValue("mountID", out long spellID))
                                    {
                                        builder.Append("i(").Append(itemID).Append(", ").Append(spellID).Append(");");
                                        if (item != null && item.TryGetValue("name", out string name)) builder.Append("\t-- ").Append(name);
                                        builder.AppendLine();
                                    }
                                    else if (item.TryGetValue("f", out long f) && f == 100)
                                    {
                                        builder.Append("i(").Append(itemID);
                                        if (item.TryGetValue("spellID", out spellID)) builder.Append(", ").Append(spellID);
                                        builder.Append(");");
                                        if (item != null && item.TryGetValue("name", out string name)) builder.Append("\t-- ").Append(name);
                                        builder.AppendLine();
                                    }
                                }
                            }
                            File.WriteAllText(Path.Combine(debugFolder.FullName, "RawMountDB.lua"), builder.ToString(), Encoding.UTF8);
                        }
                    }
                }

                // Prepare a Localization Database file.
                StringBuilder localizationDatabase = new StringBuilder()
                    .AppendLine("-----------------------------------------------------------------")
                    .AppendLine("--   L O C A L I Z A T I O N   D A T A B A S E   M O D U L E   --")
                    .AppendLine("-----------------------------------------------------------------")
                    .AppendLine("local localize = function(t, data) for k,v in pairs(data) do t[k] = v; end end")
                    .AppendLine("local appName, _, a = ...;")
                    .AppendLine("local L = _.L;").AppendLine();
                Dictionary<string, StringBuilder> localizationByLocale = new Dictionary<string, StringBuilder>();
                foreach(var language in new List<string>
                {
                    // 8 non-english locales, 9 supported in all. (English is written right away and acts as the default)
                    "es", "de", "fr", "it",
                    "pt", "ru", "ko", "zh", // NOTE: "cn" is not valid, it's actually "zh"!
                })
                {
                    // Generate a string builder for each language. (an empty builder at the end will not be exported)
                    localizationByLocale[language] = new StringBuilder();
                }


                // Export the Category DB file.
                if (CATEGORIES_WITH_REFERENCES.Any())
                {
                    CurrentParseStage = ParseStage.ExportCategoryDB;
                    var builder = new StringBuilder("-- Category Database Module").AppendLine();
                    var icons = new Dictionary<long, string>();
                    var localizationForText = new Dictionary<string, Dictionary<long, string>>();

                    // Include Only Referenced Objects!
                    var keys = CATEGORIES_WITH_REFERENCES.Keys.ToList();
                    keys.Sort();
                    foreach (var key in keys)
                    {
                        // Check to see if CategoryDB has any information on our category.
                        if (!CategoryDB.TryGetValue(key, out Dictionary<string, object> categoryData))
                        {
                            Console.Write("Missing Category information for ");
                            Console.WriteLine(key);
                            continue;
                        }

                        if (categoryData.TryGetValue("icon", out object value))
                        {
                            icons[key] = value.ToString().Replace("\\", "/");
                        }
                        if (categoryData.TryGetValue("text", out value))
                        {
                            if (!(value is IDictionary<string, object> localeData))
                            {
                                localeData = new Dictionary<string, object>
                                {
                                    ["en"] = value
                                };
                            }
                            if (localeData.TryGetValue("en", out string englishValue))
                            {
                                if (!localizationForText.TryGetValue("en", out Dictionary<long, string> sublocale))
                                {
                                    localizationForText["en"] = sublocale = new Dictionary<long, string>();
                                }
                                sublocale[key] = englishValue;

                                foreach (var locale in localeData)
                                {
                                    if (locale.Key == "en") continue;

                                    string localizedValue = locale.Value.ToString();
                                    if (!localizedValue.Contains(englishValue))
                                    {
                                        if (!localizationForText.TryGetValue(locale.Key, out sublocale))
                                        {
                                            localizationForText[locale.Key] = sublocale = new Dictionary<long, string>();
                                        }
                                        sublocale[key] = localizedValue;
                                    }
                                }
                            }
                        }
                    }

                    // Convert all "cn" into "zh" dictionaries, it makes the comparison later easier.
                    if (localizationForText.TryGetValue("cn", out Dictionary<long, string> data))
                    {
                        localizationForText.Remove("cn");
                        if (!localizationForText.TryGetValue("zh", out Dictionary<long, string> zh))
                        {
                            localizationForText["zh"] = data;
                        }
                        else
                        {
                            foreach (var pair in data)
                            {
                                zh[pair.Key] = pair.Value;
                            }
                        }
                    }

                    // Get all of the english translations and always write them to the file.
                    if (localizationForText.TryGetValue("en", out data))
                    {
                        localizationForText.Remove("en");
                        builder.AppendLine("_.CategoryNames = {");
                        foreach (var key in keys)
                        {
                            if (data.TryGetValue(key, out string name))
                            {
                                ExportStringKeyValue(builder, key, name).AppendLine();
                            }
                        }
                        builder.AppendLine("}");
                    }

                    // Now grab the non-english localizations and conditionally write them to the file.
                    foreach (var localePair in localizationForText)
                    {
                        if (localePair.Value.Any())
                        {
                            var localeBuilder = localizationByLocale[localePair.Key];
                            localeBuilder.AppendLine("localize(_.CategoryNames, {");
                            foreach (var key in keys)
                            {
                                if (localePair.Value.TryGetValue(key, out string name))
                                {
                                    ExportStringKeyValue(localeBuilder, key, name).AppendLine();
                                }
                            }
                            localeBuilder.AppendLine("});");
                        }
                    }

                    // Now write the icons last.
                    builder.AppendLine("_.CategoryIcons = {");
                    foreach (var key in keys)
                    {
                        if (icons.TryGetValue(key, out string icon))
                        {
                            ExportStringKeyValue(builder, key, icon).AppendLine();
                        }
                    }
                    builder.AppendLine("}");

                    // Append the file content to our localization database.
                    localizationDatabase.AppendLine(builder.ToString());
                }

                // Export the Custom Headers file.
                if (CustomHeaders != null && CustomHeaders.Any())
                {
                    CurrentParseStage = ParseStage.ExportCustomHeaders;

                    // Now export it based on what we know.
                    var builder = new StringBuilder("-- Custom Header Database Module").AppendLine();
                    var keys = new List<long>();
                    var eventIDs = new Dictionary<long, long>();
                    var eventRemaps = new Dictionary<long, long>();
                    var eventSchedules = new Dictionary<long, string>();
                    var icons = new Dictionary<long, string>();
                    var constants = new Dictionary<string, long>();
                    var localizationForText = new Dictionary<string, Dictionary<long, string>>();
                    var localizationForLore = new Dictionary<string, Dictionary<long, string>>();
                    var localizationForDescriptions = new Dictionary<string, Dictionary<long, string>>();
                    foreach (var key in CustomHeaders.Keys)
                    {
                        // Include Only Referenced Headers!
                        if (CUSTOM_HEADERS_WITH_REFERENCES.ContainsKey(key))
                        {
                            if (CustomHeaders.TryGetValue(key, out object o) && o is IDictionary<string, object> header)
                            {
                                keys.Add(key);
                                if (header.TryGetValue("eventID", out object value))
                                {
                                    long eventID = Convert.ToInt64(value);
                                    eventIDs[key] = eventID;
                                    if (header.TryGetValue("eventIDs", out value) && value is List<object> ids)
                                    {
                                        foreach (var eventIDAsObj in ids)
                                        {
                                            eventRemaps[Convert.ToInt64(eventIDAsObj)] = eventID;
                                        }
                                    }
                                    if (header.TryGetValue("eventSchedule", out value))
                                    {
                                        eventSchedules[eventID] = value.ToString();
                                    }
                                }
                                if (header.TryGetValue("icon", out value))
                                {
                                    icons[key] = value.ToString().Replace("\\", "/");
                                }
                                if (header.TryGetValue("constant", out value))
                                {
                                    constants[value.ToString()] = key;
                                }
                                if (header.TryGetValue("text", out value))
                                {
                                    if (!(value is IDictionary<string, object> localeData))
                                    {
                                        localeData = new Dictionary<string, object>
                                        {
                                            ["en"] = value
                                        };
                                    }
                                    foreach (var locale in localeData)
                                    {
                                        if (!localizationForText.TryGetValue(locale.Key, out Dictionary<long, string> sublocale))
                                        {
                                            localizationForText[locale.Key] = sublocale = new Dictionary<long, string>();
                                        }
                                        sublocale[key] = locale.Value.ToString();
                                    }
                                }
                                if (header.TryGetValue("description", out value))
                                {
                                    if (!(value is IDictionary<string, object> localeData))
                                    {
                                        localeData = new Dictionary<string, object>
                                        {
                                            ["en"] = value
                                        };
                                    }
                                    foreach (var locale in localeData)
                                    {
                                        if (!localizationForDescriptions.TryGetValue(locale.Key, out Dictionary<long, string> sublocale))
                                        {
                                            localizationForDescriptions[locale.Key] = sublocale = new Dictionary<long, string>();
                                        }
                                        sublocale[key] = locale.Value.ToString();
                                    }
                                }
                                if (header.TryGetValue("lore", out value))
                                {
                                    if (!(value is IDictionary<string, object> localeData))
                                    {
                                        localeData = new Dictionary<string, object>
                                        {
                                            ["en"] = value
                                        };
                                    }
                                    foreach (var locale in localeData)
                                    {
                                        if (!localizationForLore.TryGetValue(locale.Key, out Dictionary<long, string> sublocale))
                                        {
                                            localizationForLore[locale.Key] = sublocale = new Dictionary<long, string>();
                                        }
                                        sublocale[key] = locale.Value.ToString();
                                    }
                                }
                            }
                        }
                    }
                    keys.Sort(new Comparison<long>((i1, i2) => i2.CompareTo(i1)));

                    // Write the header constants!
                    builder.AppendLine("_.HeaderConstants = {");
                    var headerKeys = constants.Keys.ToList();
                    headerKeys.Sort(StringComparer.InvariantCulture);
                    foreach (var key in headerKeys)
                    {
                        builder.Append("\t").Append(key).Append(" = ").Append(constants[key]).AppendLine(",");
                    }
                    builder.AppendLine("};");

                    // Convert all "cn" into "zh" dictionaries, it makes the comparison later easier.
                    if (localizationForText.TryGetValue("cn", out Dictionary<long, string> data))
                    {
                        localizationForText.Remove("cn");
                        if (!localizationForText.TryGetValue("zh", out Dictionary<long, string> zh))
                        {
                            localizationForText["zh"] = data;
                        }
                        else
                        {
                            foreach (var pair in data)
                            {
                                zh[pair.Key] = pair.Value;
                            }
                        }
                    }
                    if (localizationForDescriptions.TryGetValue("cn", out data))
                    {
                        localizationForDescriptions.Remove("cn");
                        if (!localizationForDescriptions.TryGetValue("zh", out Dictionary<long, string> zh))
                        {
                            localizationForDescriptions["zh"] = data;
                        }
                        else
                        {
                            foreach (var pair in data)
                            {
                                zh[pair.Key] = pair.Value;
                            }
                        }
                    }
                    if (localizationForLore.TryGetValue("cn", out data))
                    {
                        localizationForLore.Remove("cn");
                        if (!localizationForLore.TryGetValue("zh", out Dictionary<long, string> zh))
                        {
                            localizationForLore["zh"] = data;
                        }
                        else
                        {
                            foreach (var pair in data)
                            {
                                zh[pair.Key] = pair.Value;
                            }
                        }
                    }

                    // Get all of the english translations and always write them to the file.
                    if (localizationForText.TryGetValue("en", out data))
                    {
                        localizationForText.Remove("en");
                        builder.AppendLine("localize(L.HEADER_NAMES, {");
                        foreach (var key in keys)
                        {
                            if (data.TryGetValue(key, out string name))
                            {
                                ExportStringKeyValue(builder, key, name).AppendLine();
                            }
                        }
                        builder.AppendLine("});");
                    }
                    if (localizationForDescriptions.TryGetValue("en", out data))
                    {
                        localizationForDescriptions.Remove("en");
                        builder.AppendLine("localize(L.HEADER_DESCRIPTIONS, {");
                        foreach (var key in keys)
                        {
                            if (data.TryGetValue(key, out string name))
                            {
                                ExportStringKeyValue(builder, key, name).AppendLine();
                            }
                        }
                        builder.AppendLine("});");
                    }
                    if (localizationForLore.TryGetValue("en", out data))
                    {
                        localizationForLore.Remove("en");
                        builder.AppendLine("localize(L.HEADER_LORE, {");
                        foreach (var key in keys)
                        {
                            if (data.TryGetValue(key, out string name))
                            {
                                ExportStringKeyValue(builder, key, name).AppendLine();
                            }
                        }
                        builder.AppendLine("});");
                    }

                    // Write the icons last.
                    builder.AppendLine("localize(L.HEADER_ICONS, {");
                    foreach (var key in keys)
                    {
                        if (icons.TryGetValue(key, out string icon))
                        {
                            ExportStringKeyValue(builder, key, icon).AppendLine();
                        }
                    }
                    builder.AppendLine("});");

                    // Write the event information!
                    if (eventIDs.Any())
                    {
                        builder.AppendLine("localize(L.HEADER_EVENTS, {");
                        foreach (var key in keys)
                        {
                            if (eventIDs.TryGetValue(key, out long eventID))
                            {
                                ExportObjectKeyValue(builder, key, eventID).AppendLine();
                            }
                        }
                        builder.AppendLine("});");
                    }
                    if (eventRemaps.Any())
                    {
                        builder.AppendLine("localize(L.EVENT_REMAPPING, {");
                        foreach (var pair in eventRemaps)
                        {
                            ExportObjectKeyValue(builder, pair.Key, pair.Value).AppendLine();
                        }
                        builder.AppendLine("});").AppendLine();
                    }
                    if (eventSchedules.Any())
                    {
                        builder.AppendLine("-- Programmatic Event Scheduling");
                        foreach (var pair in eventSchedules)
                        {
                            builder.Append("_.Modules.Events.SetEventInformation(").Append(pair.Key).Append(", ").Append(pair.Value).Append(");").AppendLine();
                        }
                    }

                    // Now grab the non-english localizations and conditionally write them to the file.
                    foreach (var localePair in localizationForText)
                    {
                        if (localePair.Value.Any())
                        {
                            var localeBuilder = localizationByLocale[localePair.Key];
                            localeBuilder.AppendLine("localize(L.HEADER_NAMES, {");
                            foreach (var key in keys)
                            {
                                if (localePair.Value.TryGetValue(key, out string name))
                                {
                                    ExportStringKeyValue(localeBuilder, key, name).AppendLine();
                                }
                            }
                            localeBuilder.AppendLine("});");
                        }
                    }
                    foreach (var localePair in localizationForDescriptions)
                    {
                        if (localePair.Value.Any())
                        {
                            var localeBuilder = localizationByLocale[localePair.Key];
                            localeBuilder.AppendLine("localize(L.HEADER_DESCRIPTIONS, {");
                            foreach (var key in keys)
                            {
                                if (localePair.Value.TryGetValue(key, out string name))
                                {
                                    ExportStringKeyValue(localeBuilder, key, name).AppendLine();
                                }
                            }
                            localeBuilder.AppendLine("});");
                        }
                    }
                    foreach (var localePair in localizationForLore)
                    {
                        if (localePair.Value.Any())
                        {
                            var localeBuilder = localizationByLocale[localePair.Key];
                            localeBuilder.AppendLine("localize(L.HEADER_LORE, {");
                            foreach (var key in keys)
                            {
                                if (localePair.Value.TryGetValue(key, out string name))
                                {
                                    ExportStringKeyValue(localeBuilder, key, name).AppendLine();
                                }
                            }
                            localeBuilder.AppendLine("});");
                        }
                    }

                    // Append the file content to our localization database.
                    localizationDatabase.AppendLine(builder.ToString());
                }

                // Export the Filter DB file.
                if (FILTERS_WITH_REFERENCES.Any())
                {
                    CurrentParseStage = ParseStage.ExportFilterDB;
                    var builder = new StringBuilder("-- Filter Database Module").AppendLine();

                    var icons = new Dictionary<long, string>();
                    var constants = new Dictionary<string, long>();
                    var localizationForText = new Dictionary<string, Dictionary<long, string>>();

                    // Include Only Referenced data!
                    var keys = FILTERS_WITH_REFERENCES.Keys.ToList();
                    keys.Sort();
                    foreach (var key in keys)
                    {
                        // Check to see if DB has any information for this id.
                        if (!FilterDB.TryGetValue(key, out Dictionary<string, object> dataEntry))
                        {
                            // If not, report that it is missing.
                            Console.Write("Missing Filter data for #");
                            Console.WriteLine(key);
                            continue;
                        }
                        if (dataEntry.TryGetValue("text", out object value))
                        {
                            if (!(value is IDictionary<string, object> localeData))
                            {
                                localeData = new Dictionary<string, object>
                                {
                                    ["en"] = value
                                };
                            }
                            if (localeData.TryGetValue("en", out string englishValue))
                            {
                                if (!localizationForText.TryGetValue("en", out Dictionary<long, string> sublocale))
                                {
                                    localizationForText["en"] = sublocale = new Dictionary<long, string>();
                                }
                                sublocale[key] = englishValue;

                                foreach (var locale in localeData)
                                {
                                    if (locale.Key == "en") continue;

                                    string localizedValue = locale.Value.ToString();
                                    if (!localizedValue.Contains(englishValue))
                                    {
                                        if (!localizationForText.TryGetValue(locale.Key, out sublocale))
                                        {
                                            localizationForText[locale.Key] = sublocale = new Dictionary<long, string>();
                                        }
                                        sublocale[key] = localizedValue;
                                    }
                                }
                            }
                        }
                        if (dataEntry.TryGetValue("icon", out object icon))
                        {
                            icons[key] = icon.ToString().Replace("\\", "/");
                        }

                        if (dataEntry.TryGetValue("constant", out value))
                        {
                            constants[value.ToString()] = key;
                        }
                    }

                    // Write the header constants!
                    if (constants.Any())
                    {
                        builder.AppendLine("_.FilterConstants = {");
                        var headerKeys = constants.Keys.ToList();
                        headerKeys.Sort(StringComparer.InvariantCulture);
                        foreach (var key in headerKeys)
                        {
                            builder.Append("\t").Append(key).Append(" = ").Append(constants[key]).AppendLine(",");
                        }
                        builder.AppendLine("};");
                    }

                    // Convert all "cn" into "zh" dictionaries, it makes the comparison later easier.
                    if (localizationForText.TryGetValue("cn", out Dictionary<long, string> data))
                    {
                        localizationForText.Remove("cn");
                        if (!localizationForText.TryGetValue("zh", out Dictionary<long, string> zh))
                        {
                            localizationForText["zh"] = data;
                        }
                        else
                        {
                            foreach (var pair in data)
                            {
                                zh[pair.Key] = pair.Value;
                            }
                        }
                    }

                    // Get all of the english translations and always write them to the file.
                    if (localizationForText.TryGetValue("en", out data))
                    {
                        localizationForText.Remove("en");
                        builder.AppendLine("L.FILTER_ID_TYPES = {");
                        foreach (var key in keys)
                        {
                            if (data.TryGetValue(key, out string name))
                            {
                                ExportStringKeyValue(builder, key, name).AppendLine();
                            }
                        }
                        builder.AppendLine("}");
                    }

                    // Now grab the non-english localizations and conditionally write them to the file.
                    foreach (var localePair in localizationForText)
                    {
                        if (localePair.Value.Any())
                        {
                            var localeBuilder = localizationByLocale[localePair.Key];
                            localeBuilder.AppendLine("localize(L.FILTER_ID_TYPES, {");
                            foreach (var key in keys)
                            {
                                if (localePair.Value.TryGetValue(key, out string name))
                                {
                                    ExportStringKeyValue(localeBuilder, key, name).AppendLine();
                                }
                            }
                            localeBuilder.AppendLine("});");
                        }
                    }

                    // Write the icons last.
                    if (icons.Any())
                    {
                        builder.AppendLine("L.FILTER_ID_ICONS = {");
                        foreach (var key in keys)
                        {
                            if (icons.TryGetValue(key, out string icon))
                            {
                                ExportStringKeyValue(builder, key, icon).AppendLine();
                            }
                        }
                        builder.AppendLine("}");
                    }

                    // Append the file content to our localization database.
                    localizationDatabase.AppendLine(builder.ToString());
                }

                // Export the Flight Paths DB file.
                if (FLIGHTPATHS_WITH_REFERENCES.Any())
                {
                    CurrentParseStage = ParseStage.ExportFlightPathDB;
                    var builder = new StringBuilder("-- Flight Path Database Module").AppendLine();

                    var localizationForText = new Dictionary<string, Dictionary<long, string>>();

                    // Include Only Referenced Flight Paths!
                    var keys = FLIGHTPATHS_WITH_REFERENCES.Keys.ToList();
                    keys.Sort();
                    foreach (var key in keys)
                    {
                        // Check to see if FlightPathDB has any information on our flight path.
                        if (!FlightPathDB.TryGetValue(key, out Dictionary<string, object> flightPathData))
                        {
                            // If not, report that it is missing.
                            Console.Write("Missing Flight Path data for #");
                            Console.WriteLine(key);
                            continue;
                        }
                        if (flightPathData.TryGetValue("text", out object value))
                        {
                            if (!(value is IDictionary<string, object> localeData))
                            {
                                localeData = new Dictionary<string, object>
                                {
                                    ["en"] = value
                                };
                            }
                            if (localeData.TryGetValue("en", out string englishValue))
                            {
                                if (!localizationForText.TryGetValue("en", out Dictionary<long, string> sublocale))
                                {
                                    localizationForText["en"] = sublocale = new Dictionary<long, string>();
                                }
                                sublocale[key] = englishValue;

                                foreach (var locale in localeData)
                                {
                                    if (locale.Key == "en") continue;

                                    string localizedValue = locale.Value.ToString();
                                    if (!localizedValue.Contains(englishValue))
                                    {
                                        if (!localizationForText.TryGetValue(locale.Key, out sublocale))
                                        {
                                            localizationForText[locale.Key] = sublocale = new Dictionary<long, string>();
                                        }
                                        sublocale[key] = localizedValue;
                                    }
                                }
                            }
                        }
                    }

                    // Convert all "cn" into "zh" dictionaries, it makes the comparison later easier.
                    if (localizationForText.TryGetValue("cn", out Dictionary<long, string> data))
                    {
                        localizationForText.Remove("cn");
                        if (!localizationForText.TryGetValue("zh", out Dictionary<long, string> zh))
                        {
                            localizationForText["zh"] = data;
                        }
                        else
                        {
                            foreach (var pair in data)
                            {
                                zh[pair.Key] = pair.Value;
                            }
                        }
                    }

                    // Get all of the english translations and always write them to the file.
                    if (localizationForText.TryGetValue("en", out data))
                    {
                        localizationForText.Remove("en");
                        builder.AppendLine("_.FlightPathNames = {");
                        foreach (var key in keys)
                        {
                            if (data.TryGetValue(key, out string name))
                            {
                                ExportStringKeyValue(builder, key, name).AppendLine();
                            }
                        }
                        builder.AppendLine("}");
                    }

                    // Now grab the non-english localizations and conditionally write them to the file.
                    foreach (var localePair in localizationForText)
                    {
                        if (localePair.Value.Any())
                        {
                            var localeBuilder = localizationByLocale[localePair.Key];
                            localeBuilder.AppendLine("localize(L.FlightPathNames, {");
                            foreach (var key in keys)
                            {
                                if (localePair.Value.TryGetValue(key, out string name))
                                {
                                    ExportStringKeyValue(localeBuilder, key, name).AppendLine();
                                }
                            }
                            localeBuilder.AppendLine("});");
                        }
                    }

                    // Append the file content to our localization database.
                    localizationDatabase.AppendLine(builder.ToString());
                }

                // Export the Object DB file.
                if (OBJECTS_WITH_REFERENCES.Any())
                {
                    CurrentParseStage = ParseStage.ExportObjectDB;
                    var builder = new StringBuilder("-- Object Database Module").AppendLine();

                    var icons = new Dictionary<long, string>();
                    var modelIDs = new Dictionary<long, long>();
                    var localizationForText = new Dictionary<string, Dictionary<long, string>>();

                    // Include Only Referenced Objects!
                    var keys = OBJECTS_WITH_REFERENCES.Keys.ToList();
                    keys.Sort();
                    foreach (var key in keys)
                    {
                        // Check to see if ObjectDB has any information on our object.
                        if (!ObjectDB.TryGetValue(key, out Dictionary<string, object> objectData))
                        {
                            // If not, get new object information from WoWHead.
                            objectData = new Dictionary<string, object>();
                            ObjectHarvester.UpdateInformationFromWoWHead(key, objectData);
                            if (!objectData.Any()) continue;
                            ObjectDB[key] = objectData;
                        }
#if HARVESTOBJECTS
                        else
                        {
                            // Check for any updated information from WoWHead.
                            ObjectHarvester.UpdateInformationFromWoWHead(key, objectData);
                        }
#endif

                        if (objectData.TryGetValue("icon", out object value))
                        {
                            icons[key] = value.ToString().Replace("\\", "/");
                        }
                        if (objectData.TryGetValue("model", out value))
                        {
                            modelIDs[key] = Convert.ToInt64(value);
                        }
                        if (objectData.TryGetValue("text", out value))
                        {
                            if (!(value is IDictionary<string, object> localeData))
                            {
                                localeData = new Dictionary<string, object>
                                {
                                    ["en"] = value
                                };
                            }
                            if (localeData.TryGetValue("en", out string englishValue))
                            {
                                if (!localizationForText.TryGetValue("en", out Dictionary<long, string> sublocale))
                                {
                                    localizationForText["en"] = sublocale = new Dictionary<long, string>();
                                }
                                sublocale[key] = englishValue;

                                foreach (var locale in localeData)
                                {
                                    if (locale.Key == "en") continue;

                                    string localizedValue = locale.Value.ToString();
                                    if (!localizedValue.Contains(englishValue))
                                    {
                                        if (!localizationForText.TryGetValue(locale.Key, out sublocale))
                                        {
                                            localizationForText[locale.Key] = sublocale = new Dictionary<long, string>();
                                        }
                                        sublocale[key] = localizedValue;
                                    }
                                }
                            }
                        }
                    }

                    // Convert all "cn" into "zh" dictionaries, it makes the comparison later easier.
                    if (localizationForText.TryGetValue("cn", out Dictionary<long, string> data))
                    {
                        localizationForText.Remove("cn");
                        if (!localizationForText.TryGetValue("zh", out Dictionary<long, string> zh))
                        {
                            localizationForText["zh"] = data;
                        }
                        else
                        {
                            foreach (var pair in data)
                            {
                                zh[pair.Key] = pair.Value;
                            }
                        }
                    }

                    // Get all of the english translations and always write them to the file.
                    if (localizationForText.TryGetValue("en", out data))
                    {
                        localizationForText.Remove("en");
                        builder.AppendLine("_.ObjectNames = {");
                        foreach (var key in keys)
                        {
                            if (data.TryGetValue(key, out string name))
                            {
                                ExportStringKeyValue(builder, key, name).AppendLine();
                            }
                        }
                        builder.AppendLine("}");
                    }

                    // Now grab the non-english localizations and conditionally write them to the file.
                    foreach (var localePair in localizationForText)
                    {
                        if (localePair.Value.Any())
                        {
                            var localeBuilder = localizationByLocale[localePair.Key];
                            localeBuilder.AppendLine("localize(L.ObjectNames, {");
                            foreach (var key in keys)
                            {
                                if (localePair.Value.TryGetValue(key, out string name))
                                {
                                    ExportStringKeyValue(localeBuilder, key, name).AppendLine();
                                }
                            }
                            localeBuilder.AppendLine("});");
                        }
                    }

                    // Now write the icons after the text.
                    builder.AppendLine("_.ObjectIcons = {");
                    foreach (var key in keys)
                    {
                        if (icons.TryGetValue(key, out string icon))
                        {
                            ExportStringKeyValue(builder, key, icon).AppendLine();
                        }
                    }
                    builder.AppendLine("}");

                    // Write the model information last.
                    builder.AppendLine("_.ObjectModels = {");
                    foreach (var key in keys)
                    {
                        if (modelIDs.TryGetValue(key, out long modelID))
                        {
                            ExportObjectKeyValue(builder, key, modelID).AppendLine();
                        }
                    }
                    builder.AppendLine("}");

                    // Append the file content to our localization database.
                    localizationDatabase.AppendLine(builder.ToString());
                }

                // Now write the localization for each locale to the localization database builder.
                var localeKeys = localizationByLocale.Keys.ToList();
                localeKeys.Sort();
                localizationDatabase.AppendLine("-- Supported Locales")
                    .AppendLine("local simplifiedLocale = string.sub(GetLocale(),1,2);");
                foreach (var localeKey in localeKeys)
                {
                    if (localizationByLocale.TryGetValue(localeKey, out StringBuilder builder) && builder.Length > 0)
                    {
                        localizationDatabase.Append("if simplifiedLocale == \"").Append(localeKey).AppendLine("\" then");
                        localizationDatabase.Append(builder.ToString());
                        localizationDatabase.AppendLine("end");
                    }
                }

                // Check to make sure the content is different since Diff tools are dumb as hell.
                var filename = Path.Combine(addonRootFolder, $"db/{dbRootFolder}LocalizationDB.lua");
                var localizationDatabaseContent = localizationDatabase.ToString().Replace("\r\n", "\n").Trim();
                if (!File.Exists(filename) || File.ReadAllText(filename, Encoding.UTF8).Replace("\r\n", "\n").Trim() != localizationDatabaseContent)
                {
                    File.WriteAllText(filename, localizationDatabaseContent, Encoding.UTF8);
                }

                CurrentParseStage = ParseStage.ExportAddonData;
                IncludeRawNewlines = false;
                Objects.Export(outputFolder.FullName);
                IncludeRawNewlines = true;

                CurrentParseStage = ParseStage.ExportAutoSources;
                Objects.ExportAutoItemSources(Config["root-data"] ?? "./DATAS");
                CurrentParseStage = ParseStage.ExportAutoLocale;
                Objects.ExportAutoLocale(Path.Combine(addonRootFolder, $"db/{dbRootFolder}en_auto.lua"));

                // Attempt to find some dirty objects and write them to a dynamic file.
                var dirtyObjectsFilePath = Path.Combine(Config["root-data"] ?? "./DATAS", "00 - DB/Dynamic/", $"DynamicObjectDB_{DateTime.UtcNow.Ticks}.lua");
                /*
                // This is the bulk harvester. It grabs aaaaaalll of them!
                for (int objectID = 111911; objectID > 163; --objectID)
                {
                    if (!ObjectDB.TryGetValue(objectID, out Dictionary<string, object> objectData))
                    {
                        // If not, get new object information from WoWHead.
                        objectData = new Dictionary<string, object>();
                        ObjectHarvester.UpdateInformationFromWoWHead(objectID, objectData);
                        if (!objectData.Any()) continue;
                        ObjectDB[objectID] = objectData;
                        ObjectHarvester.ExportDirtyObjectsToFilePath(dirtyObjectsFilePath);
                    }
                }
                */

                // Check to see if we need to export any dirty objects.
                ObjectHarvester.ExportDirtyObjectsToFilePath(dirtyObjectsFilePath);
            }
        }
    }
}
