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
		private ParticleSystem ps;

		private void Update()
		{
			if(ps == null)
			{
				ps = gameObject.GetComponent<ParticleSystem>();
			}
			ParticleSystem.MainModule main = ps.main;
			main.startColor = new Color(0.8f, 0.8f, 0.8f);
		}
	}
}
