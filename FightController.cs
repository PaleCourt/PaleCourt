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
        private GameObject _dryya;
        private GameObject _zemer;

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

            foreach (GameObject i in FiveKnights.preloadedGO.Values.Where(x => !x.name.Contains("Dream") && x.GetComponent<SpriteRenderer>() != null))
            {
                if (i.name.Contains("Isma")) continue;
                i.GetComponent<SpriteRenderer>().material = new Material(Shader.Find("Sprites/Default"));
            }

            foreach (SpriteRenderer i in _isma.GetComponentsInChildren<SpriteRenderer>(true))
            {
                i.material = new Material(Shader.Find("Sprites/Default"));
                if (i.gameObject.GetComponent<PolygonCollider2D>())
                {
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
        
        public void CreateDryya()
        {
            _dryya = Instantiate(new GameObject("Dryya"), new Vector2(74, 8.5f), Quaternion.identity);
            _dryya.SetActive(true);
            _dryya.AddComponent<DryyaController>().ogrim = _whiteD;
        }

        public void CreateZemer()
        {
            Log("Creating Zemer");
            _zemer = Instantiate(FiveKnights.preloadedGO["Zemer"]);
            _zemer.SetActive(true);

            HealthManager hm = _zemer.AddComponent<HealthManager>();
            HealthManager hornHP = _whiteD.GetComponent<HealthManager>();
            foreach (FieldInfo fi in typeof(HealthManager).GetFields(BindingFlags.Instance | BindingFlags.NonPublic)
                .Where(x => x.Name.Contains("Prefab")))
            {
                fi.SetValue(hm, fi.GetValue(hornHP));
            }

            SpriteRenderer _sr = _zemer.GetComponent<SpriteRenderer>();
            ZemerController zc = _zemer.AddComponent<ZemerController>();
            Log("Done creating Zemer");
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
