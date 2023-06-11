using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace FiveKnights
{
	public class ModifyAuraColor : MonoBehaviour
	{
		private readonly Color DefaultColor = new Color(0.6706f, 0.4275f, 0f, 1f);
		private readonly Color PaleColor = new Color(0.8f, 0.8f, 0.8f);
		private ParticleSystem ps;

		private void Update()
		{
			if(ps == null)
			{
				ps = gameObject.GetComponent<ParticleSystem>();
			}
			ParticleSystem.MainModule main = ps.main;
			main.startColor = FiveKnights.Instance.SaveSettings.upgradedCharm_10 ? PaleColor : DefaultColor;
		}
	}
}
