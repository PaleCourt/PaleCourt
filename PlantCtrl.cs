using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using ModCommon;
using UnityEngine;
using System.Reflection;

namespace FiveKnights
{
    public class PlantCtrl : MonoBehaviour
    {
        private Animator _anim;
        private BoxCollider2D _bc;
        private HealthManager _hm;
        private SpriteRenderer _sr;
        public List<float> PlantX;
        public bool onlyIsma;

        private void Awake()
        {
            _anim = gameObject.GetComponent<Animator>();
            _bc = gameObject.GetComponent<BoxCollider2D>();
            _sr = gameObject.GetComponent<SpriteRenderer>();
        }

        private IEnumerator Start()
        {
            yield return null;
            if (onlyIsma)
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
            if (!onlyIsma) StartCoroutine(Death());
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
