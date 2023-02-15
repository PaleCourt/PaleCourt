using UnityEngine;
using System.Collections;
using System.Linq;
using FiveKnights.BossManagement;
using FiveKnights.Dryya;
using FiveKnights.Hegemol;
using FiveKnights.Isma;
using FiveKnights.Zemer;
using HutongGames.PlayMaker.Actions;
using Random = UnityEngine.Random;

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
            Log("In arena");   
            
            Instance = this;
            
            yield return new WaitWhile(() => !HeroController.instance);

            _whiteD = Instantiate(FiveKnights.preloadedGO["WhiteDef"]); //GameObject.Find("White Defender");
            _whiteD.SetActive(false);
            //_whiteD.AddComponent<GGBossManager>();
            
            GameManager.instance.gameObject.AddComponent<GGBossManager>().dd = _whiteD;
            
            PlayMakerFSM fsm = _whiteD.LocateMyFSM("Dung Defender");
            GameObject pillar = fsm.GetAction<SendEventByName>("G Slam", 5).eventTarget.gameObject.GameObject.Value.transform.Find("Dung Pillar (1)").gameObject;
            FiveKnights.preloadedGO["pillar"] = pillar;

            GameObject dungBall = fsm.GetAction<SpawnObjectFromGlobalPool>("Throw 1", 1).gameObject.Value;
            FiveKnights.preloadedGO["ball"] = dungBall;

            foreach (GameObject i in Resources.FindObjectsOfTypeAll<GameObject>())
            {
                if (!(i.name == "Slash" && i.transform.parent != null && i.transform.parent.gameObject.name == "Hollow Shade"))
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

            AssetBundle snd = ABManager.AssetBundles[ABManager.Bundle.Sound];
            string[] arr = new[]
            {
                "IsmaAudAtt1", "IsmaAudAtt2", "IsmaAudAtt3","IsmaAudAtt4","IsmaAudAtt5",
                "IsmaAudAtt6","IsmaAudAtt7","IsmaAudAtt8","IsmaAudAtt9","IsmaAudDeath"
            };

            IEnumerator LoadSlow()
            {
                foreach (var i in arr)
                {
                    var r = snd.LoadAssetAsync<AudioClip>(i);
                    yield return r;
                    FiveKnights.IsmaClips[i] = r.asset as AudioClip;
                }
            }
            
            AssetBundle misc = ABManager.AssetBundles[ABManager.Bundle.Misc];
            foreach (var i in misc.LoadAllAssets<Sprite>().Where(x => x.name.Contains("Sil_Isma_")))
            {
                ArenaFinder.Sprites[i.name] = i;
            }

            StartCoroutine(LoadSlow());
            
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
            
            foreach (Transform i in _isma.transform.Find("Arm2").Find("TentArm"))
            {
                i.gameObject.AddComponent<DamageHero>().damageDealt = 1;
                i.gameObject.layer = 11;
            }
            
            foreach (Transform par in _isma.transform.Find("Thorn"))
            {
                foreach (Transform i in par)
                {
                    i.gameObject.layer = 11;
                    i.gameObject.AddComponent<DamageHero>().damageDealt = 1;   
                }
            }

            var _sr = _isma.GetComponent<SpriteRenderer>();
            _sr.material = FiveKnights.Materials["flash"];
            
            _isma.AddComponent<IsmaController>();
            
            Log("Done creating Isma");
        }
        
        public DryyaSetup CreateDryya()
        {
            AssetBundle snd = ABManager.AssetBundles[ABManager.Bundle.Sound];
            FiveKnights.Clips["DryyaMusic"] = snd.LoadAsset<AudioClip>("DryyaMusic");
            
            AssetBundle misc = ABManager.AssetBundles[ABManager.Bundle.Misc];
            foreach (var i in misc.LoadAllAssets<Sprite>().Where(x => x.name.Contains("Dryya_Silhouette_")))
            {
                ArenaFinder.Sprites[i.name] = i;
            }
            
            IEnumerator DryyaIntro()
            {
                var bc = _dryya.GetComponent<BoxCollider2D>();
                bc.enabled = false;
                while (_dryya.transform.position.y > 20f)
                    yield return new WaitForFixedUpdate();
                bc.enabled = true;
            }
            
            Vector2 pos = (CustomWP.boss == CustomWP.Boss.All) ? new Vector2(91, 25.5f) : new Vector2(90, 25);
            _dryya = Instantiate(FiveKnights.preloadedGO["Dryya2"], pos, Quaternion.identity);
            StartCoroutine(DryyaIntro());
            return _dryya.AddComponent<DryyaSetup>();
        }

        public HegemolController CreateHegemol()
        {
            AssetBundle snd = ABManager.AssetBundles[ABManager.Bundle.Sound];
            FiveKnights.Clips["HegemolMusic"] = snd.LoadAsset<AudioClip>("HegemolMusic");
            string[] arr = new[]
            {
                "HegArrive", "HegAttackSwing", "HegAttackHit", "HegAttackCharge", "HegDamage", "HegDamageFinal", "HegDebris", "HegJump",
                "HegLand", "HegShockwave", "HCalm1", "HCalm2", "HCalm3", "HCharge", "HHeavy1", "HHeavy2", "HDeath", "HGrunt1", "HGrunt2",
                "HGrunt3", "HGrunt4", "HTired1", "HTired2", "HTired3"
            };

            IEnumerator LoadSlow()
            {
                foreach(var i in arr)
                {
                    var r = snd.LoadAssetAsync<AudioClip>(i);
                    yield return r;
                    FiveKnights.Clips[i] = r.asset as AudioClip;
                }
            }
            StartCoroutine(LoadSlow());

            AssetBundle misc = ABManager.AssetBundles[ABManager.Bundle.Misc];
            foreach (var i in misc.LoadAllAssets<Sprite>().Where(x => x.name.Contains("hegemol_silhouette_")))
            {
                ArenaFinder.Sprites[i.name] = i;
            }
            
            Log("Creating Hegemol");
            _hegemol = Instantiate(FiveKnights.preloadedGO["Hegemol"], new Vector2(87, 28), Quaternion.identity);
            _hegemol.SetActive(true);
            Log("Adding HegemolController component");
            return _hegemol.AddComponent<HegemolController>();
        }
        
        public ZemerController CreateZemer()
        {
            Log("Creating Zemer");

            AssetBundle snd = ABManager.AssetBundles[ABManager.Bundle.Sound];
            string[] arr = new[]
            {
                "ZAudP2Death2", "ZP2Intro","ZP1Loop", "ZAudP1Death", "ZAudAtt4", "ZAudP2Death1",
                "ZAudBow", "ZAudCounter", "ZAudAtt5", "ZP1Intro", "ZAudAtt2", "ZP2Loop",
                "ZAudLaser", "ZAudHoriz", "ZAudAtt3", "ZAudAtt1", "ZAudAtt6","AudBasicSlash1", 
                "AudBigSlash", "AudBigSlash2", "AudLand", "AudDashIntro", "AudDash", "AudBasicSlash2",
                "breakable_wall_hit_1", "breakable_wall_hit_2"
            };

            IEnumerator LoadSlow()
            {
                foreach(var i in arr)
                {
                    var r = snd.LoadAssetAsync<AudioClip>(i);
                    yield return r;
                    FiveKnights.Clips[i] = r.asset as AudioClip;
                }
            }
            StartCoroutine(LoadSlow());

            AssetBundle misc = ABManager.AssetBundles[ABManager.Bundle.Misc];
            ArenaFinder.Sprites["ZemParticPetal"] = misc.LoadAsset<Sprite>("petal-test");
            ArenaFinder.Sprites["ZemParticDung"] = misc.LoadAsset<Sprite>("dung-test");
            foreach (var i in misc.LoadAllAssets<Sprite>().Where(x => x.name.Contains("Zem_Sil_")))
            {
                ArenaFinder.Sprites[i.name] = i;
            }

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
            foreach (Transform tRing in FiveKnights.preloadedGO["SlashRingControllerNew"].transform)
            {
                foreach (Transform t in tRing)
                {
                    foreach (PolygonCollider2D i in t.GetComponentsInChildren<PolygonCollider2D>(true))
                    {
                        i.gameObject.AddComponent<DamageHero>().damageDealt = 2;
                        i.gameObject.layer = 22;
                        i.gameObject.AddComponent<Tink>();
                    }
                }
            }
            foreach (Transform tRing in FiveKnights.preloadedGO["SlashRingController"].transform)
            {
                foreach (Transform t in tRing)
                {
                    foreach (PolygonCollider2D i in t.GetComponentsInChildren<PolygonCollider2D>(true))
                    {
                        i.gameObject.AddComponent<DamageHero>().damageDealt = 2;
                        i.gameObject.layer = 22;
                        i.gameObject.AddComponent<Tink>();
                    }
                }
            }
            foreach (SpriteRenderer i in _zemer.GetComponentsInChildren<SpriteRenderer>(true))
            {
                i.material = new Material(Shader.Find("Sprites/Default"));
                
                var bc = i.gameObject.GetComponent<BoxCollider2D>();
                
                if (bc == null) 
                    continue;
                bc.isTrigger = true;
                bc.gameObject.AddComponent<DamageHero>().damageDealt = 1;
                i.gameObject.AddComponent<Pogoable>().tar = _zemer;
                bc.gameObject.layer = 22;
                if (!i.name.Contains("Zemer")) i.gameObject.AddComponent<Tink>();
            }
            foreach (PolygonCollider2D i in _zemer.GetComponentsInChildren<PolygonCollider2D>(true))
            { 
                i.isTrigger = true;
                i.gameObject.AddComponent<DamageHero>().damageDealt = 1;
                i.gameObject.AddComponent<Tink>();
                i.gameObject.AddComponent<Pogoable>().tar = _zemer;
                i.gameObject.layer = 22;
                
            }
            
            var bcMultiDashHB = _zemer.transform.Find("MultiDashParryHB").gameObject;
            bcMultiDashHB.AddComponent<Tink>();
            bcMultiDashHB.AddComponent<Pogoable>().tar = _zemer;
            bcMultiDashHB.layer = 22;
            bcMultiDashHB.SetActive(false);
            
            _zemer.GetComponent<SpriteRenderer>();
            var zc = _zemer.AddComponent<ZemerController>();
            Log("Done creating Zemer");
            
            return zc;
        }

        private void OnDestroy()
        {
            Log("OnDestroy");
            GGBossManager ctrl = GameManager.instance.gameObject.GetComponent<GGBossManager>();
            if (ctrl != null)
            {
                Destroy(ctrl);
                GGBossManager.Instance = null;
                Log("Destroyed WDCtrl");
            }

            if (_whiteD != null)
            {
                Log("Destroying _whiteD");
                Destroy(_whiteD);
            }

            if (_isma != null)
            {
                Log("Destroying _isma");
                Destroy(_isma);
            }
            
            if (_zemer != null)
            {
                Log("Destroying _zemer");
                Destroy(_zemer);
            }
        }

        private void Log(object o)
        {
            Modding.Logger.Log("[Fight Ctrl] " + o);
        }
    }
}