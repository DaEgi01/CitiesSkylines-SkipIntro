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
            TranspileIntroScreensToEmptyStrings(_harmony);
        }

        private void TranspileIntroScreensToEmptyStrings(HarmonyInstance harmony)
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
            foreach (var codeInstruction in codeInstructions)
            {
                if (codeInstruction.opcode == OpCodes.Ldstr
                    && (
                        codeInstruction.operand as string == "IntroScreen"
                        || codeInstruction.operand as string == "IntroScreen2"
                    )
                )
                {
                    codeInstruction.operand = string.Empty;
                }

                if (codeInstruction.opcode == OpCodes.Ldc_R4 
                    && (
                        codeInstruction.operand as float? == 4f //wait time of IntroScreen and FadeImage
                        || codeInstruction.operand as float? == 1f //wait time of IntroScreen2
                        || codeInstruction.operand as float? == 20f //wait time for IsDLCStateReady and LegalDocumentsReady
                    )
                )
                {
                    codeInstruction.operand = 0f;
                }

                yield return codeInstruction;
            }
        }
    }
}
