using UnityEngine;
using System.Collections;
using HutongGames.PlayMaker.Actions;
using ModCommon;

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

            foreach (GameObject i in Resources.FindObjectsOfTypeAll<GameObject>())
            {
                if (i.PrintSceneHierarchyPath() != "Hollow Shade\\Slash")
                    continue;
                
                FiveKnights.preloadedGO["parryFX"] = i.LocateMyFSM("nail_clash_tink").GetAction<SpawnObjectFromGlobalPool>("No Box Down", 1).gameObject.Value;

                AudioClip aud = i
                                .LocateMyFSM("nail_clash_tink")
                                .GetAction<AudioPlayerOneShot>("Blocked Hit", 5)
                                .audioClips[0];

                var clashSndObj = new GameObject();
                var clashSnd = clashSndObj.AddComponent<AudioSource>();

                clashSnd.clip = aud;
                clashSnd.pitch = Random.Range(0.85f, 1.15f);

                Tink.TinkClip = aud;

                FiveKnights.preloadedGO["ClashTink"] = clashSndObj;

                break;
            }
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
                    continue;

                if (!i.gameObject.GetComponent<PolygonCollider2D>() && !i.gameObject.GetComponent<BoxCollider2D>()) 
                    continue;
                
                i.gameObject.AddComponent<DamageHero>().damageDealt = 1;
                i.gameObject.layer = 11;
            }
            
            foreach (LineRenderer lr in _isma.GetComponentsInChildren<LineRenderer>(true))
            {
                lr.material = new Material(Shader.Find("Sprites/Default"));
            }
            
            var _sr = _isma.GetComponent<SpriteRenderer>();
            _sr.material = ArenaFinder.Materials["flash"];
            
            _isma.AddComponent<IsmaController>();
            
            Log("Done creating Isma");
        }
        
        public DryyaSetup CreateDryya()
        {
            _dryya = Instantiate(FiveKnights.preloadedGO["Dryya"], new Vector2(90, 25), Quaternion.identity);
            
            return _dryya.AddComponent<DryyaSetup>();
        }

        public HegemolController CreateHegemol()
        {
            Log("Creating Hegemol");
            
            _hegemol = Instantiate(FiveKnights.preloadedGO["fk"], new Vector2(HeroController.instance.transform.position.x, 23), Quaternion.identity);
            _hegemol.SetActive(true);
            
            Log("Adding HegemolController component");
            
            return _hegemol.AddComponent<HegemolController>();
        }
        
        public ZemerController CreateZemer()
        {
            Log("Creating Zemer");
            
            _zemer = Instantiate(FiveKnights.preloadedGO["Zemer"]);
            _zemer.SetActive(true);
            
            foreach (Transform i in FiveKnights.preloadedGO["SlashBeam"].transform)
            {
                i.gameObject.AddComponent<DamageHero>().damageDealt = 1;
                i.gameObject.layer = 22;
            }
            
            foreach (Transform i in FiveKnights.preloadedGO["SlashBeam2"].transform)
            {
                i.GetComponent<SpriteRenderer>().material =  new Material(Shader.Find("Sprites/Default"));   
                
                i.Find("HB1").gameObject.AddComponent<DamageHero>().damageDealt = 1;
                i.Find("HB2").gameObject.AddComponent<DamageHero>().damageDealt = 1;
                
                i.Find("HB1").gameObject.layer = 22;
                i.Find("HB2").gameObject.layer = 22;
            }
            
            foreach (Transform i in FiveKnights.preloadedGO["SlashBeam3"].transform)
            {
                i.GetComponent<SpriteRenderer>().material =  new Material(Shader.Find("Sprites/Default"));   
                
                i.Find("HB1").gameObject.AddComponent<DamageHero>().damageDealt = 1;
                i.Find("HB2").gameObject.AddComponent<DamageHero>().damageDealt = 1;
                
                i.Find("HB1").gameObject.layer = 22;
                i.Find("HB2").gameObject.layer = 22;
            }
            
            foreach (SpriteRenderer i in _zemer.GetComponentsInChildren<SpriteRenderer>(true))
            {
                i.material = new Material(Shader.Find("Sprites/Default"));
                
                var bc = i.gameObject.GetComponent<BoxCollider2D>();
                
                if (bc == null) 
                    continue;
                
                bc.isTrigger = true;
                bc.gameObject.AddComponent<DamageHero>().damageDealt = 1;
                bc.gameObject.layer = 22;
            }
            
            foreach (PolygonCollider2D i in _zemer.GetComponentsInChildren<PolygonCollider2D>(true))
            { 
                i.isTrigger = true;
                i.gameObject.AddComponent<DamageHero>().damageDealt = 1;
                i.gameObject.AddComponent<Tink>();
                i.gameObject.AddComponent<Pogoable>();
                i.gameObject.layer = 22;
                
            }
            
            _zemer.GetComponent<SpriteRenderer>();
            var zc = _zemer.AddComponent<ZemerController>();
            
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
