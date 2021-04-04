using Harmony;
using ICities;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;

namespace SkipIntro
{
    public class Mod : IUserMod
    {
        private readonly string _harmonyId = "egi.citiesskylinesmods.skipintro";
        private HarmonyInstance _harmony;

        public string Name => "Skip Intro";
        public string Description => "Skips the two logos during game start.";

        public void OnEnabled()
        {
            var applicationStarter = GameObject.Find("Application Starter");
            if (applicationStarter == null)
            {
                return;
            }

            _harmony = HarmonyInstance.Create(_harmonyId);
            TranspileAwayTheIntroScreens(_harmony);
        }

        private void TranspileAwayTheIntroScreens(HarmonyInstance harmony)
        {
            var loadIntroCoroutineOriginal = typeof(LoadingManager)
                .GetNestedTypes(BindingFlags.NonPublic)
                .Single(x => x.FullName == "LoadingManager+<LoadIntroCoroutine>c__Iterator0")
                .GetMethod("MoveNext", BindingFlags.Public | BindingFlags.Instance);

            var loadIntroCoroutineTranspiler = typeof(Mod)
                .GetMethod(nameof(LoadIntroCoroutineTranspiler), BindingFlags.NonPublic | BindingFlags.Static);

            harmony.Patch(loadIntroCoroutineOriginal, null, null, new HarmonyMethod(loadIntroCoroutineTranspiler));
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

        private static IEnumerable<CodeInstruction> LoadIntroCoroutineTranspiler(IEnumerable<CodeInstruction> codeInstructions)
        {
            var menuSceneField = typeof(LoadingManager)
                .GetNestedTypes(BindingFlags.NonPublic)
                .Single(x => x.FullName == "LoadingManager+<LoadIntroCoroutine>c__Iterator0")
                .GetField("<menuScene>__0", BindingFlags.NonPublic | BindingFlags.Instance);

            var codes = codeInstructions.ToList();

            for (int i = 0; i < codes.Count; i++)
            {
                var code = codes[i];

                if (code.opcode == OpCodes.Ldstr
                    && (
                        code.operand as string == "IntroScreen"
                        || code.operand as string == "IntroScreen2"
                    )
                )
                {
                    code.operand = string.Empty;
                }

                if (code.opcode == OpCodes.Ldc_R4
                    && (
                        code.operand as float? == 4f //wait time of IntroScreen and FadeImage
                        || code.operand as float? == 1f //wait time of IntroScreen2
                        || code.operand as float? == 20f //wait time for IsDLCStateReady and LegalDocumentsReady
                    )
                )
                {
                    code.operand = 0f;
                }

                /*
                Replace
                    yield return SceneManager.LoadSceneAsync(menuScene, LoadSceneMode.Additive);
                with
                    yield return SceneManager.LoadSceneAsync(menuScene, LoadSceneMode.Single);
                Ensures that SceneManager.GetActiveScene() reports "MainMenu" instead of "Startup".
                Default is "IntroScreen" due to 'LoadSceneMode.Additive' but mods that use GetActiveScene() should check
                for "IntroScreen" || "MainMenu" anyway since its "MainMenu" if you go from game back to main menu.
                */
                if (code.opcode == OpCodes.Ldarg_0
                    && codes[i + 1].opcode == OpCodes.Ldarg_0
                    && codes[i + 2].opcode == OpCodes.Ldfld && codes[i + 2].operand == menuSceneField
                    && codes[i + 3].opcode == OpCodes.Ldc_I4_1)
                {
                    codes[i + 3].opcode = OpCodes.Ldc_I4_0;
                }
            }

            return codes;
        }
    }
}
