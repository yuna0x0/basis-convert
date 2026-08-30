using GatorDragonGames.JigglePhysics;
using UnityEditor;
using UnityEngine;
using yuna0x0.Basis.Convert.Model;

namespace yuna0x0.Basis.Convert.Writers
{
    /// <summary>
    /// Loads the preset rigs shipped with the jiggle package, so a converted rig starts from
    /// values tuned by that package's author and the converter only overwrites what the source
    /// data actually determines.
    /// </summary>
    public static class JigglePresetLibrary
    {
        private const string PresetFolder =
            "Packages/com.gator-dragon-games.jigglephysics/Presets/";

        public static string PathFor(JigglePreset preset)
        {
            string name = preset switch
            {
                JigglePreset.Tail => "JiggleTail",
                JigglePreset.Breasts => "JiggleBreasts",
                JigglePreset.Rope => "JiggleRope",
                _ => "JiggleHair",
            };

            return PresetFolder + name + ".prefab";
        }

        /// <summary>
        /// The preset's rig, or null when the jiggle package has moved its presets. A missing
        /// preset is not fatal: the rig then starts from jiggle's own defaults instead.
        /// </summary>
        public static JiggleRig TryLoad(JigglePreset preset)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PathFor(preset));
            if (prefab == null)
            {
                return null;
            }

            return prefab.GetComponentInChildren<JiggleRig>(true);
        }

        /// <summary>
        /// Picks a preset from a bone's name. A guess, and overridable before conversion runs.
        /// </summary>
        public static JigglePreset GuessFrom(string boneName)
        {
            if (string.IsNullOrEmpty(boneName))
            {
                return JigglePreset.Hair;
            }

            string name = boneName.ToLowerInvariant();

            if (Contains(name, "breast", "boob", "bust", "chest", "oppai", "mune"))
            {
                return JigglePreset.Breasts;
            }

            if (Contains(name, "tail", "shippo"))
            {
                return JigglePreset.Tail;
            }

            if (Contains(name, "rope", "chain", "cord", "string", "strap"))
            {
                return JigglePreset.Rope;
            }

            return JigglePreset.Hair;
        }

        private static bool Contains(string haystack, params string[] needles)
        {
            foreach (string needle in needles)
            {
                if (haystack.Contains(needle))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
