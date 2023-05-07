using SFCore.Utils;
using System.Collections;
using UnityEngine;

namespace FiveKnights
{
    public class RoyalAuraSpread : MonoBehaviour
    {
        private int repeat = 5;
        private float cooldown = 0.75f;

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
            for(int i = 0; i < repeat; i++)
			{
                GameObject dungTrail = Instantiate(_dungTrail, gameObject.transform.position, Quaternion.identity);
                dungTrail.transform.localScale *= 2f;
                dungTrail.SetActive(true);
                yield return new WaitForSeconds(cooldown);
                yield return new WaitUntil(() => gameObject.activeSelf);
            }
            Destroy(this);
        }

        private void OnDestroy()
		{
            StopAllCoroutines();
		}

        private void Log(object message) => Modding.Logger.Log("[FiveKnights][Royal Aura Spread] " + message);
    }
}
