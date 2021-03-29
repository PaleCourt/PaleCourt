using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ModCommon;
using UnityEngine;

namespace FiveKnights.Isma
{
    public class PlantCtrl : MonoBehaviour
    {
        private Animator _anim;
        private HealthManager _hm;
        public List<float> PlantX;
        public bool IsmaFight;

        private void Awake()
        {
            _anim = gameObject.GetComponent<Animator>();
        }

        private IEnumerator Start()
        {
            yield return null;
            if (IsmaFight)
            {
                _hm = gameObject.AddComponent<HealthManager>();
                SetupHM();
                gameObject.AddComponent<Flash>();
                _hm.hp = 75;
            }
            DamageHero dh = gameObject.AddComponent<DamageHero>();
            dh.damageDealt = 1;
            dh.enabled = false;
            gameObject.layer = 11;
            gameObject.SetActive(true);
            _anim.enabled = true;
            _anim.Play("PlantGrow");
            yield return null;
            yield return new WaitWhile(() => _anim.GetCurrentFrame() < 2);
            _anim.enabled = false;
            yield return new WaitForSeconds(0.9f);
            _anim.enabled = true;
            yield return new WaitWhile(() => _anim.IsPlaying());
            dh.enabled = true;
            yield return new WaitForSeconds(0.55f);
            if (!IsmaFight) StartCoroutine(Death());
        }

        private IEnumerator Death()
        {
            _anim.Play("PlantDie");
            yield return null;
            yield return new WaitWhile(() => _anim.IsPlaying());
            PlantX.Remove(gameObject.transform.GetPositionX());
            Destroy(gameObject);
        }

        private void SetupHM()
        {
            HealthManager wdHM = FiveKnights.preloadedGO["WD"].GetComponent<HealthManager>();
            HealthManager hm2 = gameObject.GetComponent<HealthManager>();
            foreach (FieldInfo fi in typeof(HealthManager).GetFields(BindingFlags.Instance | BindingFlags.NonPublic)
                .Where(x => x.Name.Contains("Prefab")))
            {
                fi.SetValue(hm2, fi.GetValue(wdHM));
            }
            Modding.Logger.Log("HM SZTUFFF2");
        }
    }
}
