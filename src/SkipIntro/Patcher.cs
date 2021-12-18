using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace SkipIntro
{
	public static class Patcher
	{
		private const string _harmonyId = "egi.citiesskylinesmods.skipintro";
		private static bool _patched = false;

		public static void PatchAll()
		{
			if (_patched)
				return;

			var harmony = new Harmony(_harmonyId);

			var loadIntroCoroutineOriginal = typeof(LoadingManager)
				.GetNestedTypes(BindingFlags.NonPublic)
				.Single(x => x.FullName == "LoadingManager+<LoadIntroCoroutine>c__Iterator0")
				.GetMethod("MoveNext", BindingFlags.Public | BindingFlags.Instance);

			var loadIntroCoroutineTranspiler = typeof(Patcher)
				.GetMethod(nameof(LoadIntroCoroutineTranspiler), BindingFlags.NonPublic | BindingFlags.Static);

			harmony.Patch(loadIntroCoroutineOriginal, null, null, new HarmonyMethod(loadIntroCoroutineTranspiler));

			_patched = true;
		}

		public static void UnpatchAll()
		{
			if (!_patched)
				return;

			var harmony = new Harmony(_harmonyId);
			harmony.UnpatchAll(_harmonyId);

			_patched = false;
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
