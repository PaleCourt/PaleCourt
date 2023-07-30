using SFCore.Utils;
using System.Collections;
using UnityEngine;

namespace FiveKnights
{
    public class RoyalAuraSpread : MonoBehaviour
    {
        private int _repeat = 5;
        private float _cooldown = 0.75f;

        private GameObject _dungTrail;
        private PlayMakerFSM _dungTrailControl;
        private ParticleSystem _dungPt;

        private void Awake()
        {
            foreach(var pool in ObjectPool.instance.startupPools)
            {
                if(pool.prefab.name == "Knight Dung Trail")
                {
                    _dungTrail = Instantiate(pool.prefab);
                    _dungTrail.SetActive(false);
                    DontDestroyOnLoad(_dungTrail);
                    break;
                }
            }
            _dungTrailControl = _dungTrail.LocateMyFSM("Control");
            _dungPt = _dungTrailControl.Fsm.GetFsmGameObject("Pt Normal").Value.GetComponent<ParticleSystem>();
            ParticleSystem.MainModule main = _dungPt.main;
            main.startColor = new Color(0.65f, 0.65f, 0.65f, 0.75f);

            StartCoroutine(StartCooldown());
        }

        private IEnumerator StartCooldown()
        {
            for(int i = 0; i < _repeat; i++)
            {
                HealthManager hm = gameObject.GetComponent<HealthManager>();
                if(hm.hp <= 0 || hm.isDead) break;

                GameObject dungTrail = Instantiate(_dungTrail, gameObject.transform.position, Quaternion.identity);
                dungTrail.transform.localScale *= 2f;
                dungTrail.SetActive(true);
                yield return new WaitForSeconds(_cooldown);
                yield return new WaitUntil(() => gameObject.activeSelf);
            }
            // Hopefully to prevent it from self-inducing and making the effect infinite
            yield return new WaitForSeconds(1f);
            Destroy(this);
        }

        private void OnDestroy()
        {
            Destroy(_dungTrail);

            StopAllCoroutines();
        }

        private void Log(object message) => Modding.Logger.Log("[FiveKnights][Royal Aura Spread] " + message);
    }
}
