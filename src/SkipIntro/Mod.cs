using CitiesHarmony.API;
using ICities;
using UnityEngine;

namespace SkipIntro
{
	public class Mod : IUserMod
	{
		public string Name => "Skip Intro";
		public string Description => "Skips the two logos during game start.";

		public void OnEnabled()
		{
			var applicationStarter = GameObject.Find("Application Starter");
			if (applicationStarter == null)
			{
				return;
			}

			HarmonyHelper.DoOnHarmonyReady(() => Patcher.PatchAll());
		}

		public void OnDisabled()
		{
			if (!HarmonyHelper.IsHarmonyInstalled)
				return;

			Patcher.UnpatchAll();
		}
	}
}
