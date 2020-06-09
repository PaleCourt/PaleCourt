using System.Collections;
using UnityEngine;

namespace FiveKnights
{
    public class StatueControl : MonoBehaviour
    {
        private BossStatue _bs;
        private SpriteRenderer _sr;
        private Sprite _spr;
        private bool canToggle;
        private GameObject _fakeStat;
        private string statName;

        private void Start()
        {
            _bs = gameObject.transform.parent.gameObject.GetComponent<BossStatue>();
            _spr = transform.Find("Statue").Find("GG_statues_0006_5").GetComponent<SpriteRenderer>().sprite;
            _fakeStat = new GameObject("FakeStat");
            _sr = _fakeStat.AddComponent<SpriteRenderer>();
            _sr.sprite = _spr;
            _fakeStat.transform.localScale = transform.Find("Statue").Find("GG_statues_0006_5").localScale;
            _fakeStat.transform.position = transform.Find("Statue").Find("GG_statues_0006_5").position;

            if (_bs.statueStatePD.Contains("Isma"))
            {
                statName = "Isma";
                canToggle = true;
                _sr.flipX = FiveKnights.Instance.Settings.AltStatueIsma;
                On.BossStatueLever.OnTriggerEnter2D += BossStatueLever_OnTriggerEnter2D;
            }
            else if (_bs.statueStatePD.Contains("Zemer"))
            {
                statName = "Zemer";
                canToggle = true;
                _sr.flipX = FiveKnights.Instance.Settings.AltStatueZemer;
                On.BossStatueLever.OnTriggerEnter2D += BossStatueLever_OnTriggerEnter2D;
            }
        }

        private void BossStatueLever_OnTriggerEnter2D(On.BossStatueLever.orig_OnTriggerEnter2D orig, BossStatueLever self, Collider2D collision)
        {
            if (!canToggle) return;
            string name = self.gameObject.transform.parent.parent.GetComponent<BossStatue>().statueStatePD;
            if (collision.tag == "Nail Attack" && (name.Contains("Isma") || name.Contains("Zemer")))
            {
                //_bs.transform.Find("alt_lever").Find("GG_statue_switch_lever").GetComponent<BossStatueLever>()
                StartCoroutine(SwapStatues(self));
                if (name.Contains("Isma"))
                {
                    FiveKnights.Instance.Settings.AltStatueIsma = !FiveKnights.Instance.Settings.AltStatueIsma;
                    _bs.SetDreamVersion(FiveKnights.Instance.Settings.AltStatueIsma, false, false);
                }
                else
                {
                    FiveKnights.Instance.Settings.AltStatueZemer = !FiveKnights.Instance.Settings.AltStatueZemer;
                    _bs.SetDreamVersion(FiveKnights.Instance.Settings.AltStatueZemer, false, false);
                }
                canToggle = false;
                self.switchSound.SpawnAndPlayOneShot(self.audioPlayerPrefab, transform.position);
                GameManager.instance.FreezeMoment(1);
                GameCameras.instance.cameraShakeFSM.SendEvent("EnemyKillShake");
                if (self.strikeNailPrefab && self.hitOrigin)
                {
                    self.strikeNailPrefab.Spawn(self.hitOrigin.transform.position);
                }
                if (self.leverAnimator)
                {
                    self.leverAnimator.Play("Hit");
                }
                return;
            }
            orig(self, collision);
        }

        private IEnumerator SwapStatues(BossStatueLever lev)
        {
            canToggle = false;
            if (_bs.bossUIControlFSM)
            {
                FSMUtility.SendEventToGameObject(_bs.bossUIControlFSM.gameObject, "NPC CONTROL OFF", false);
            }
            yield return new WaitForSeconds(0.25f);
            if (_bs.statueShakeParticles)
            {
                _bs.statueShakeParticles.Play();
            }
            if (_bs.statueShakeLoop)
            {
                _bs.statueShakeLoop.Play();
            }
            yield return this.StartCoroutine(this.Jitter(_bs.shakeTime, 0.1f, _fakeStat));
            if (_bs.statueShakeLoop)
            {
                _bs.statueShakeLoop.Stop();
            }
            StartCoroutine(this.PlayAudioEventDelayed(_bs.statueDownSound, _bs.statueDownSoundDelay));
            float time = (statName == "Isma") ? 0.5f : 1.5f;
            yield return StartCoroutine(PlayAnimWait(_fakeStat, "Down", time));

            yield return new WaitForSeconds(0.5f);
            StartCoroutine(this.PlayParticlesDelay(_bs.statueUpParticles, _bs.upParticleDelay));
            StartCoroutine(this.PlayAudioEventDelayed(_bs.statueUpSound, _bs.statueUpSoundDelay));

            _sr.flipX = !_sr.flipX;

            yield return this.StartCoroutine(this.PlayAnimWait(_fakeStat, "Up", time));
            if (_bs.bossUIControlFSM)
            {
                FSMUtility.SendEventToGameObject(_bs.bossUIControlFSM.gameObject, "CONVO CANCEL", false);
            }
            lev.leverAnimator.Play("Shine");
            canToggle = true;
            
        }

        private IEnumerator Jitter(float duration, float magnitude, GameObject obj)
        {
            Transform sprite = obj.transform;
            Vector3 initialPos = sprite.position;
            float elapsed = 0f;
            float half = magnitude / 2f;
            while (elapsed < duration)
            {
                sprite.position = initialPos + new Vector3(UnityEngine.Random.Range(-half, half), UnityEngine.Random.Range(-half, half), 0f);
                yield return null;
                elapsed += Time.deltaTime;
            }
            sprite.position = initialPos;
            yield break;
        }

        private IEnumerator PlayAnimWait(GameObject go, string stateName, float normalizedTime)
        {
            float t = normalizedTime;
            if (stateName == "Down")
            {
                while (t >= 0f)
                {
                    go.transform.position -= new Vector3(0f,0.2f,0f);
                    t -= Time.fixedDeltaTime;
                    yield return new WaitForEndOfFrame();
                }
            }
            if (stateName == "Up")
            {
                while (t >= 0f)
                {
                    go.transform.position += new Vector3(0f, 0.2f, 0f);
                    t -= Time.fixedDeltaTime;
                    yield return new WaitForEndOfFrame();
                }
            }
            yield break;
        }

        private IEnumerator PlayParticlesDelay(ParticleSystem system, float delay)
        {
            if (system)
            {
                yield return new WaitForSeconds(delay);
                system.Play();
            }
            yield break;
        }

        private IEnumerator PlayAudioEventDelayed(AudioEvent audioEvent, float delay)
        {
            yield return new WaitForSeconds(delay);
            audioEvent.SpawnAndPlayOneShot(_bs.audioSourcePrefab, _bs.transform.position);
            yield break;
        }

        private void OnDestroy()
        {
            On.BossStatueLever.OnTriggerEnter2D -= BossStatueLever_OnTriggerEnter2D;
        }
    }
}
