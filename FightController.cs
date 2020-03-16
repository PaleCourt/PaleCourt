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
            FiveKnights.preloadedGO["Isma2"] = _isma;
            _isma.SetActive(true);
            foreach (SpriteRenderer i in _isma.GetComponentsInChildren<SpriteRenderer>(true))
            {
                i.material = new Material(Shader.Find("Sprites/Default"));
                if (i.name == "FrontW")
                {
                    continue;
                }
                if (i.gameObject.GetComponent<PolygonCollider2D>() || i.gameObject.GetComponent<BoxCollider2D>())
                {
                    i.gameObject.AddComponent<DamageHero>().damageDealt = 1;
                    i.gameObject.layer = 11;
                }
            }
            foreach (LineRenderer lr in _isma.GetComponentsInChildren<LineRenderer>(true))
            {
                lr.material = new Material(Shader.Find("Sprites/Default"));
            }
            SpriteRenderer _sr = _isma.GetComponent<SpriteRenderer>();
            _sr.material = ArenaFinder.materials["flash"];
            _isma.AddComponent<IsmaController>();
            PlayMakerFSM fsm = _whiteD.LocateMyFSM("Dung Defender");
            GameObject pillar = fsm.GetAction<SendEventByName>("G Slam", 5).eventTarget.gameObject.GameObject.Value.transform.Find("Dung Pillar (1)").gameObject;
            FiveKnights.preloadedGO["pillar"] = pillar;
            Log("Done creating Isma");
        }
        
        public void CreateDryya()
        {
            _dryya = Instantiate(FiveKnights.preloadedGO["Dryya"], new Vector2(90, 15), Quaternion.identity);
            _dryya.AddComponent<DryyaController>().ogrim = _whiteD;
        }

        public void CreateZemer()
        {
            Log("Creating Zemer");
            _zemer = Instantiate(FiveKnights.preloadedGO["Zemer"]);
            _zemer.SetActive(true);

            foreach (GameObject i in Resources.FindObjectsOfTypeAll<GameObject>())
            {
                if (i.PrintSceneHierarchyPath() == "Hollow Shade\\Slash")
                {
                    FiveKnights.preloadedGO["parryFX"] = i.LocateMyFSM("nail_clash_tink").GetAction<SpawnObjectFromGlobalPool>("No Box Down", 1).gameObject.Value;
                    AudioClip aud = i.LocateMyFSM("nail_clash_tink").GetAction<AudioPlayerOneShot>("Blocked Hit", 5).audioClips[0];
                    GameObject clashSndObj = new GameObject();
                    AudioSource clashSnd = clashSndObj.AddComponent<AudioSource>();
                    clashSnd.clip = aud;
                    clashSnd.pitch = UnityEngine.Random.Range(0.85f, 1.15f);
                    FiveKnights.preloadedGO["ClashTink"] = clashSndObj;
                    break;
                }
            }
            
            HealthManager hm = _zemer.AddComponent<HealthManager>();
            HealthManager hornHP = _whiteD.GetComponent<HealthManager>();
            foreach (FieldInfo fi in typeof(HealthManager).GetFields(BindingFlags.Instance | BindingFlags.NonPublic)
                .Where(x => x.Name.Contains("Prefab")))
            {
                fi.SetValue(hm, fi.GetValue(hornHP));
            }

            foreach (PolygonCollider2D i in _zemer.GetComponentsInChildren<PolygonCollider2D>(true))
            {
                i.isTrigger = true;
                i.gameObject.AddComponent<DamageHero>().damageDealt = 1;
                i.gameObject.AddComponent<Parryable>();
                i.gameObject.layer = 22;
            }
            SpriteRenderer _sr = _zemer.GetComponent<SpriteRenderer>();
            ZemerController zc = _zemer.AddComponent<ZemerController>();
            Log("Done creating Zemer");
        }

        private void OnDestroy()
        {
            Destroy(_whiteD);
            Destroy(_isma);
            Destroy(_zemer);
        }

        private void Log(object o)
        {
            Modding.Logger.Log("[Fight Ctrl] " + o);
        }
    }
}
