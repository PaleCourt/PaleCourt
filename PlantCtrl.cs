using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using UnityEngine;
using System.Reflection;

namespace FiveKnights
{
    public class PlantCtrl : MonoBehaviour
    {
        private Animator _anim;
        private HealthManager _hm;
        private SpriteRenderer _sr;
        public List<float> PlantX;

        private void Awake()
        {
            _anim = gameObject.GetComponent<Animator>();
            _hm = gameObject.AddComponent<HealthManager>();
            _sr = gameObject.GetComponent<SpriteRenderer>();
        }

        private IEnumerator Start()
        {
            SetupHM();
            gameObject.AddComponent<Flash>();
            _hm.hp = 150;
            DamageHero dh = gameObject.AddComponent<DamageHero>();
            dh.damageDealt = 1;
            dh.enabled = false;
            gameObject.layer = 11;
            gameObject.SetActive(true);
            _anim.Play("PlantGrow");
            yield return new WaitForSeconds(0.05f);
            yield return new WaitWhile(() => _anim.IsPlaying());
            dh.enabled = true;
        }

        private void Update()
        {
            if (_hm.hp <= 0)
            {
                _hm.Die(new float?(0f), AttackTypes.Nail, true);
                StartCoroutine(Death());
            }
        }

        private IEnumerator Death()
        {
            _anim.Play("PlantDie");
            yield return new WaitForSeconds(0.05f);
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
        }
    }
}
