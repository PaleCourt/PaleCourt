using System.Collections;
using UnityEngine;

namespace FiveKnights
{
    public class StatueControl : MonoBehaviour
    {
        public BossStatue _bs;
        public SpriteRenderer _sr;
        public GameObject _fakeStat;
        public GameObject _fakeStatAlt;
        public GameObject _fakeStatAlt2;
        public string StatueName;
        private bool canToggle;

        private IEnumerator Start()
        {
            yield return null;
            yield return null;
            yield return null;
            yield return null;
            canToggle = true;
            if (StatueName.Contains("Isma"))
            {
                if (FiveKnights.Instance.LocalSaveData.AltStatueIsma)
                {
                    _fakeStat = _fakeStatAlt;
                    _fakeStat.SetActive(false);
                    _fakeStat = _fakeStatAlt2;
                    _fakeStat.transform.position = new Vector3(82.2f, 95.9f, 2.0f);
                    _fakeStatAlt.transform.position = new Vector3(82.2f, 91.6f, 2.0f);
                    _fakeStat.SetActive(true);
                }
                else
                {
                    _fakeStat = _fakeStatAlt2;
                    _fakeStat.SetActive(false);
                    _fakeStat = _fakeStatAlt;
                    _sr.sprite = FiveKnights.SPRITES["Isma"];
                    _fakeStat.transform.position = new Vector3(82.2f, 96.5f, 2.0f);
                    _fakeStatAlt2.transform.position = new Vector3(82.2f, 90.9f, 2.0f);
                    _fakeStat.SetActive(true);
                }
            }
            else
            {
                _fakeStat = _fakeStatAlt;
                _sr.flipX = FiveKnights.Instance.LocalSaveData.AltStatueZemer;
            }
            StatueName = StatueName.Contains("Isma") ? "Isma" : "Zemer";
        }

        public void StartLever(BossStatueLever self)
        {
            if (!canToggle) return;
            if (StatueName == "Isma")
            {
                StartCoroutine(SwapStatues(self));
                FiveKnights.Instance.LocalSaveData.AltStatueIsma = !FiveKnights.Instance.LocalSaveData.AltStatueIsma;
                _bs.SetDreamVersion(FiveKnights.Instance.LocalSaveData.AltStatueIsma, false, false);
            }
            else if (StatueName == "Zemer")
            {
                StartCoroutine(SwapStatues(self));
                FiveKnights.Instance.LocalSaveData.AltStatueZemer = !FiveKnights.Instance.LocalSaveData.AltStatueZemer;
                _bs.SetDreamVersion(FiveKnights.Instance.LocalSaveData.AltStatueZemer, false, false);
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
        }

        private IEnumerator SwapStatues(BossStatueLever lev)
        {
            canToggle = false;
            if (_bs.bossUIControlFSM)
            {
                FSMUtility.SendEventToGameObject(_bs.bossUIControlFSM.gameObject, "NPC CONTROL OFF");
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
            float time = (StatueName == "Isma") ? 0.5f : 1.5f;
            yield return StartCoroutine(PlayAnimWait(_fakeStat, "Down", time));

            yield return new WaitForSeconds(time);
            StartCoroutine(this.PlayParticlesDelay(_bs.statueUpParticles, _bs.upParticleDelay));
            StartCoroutine(this.PlayAudioEventDelayed(_bs.statueUpSound, _bs.statueUpSoundDelay));
            if (StatueName.Contains("Isma"))
            {
                if (FiveKnights.Instance.LocalSaveData.AltStatueIsma)
                {
                    _fakeStat.SetActive(false);
                    _fakeStat = _fakeStatAlt2;
                    _fakeStat.SetActive(true);
                }
                else
                {
                    _fakeStat.SetActive(false);
                    _fakeStat = _fakeStatAlt;
                    _sr.sprite = FiveKnights.SPRITES["Isma"];
                    _fakeStat.SetActive(true);
                }
            }
            else
            {
                _sr.flipX = FiveKnights.Instance.LocalSaveData.AltStatueZemer;
            }

            yield return this.StartCoroutine(this.PlayAnimWait(_fakeStat, "Up", time));
            if (_bs.bossUIControlFSM)
            {
                FSMUtility.SendEventToGameObject(_bs.bossUIControlFSM.gameObject, "CONVO CANCEL");
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
            float mid = t / 2;
            if (stateName == "Down")
            {
                while (t >= 0f)
                {
                    if (t <= mid) go.SetActive(false);
                    go.transform.position -= new Vector3(0f,0.2f,0f);
                    t -= Time.fixedDeltaTime;
                    yield return new WaitForEndOfFrame();
                }
            }
            if (stateName == "Up")
            {
                while (t >= 0f)
                {
                    if (t <= mid) go.SetActive(true);
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
        }
    }
}
