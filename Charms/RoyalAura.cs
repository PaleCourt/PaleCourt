using System;
using System.Collections;
using ModCommon.Util;
using UnityEngine;

namespace FiveKnights
{
    public class RoyalAura : MonoBehaviour
    {
        private const int AuraDamage = 2;
        private const float CooldownTime = 0.25f;

        private bool _cooledDown = true;

        private void OnParticleCollision(GameObject other)
        {
            if (other.layer == 11 && _cooledDown)
            {
                _cooledDown = false;
                if (other.GetComponent<HealthManager>())
                    other.GetComponent<HealthManager>().ApplyExtraDamage(AuraDamage);
                if (other.GetComponent<SpriteFlash>())
                    other.GetComponent<SpriteFlash>().flashFocusHeal();
                if (other.GetComponent<ExtraDamageable>())
                {
                    ExtraDamageable extraDamageable = other.GetComponent<ExtraDamageable>();
                    RandomAudioClipTable audioClipTable =
                        extraDamageable.GetAttr<ExtraDamageable, RandomAudioClipTable>("impactClipTable");
                    AudioSource audioPlayerPrefab =
                        extraDamageable.GetAttr<ExtraDamageable, AudioSource>("audioPlayerPrefab");
                    audioClipTable.SpawnAndPlayOneShot(audioPlayerPrefab, other.transform.position);
                }

                StartCoroutine(StartCooldown());
            }
        }

        private IEnumerator StartCooldown()
        {
            yield return new WaitForSeconds(CooldownTime);
            _cooledDown = true;
        }

        private void Log(object message) => Modding.Logger.Log("[Royal Aura] " + message);
    }
}
