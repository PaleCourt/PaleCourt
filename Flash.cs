using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using System.Collections;
using On;

namespace FiveKnights
{
    public class Flash : MonoBehaviour
    {
        private bool flashing;
        private SpriteRenderer _sr;

        private void Start()
        {
            _sr = gameObject.GetComponent<SpriteRenderer>();
            _sr.material = ArenaFinder.materials["flash"];
            On.HealthManager.TakeDamage += HealthManager_TakeDamage;
        }

        private void HealthManager_TakeDamage(On.HealthManager.orig_TakeDamage orig, HealthManager self, HitInstance hitInstance)
        {
            if (self.gameObject == gameObject)
            {
                if (!flashing)
                {
                    flashing = true;
                    StartCoroutine(FlashWhite());
                }
            }
            orig(self, hitInstance);
        }

        IEnumerator FlashWhite()
        {
            _sr.material.SetFloat("_FlashAmount", 1f);
            yield return null;
            for (float i = 1f; i >= 0f; i -= 0.05f)
            {
                _sr.material.SetFloat("_FlashAmount", i);
                yield return null;
            }
            yield return null;
            flashing = false;
        }

        private void OnDestroy()
        {
            On.HealthManager.TakeDamage -= HealthManager_TakeDamage;
        }
    }
}
