using System;

using MonoMod.ModInterop;

using UnityEngine.SceneManagement;

namespace FiveKnights
{
	internal static class GSPImport
	{
		public static void AddFastDashPredicate(Func<Scene, Scene, bool> predicate)
		{
			GodSeekerPlus.AddFastDashPredicate?.Invoke(predicate);
		}

		public static void AddInfiniteChallengeReturnScenePredicate(Func<GameManager.SceneLoadInfo, bool> predicate) {
			GodSeekerPlus.AddInfiniteChallengeReturnScenePredicate?.Invoke(predicate);
		}

		static GSPImport()
		{
			typeof(GodSeekerPlus).ModInterop();
		}

		[ModImportName(nameof(GodSeekerPlus))]
		internal static class GodSeekerPlus
		{
			public static Action<Func<Scene, Scene, bool>> AddFastDashPredicate;
			public static Action<Func<GameManager.SceneLoadInfo, bool>> AddInfiniteChallengeReturnScenePredicate;
		}
	}
}
