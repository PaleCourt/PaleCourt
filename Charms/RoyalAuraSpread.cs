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

        private void Awake()
		{
            foreach(var pool in ObjectPool.instance.startupPools)
            {
                if(pool.prefab.name == "Knight Dung Trail")
                {
                    _dungTrail = pool.prefab;
                    break;
                }
            }
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
