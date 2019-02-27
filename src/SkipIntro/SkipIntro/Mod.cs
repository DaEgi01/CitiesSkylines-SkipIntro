using Harmony;
using ICities;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SkipIntro
{
    public class Mod : IUserMod
    {
        private readonly string _harmonyId = "egi.citiesskylinesmods.skipintro";
        private HarmonyInstance _harmony;

        public string Name => "Skip Intro";
        public string Description => "Skips the intro during game start.";

        public void OnEnabled()
        {
            var applicationStarter = GameObject.Find("Application Starter");
            if (applicationStarter == null)
            {
                return;
            }

            var loadIntroOriginal = typeof(LoadingManager).GetMethod("LoadIntro", BindingFlags.Public | BindingFlags.Instance);
            var loadIntroPrefix = typeof(Mod).GetMethod(nameof(LoadIntroPrefix), BindingFlags.Public | BindingFlags.Static);

            _harmony = HarmonyInstance.Create(_harmonyId);
            _harmony.Patch(loadIntroOriginal, new HarmonyMethod(loadIntroPrefix));
        }

        public void OnDisabled()
        {
            if (_harmony == null)
            {
                return;
            }

            _harmony.UnpatchAll(_harmonyId);
            _harmony = null;
        }

        public static bool LoadIntroPrefix()
        {
            SceneManager.LoadScene("MainMenu", LoadSceneMode.Single);

            return false;
        }
    }
}
