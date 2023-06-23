using System;

using MonoMod.ModInterop;

using UnityEngine;

namespace FiveKnights
{
	internal static class EnemyHPBarImport
	{
		public static void DisableHPBar(GameObject go)
		{
			EnemyHPBar.DisableHPBar?.Invoke(go);
		}

		public static void EnableHPBar(GameObject go) {
			EnemyHPBar.EnableHPBar?.Invoke(go);
		}

		public static void RefreshHPBar(GameObject go)
		{
			EnemyHPBar.RefreshHPBar?.Invoke(go);
		}

		public static void MarkAsBoss(GameObject go)
		{
			EnemyHPBar.MarkAsBoss?.Invoke(go);
		}

		static EnemyHPBarImport()
		{
			typeof(EnemyHPBar).ModInterop();
		}

		[ModImportName(nameof(EnemyHPBar))]
		internal static class EnemyHPBar
		{
			public static Action<GameObject> DisableHPBar;
			public static Action<GameObject> EnableHPBar;
			public static Action<GameObject> RefreshHPBar;
			public static Action<GameObject> MarkAsBoss;
		}
	}
}
