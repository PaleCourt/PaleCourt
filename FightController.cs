using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Modding;
using System.Collections;
using HutongGames.PlayMaker.Actions;
using ModCommon.Util;
using ModCommon;
using HutongGames.PlayMaker;
using Object = UnityEngine.Object;
using System.Reflection;

namespace FiveKnights
{
    public class FightController : MonoBehaviour
    {
        public static FightController Instance;
        private GameObject _whiteD;
        private GameObject _isma;

        private IEnumerator Start()
        {
            Instance = this;
            yield return new WaitWhile(() => !GameObject.Find("White Defender"));
            _whiteD = GameObject.Find("White Defender");
            _whiteD.AddComponent<WDController>();
        }

        public void CreateIsma()
        {
            Log("Creating Isma");
            _isma = Instantiate(FiveKnights.preloadedGO["Isma"]);
            _isma.SetActive(true);

            HealthManager hm = _isma.AddComponent<HealthManager>();
            HealthManager hornHP = _whiteD.GetComponent<HealthManager>();
            foreach (FieldInfo fi in typeof(HealthManager).GetFields(BindingFlags.Instance | BindingFlags.NonPublic)
                .Where(x => x.Name.Contains("Prefab")))
            {
                fi.SetValue(hm, fi.GetValue(hornHP));
            }

            EnemyHitEffectsUninfected hitEff = _isma.AddComponent<EnemyHitEffectsUninfected>();
            hitEff.enabled = true;
            EnemyHitEffectsUninfected hornetHitEffects = _whiteD.GetComponent<EnemyHitEffectsUninfected>();
            foreach (FieldInfo fi in typeof(EnemyHitEffectsUninfected).GetFields(BindingFlags.Default | BindingFlags.Instance | BindingFlags.Public | BindingFlags.Static | BindingFlags.CreateInstance | BindingFlags.DeclaredOnly | BindingFlags.ExactBinding | BindingFlags.FlattenHierarchy | BindingFlags.GetField | BindingFlags.GetProperty | BindingFlags.IgnoreCase | BindingFlags.IgnoreReturn | BindingFlags.InvokeMethod | BindingFlags.NonPublic | BindingFlags.SetField | BindingFlags.SetProperty | BindingFlags.OptionalParamBinding | BindingFlags.PutDispProperty | BindingFlags.SuppressChangeType | BindingFlags.PutRefDispProperty))
            {
                fi.SetValue(hitEff, fi.GetValue(hornetHitEffects));
            }

            EnemyDeathEffectsUninfected deathEff = _isma.AddComponent<EnemyDeathEffectsUninfected>();
            deathEff.enabled = true;
            EnemyDeathEffectsUninfected hornetDeathEffects = _whiteD.GetComponent<EnemyDeathEffectsUninfected>();
            foreach (FieldInfo fi in typeof(EnemyDeathEffectsUninfected).GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Default | BindingFlags.Static | BindingFlags.DeclaredOnly))
            {
                fi.SetValue(deathEff, fi.GetValue(hornetDeathEffects));
            }

            foreach (GameObject i in FiveKnights.preloadedGO.Values.Where(x => !x.name.Contains("Dream")))
            {
                if (i.name.Contains("Isma")) continue;
                i.GetComponent<SpriteRenderer>().material = new Material(Shader.Find("Sprites/Default"));
            }

            foreach (SpriteRenderer i in _isma.GetComponentsInChildren<SpriteRenderer>(true))
            {
                i.material = new Material(Shader.Find("Sprites/Default"));
                if (i.gameObject.GetComponent<PolygonCollider2D>())
                {
                    Log(i.name);
                    i.gameObject.AddComponent<DamageHero>().damageDealt = 1;
                    i.gameObject.layer = 11;
                }
            }

            SpriteRenderer _sr = _isma.GetComponent<SpriteRenderer>();
            _sr.material = ArenaFinder.materials["flash"];
            IsmaController ic = _isma.AddComponent<IsmaController>();
            ic.dd = _whiteD;
            PlayMakerFSM fsm = _whiteD.LocateMyFSM("Dung Defender");
            GameObject pillar = fsm.GetAction<SendEventByName>("G Slam", 5).eventTarget.gameObject.GameObject.Value.transform.Find("Dung Pillar (1)").gameObject;
            FiveKnights.preloadedGO["pillar"] = pillar;
            Log("Done creating Isma");
        }

        private void OnDestroy()
        {
            Destroy(_whiteD);
            Destroy(_isma);
        }

        private void Log(object o)
        {
            Modding.Logger.Log("[Fight Ctrl] " + o);
        }
    }
}
