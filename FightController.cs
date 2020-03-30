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
        private GameObject _hegemol;

        private IEnumerator Start()
        {
            Instance = this;
            yield return new WaitWhile(() => !GameObject.Find("White Defender"));
            _whiteD = GameObject.Find("White Defender");
            //_whiteD.AddComponent<WDController>();
            GameManager.instance.gameObject.AddComponent<WDController>().dd = _whiteD;
            
            PlayMakerFSM fsm = _whiteD.LocateMyFSM("Dung Defender");
            GameObject pillar = fsm.GetAction<SendEventByName>("G Slam", 5).eventTarget.gameObject.GameObject.Value.transform.Find("Dung Pillar (1)").gameObject;
            FiveKnights.preloadedGO["pillar"] = pillar;

            GameObject dungBall = fsm.GetAction<SpawnObjectFromGlobalPool>("Throw 1", 1).gameObject.Value;
            FiveKnights.preloadedGO["ball"] = dungBall;
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
            Log("Done creating Isma");
        }
        
        public DryyaController CreateDryya()
        {
            _dryya = Instantiate(FiveKnights.preloadedGO["Dryya"], new Vector2(90, 15), Quaternion.identity);
            _dryya.AddComponent<DryyaController>().ogrim = _whiteD;
            return _dryya.GetComponent<DryyaController>();
        }

        public HegemolController CreateHegemol()
        {
            Log("Creating Hegemol");
            _hegemol = Instantiate(FiveKnights.preloadedGO["fk"],
                new Vector2(HeroController.instance.transform.position.x, 23), Quaternion.identity);
            _hegemol.SetActive(true);
            Log("Adding HegemolController component");
            return _hegemol.AddComponent<HegemolController>();
        }
        
        public ZemerController CreateZemer()
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
            return zc;
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
