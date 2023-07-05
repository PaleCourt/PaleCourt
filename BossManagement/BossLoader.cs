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
using System.Collections.Generic;
using FiveKnights.Tiso;

namespace FiveKnights
{
    public static class BossLoader
    {
        private static AssetBundle _soundBundle => ABManager.AssetBundles[ABManager.Bundle.Sound];
        private static AssetBundle _miscBundle => ABManager.AssetBundles[ABManager.Bundle.Misc];
        private static AssetBundle _ismaBundle => ABManager.AssetBundles[ABManager.Bundle.GIsma];
        private static AssetBundle _dryyaBundle => ABManager.AssetBundles[ABManager.Bundle.GDryya];
        private static AssetBundle _hegemolBundle => ABManager.AssetBundles[ABManager.Bundle.GHegemol];
        private static AssetBundle _zemerBundle => ABManager.AssetBundles[ABManager.Bundle.GZemer];
        private static AssetBundle _tisoBundle => ABManager.AssetBundles[ABManager.Bundle.TisoBund];

        public static IsmaController CreateIsma(bool onlyIsma)
        {
            Log("Creating Isma");
            
            GameObject isma = GameObject.Instantiate(FiveKnights.preloadedGO["Isma"]);
            isma.SetActive(true);
            isma.AddComponent<IsmaController>().onlyIsma = onlyIsma;
            
            Log("Done creating Isma");
            return isma.GetComponent<IsmaController>();
        }
        
        public static DryyaSetup CreateDryya()
        {
            Log("Creating Dryya");

            Vector2 pos = OWArenaFinder.IsInOverWorld ? new Vector2(457.6f, 112.5f) : 
                (CustomWP.boss == CustomWP.Boss.All ? new Vector2(91f, 25.5f) : new Vector2(90f, 25f));
            GameObject dryya = GameObject.Instantiate(FiveKnights.preloadedGO["Dryya2"], pos, Quaternion.identity);
			IEnumerator DryyaIntro()
			{
                BoxCollider2D bc = dryya.GetComponent<BoxCollider2D>();
				bc.enabled = false;
				while(dryya.transform.position.y > (OWArenaFinder.IsInOverWorld ? 103f : 20f))
					yield return new WaitForFixedUpdate();
				bc.enabled = true;
			}
            GameManager.instance.StartCoroutine(DryyaIntro());
            dryya.AddComponent<DryyaSetup>();

            Log("Done creating Dryya");
            return dryya.GetComponent<DryyaSetup>();
        }

        public static HegemolController CreateHegemol()
        {
            Log("Creating Hegemol");
            GameObject _hegemol = GameObject.Instantiate(FiveKnights.preloadedGO["Hegemol"], 
                OWArenaFinder.IsInOverWorld ? new Vector2(438.4f, 28f) : new Vector2(87f, 28f), Quaternion.identity);
            _hegemol.SetActive(!OWArenaFinder.IsInOverWorld);
            _hegemol.AddComponent<HegemolController>();

            Log("Done creating Hegemol");
            return _hegemol.GetComponent<HegemolController>();
        }
        
        public static ZemerController CreateZemer()
        {
            Log("Creating Zemer");

            GameObject zemer = GameObject.Instantiate(FiveKnights.preloadedGO["Zemer"]);
            zemer.SetActive(true);
            ZemerController zc = zemer.AddComponent<ZemerController>();

            Log("Done creating Zemer");
            return zc;
        }

        public static TisoController CreateTiso()
		{
            Log("Creating Tiso");

            GameObject tiso = GameObject.Instantiate(FiveKnights.preloadedGO["Tiso"]);
            tiso.transform.position = HeroController.instance.transform.position;
            tiso.SetActive(true);
            TisoController tc = tiso.AddComponent<TisoController>();

            Log("Done creating Tiso");
            return tc;
        }

        public static void LoadIsmaSound()
        {
            if (FiveKnights.Clips.ContainsKey("IsmaAudAgonyShoot")) return;
            
            // Audio clips
            string[] clips = 
            {
                "IsmaAudAgonyShoot", "IsmaAudAgonyIntro", "IsmaAudGroundWhip", "IsmaAudSeedBomb", "IsmaAudVineGrow", "IsmaAudVineHit",
                "IsmaAudWallGrow", "IsmaAudWallHit", "IsmaAudDungHit", "IsmaAudDungBreak", "IsmaAudAtt1", "IsmaAudAtt2", "IsmaAudAtt3",
                "IsmaAudAtt4", "IsmaAudAtt5", "IsmaAudAtt6", "IsmaAudAtt7", "IsmaAudAtt8", "IsmaAudAtt9", "IsmaAudAtt10",
                "IsmaAudBow", "IsmaAudDeath",
                "LoneIsmaIntro", "LoneIsmaLoop", "OgrimMusic", "OgrismaMusic", "IsmaAreaMusic",
                "IsmaAudTalk1", "IsmaAudTalk2", "IsmaAudTalk3", "IsmaAudTalk4", "IsmaAudTalk5", "IsmaAudTalk6",
                "IsmaAudTalkBye", "IsmaAudTalkHi", "IsmaAudTalkCharm"
            };
            foreach(string name in clips)
            {
                FiveKnights.Clips[name] = _soundBundle.LoadAsset<AudioClip>(name);
            }
        }
        
        public static void LoadIsmaBundle()
		{
            Log("Loading Isma bundle");
            if(FiveKnights.preloadedGO.TryGetValue("Isma", out var go) && go != null)
            {
                Log("Already loaded Isma");
                return;
            }

            LoadIsmaSound();

            // Animation clips
            foreach(AnimationClip c in _ismaBundle.LoadAllAssets<AnimationClip>())
            {
                Log($"Name of anim adding is {c.name}");
                FiveKnights.AnimClips[c.name] = c;
            }

            // GameObjects
            string[] gos = new string[]
            {
                "Isma", "Gulka", "Plant", "Wall", "Fool", "ThornPlant", "Seal"
            };
            foreach(string name in gos)
            {
                FiveKnights.preloadedGO[name] = _ismaBundle.LoadAsset<GameObject>(name);
                foreach(SpriteRenderer sr in FiveKnights.preloadedGO[name].GetComponentsInChildren<SpriteRenderer>(true))
                {
                    sr.material = new Material(Shader.Find("Sprites/Default"));
                }
            }
            foreach(Collider2D col in FiveKnights.preloadedGO["Isma"].GetComponentsInChildren<Collider2D>(true))
            {
                col.gameObject.layer = 11;
                col.gameObject.AddComponent<DamageHero>().damageDealt = 1;
            }
            FiveKnights.Materials["flash"] = _miscBundle.LoadAsset<Material>("UnlitFlashMat");
            FiveKnights.preloadedGO["Isma"].GetComponent<SpriteRenderer>().material = FiveKnights.Materials["flash"];

            // CC Silhouette
            foreach(Sprite s in _miscBundle.LoadAllAssets<Sprite>().Where(x => x.name.Contains("Sil_Isma_")))
            {
                ArenaFinder.Sprites[s.name] = s;
            }

            #region Custom acid
            PlayMakerFSM noskFSM = FiveKnights.preloadedGO["Nosk"].LocateMyFSM("Mimic Spider");
            GameObject acidOrig = GameObject.Instantiate(noskFSM.GetAction<FlingObjectsFromGlobalPool>("Spit 1", 1).gameObject.Value);
            acidOrig.SetActive(false);
            GameObject.DontDestroyOnLoad(acidOrig);

            // Change particle color to green
            ParticleSystem.MainModule stmain = acidOrig.transform.Find("Steam").GetComponent<ParticleSystem>().main;
            ParticleSystem.MainModule stamain = acidOrig.transform.Find("Air Steam").GetComponent<ParticleSystem>().main;
            stmain.startColor = new ParticleSystem.MinMaxGradient(new Color(128 / 255f, 226 / 255f, 169 / 255f, 217 / 255f));
            stamain.startColor = new ParticleSystem.MinMaxGradient(new Color(128 / 255f, 226 / 255f, 169 / 255f, 217 / 255f));
            // Get audio clip
            var clip = FiveKnights.preloadedGO["Nosk"].LocateMyFSM("Glob Audio")
                .GetAction<AudioPlayerOneShotSingle>("SFX", 0).audioClip.Value as AudioClip;
            // Store values
            FiveKnights.Clips["AcidSpitSnd"] = clip;
            FiveKnights.preloadedGO["AcidSpit"] = acidOrig;
            #endregion

            Log("Finished loading Isma bundle");
		}

        public static void LoadDryyaSound()
        {
            if (FiveKnights.Clips.ContainsKey("DryyaMusic")) return;
            
            // Audio
            FiveKnights.Clips["DryyaMusic"] = _soundBundle.LoadAsset<AudioClip>("DryyaMusic");
            FiveKnights.Clips["DryyaAreaMusic"] = _soundBundle.LoadAsset<AudioClip>("DryyaAreaMusic");
            string[] clips = new string[]
            {
                "1", "2", "3", "4", "5", "6", "7", "Alt1", "Alt2", "Alt3", "Alt4", "Alt5", "Alt6", "Beams1",
                "Beams2", "Beams3", "Bow", "Death", "Convo2", "Convo1", "Convo3"
            };
            foreach(string name in clips)
            {
                FiveKnights.Clips["DryyaVoice" + name] = _soundBundle.LoadAsset<AudioClip>("DryyaVoice" + name);
            }
        }
        
        public static void LoadDryyaBundle()
		{
            Log("Loading Dryya bundle");
            if(FiveKnights.preloadedGO.TryGetValue("Dryya2", out var go) && go != null)
            {
                Log("Already loaded Dryya");
                return;
            }

            LoadDryyaSound();

            // Animation clips
            foreach(var c in _dryyaBundle.LoadAllAssets<AnimationClip>())
            {
                Log($"Name of anim adding is {c.name}");
                FiveKnights.AnimClips[c.name] = c;
            }

            // GameObjects
            FiveKnights.preloadedGO["Dryya2"] = _dryyaBundle.LoadAsset<GameObject>("Dryya2");
            FiveKnights.preloadedGO["Beams"] = _dryyaBundle.LoadAsset<GameObject>("Beams");
            FiveKnights.preloadedGO["Dagger"] = _dryyaBundle.LoadAsset<GameObject>("Dagger");
            FiveKnights.preloadedGO["Beams"].GetComponent<SpriteRenderer>().material = new Material(Shader.Find("Sprites/Default"));
            FiveKnights.preloadedGO["Dagger"].GetComponent<SpriteRenderer>().material = new Material(Shader.Find("Sprites/Default"));

            // CC Silhouette
            foreach(var i in _miscBundle.LoadAllAssets<Sprite>().Where(x => x.name.Contains("Dryya_Silhouette_")))
            {
                ArenaFinder.Sprites[i.name] = i;
            }

            Log("Finished loading Dryya bundle");
        }

        public static void LoadHegemolSound()
        {
            if (FiveKnights.Clips.ContainsKey("HegArrive")) return;
            
            // Audio clips
            string[] clips = new[]
            {
                "HegArrive", "HegAttackSwing", "HegAttackHit", "HegAttackCharge", "HegDamage", "HegDamageFinal", "HegDebris", "HegDungDebris",
                "HegJump", "HegLand", "HegShockwave", "HNeutral1", "HNeutral2", "HNeutral3", "HCharge", "HHeavy1", "HHeavy2", "HDeath",
                "HGrunt1", "HGrunt2", "HGrunt3", "HGrunt4", "HTired1", "HTired2", "HTired3", "HegemolMusicIntro", "HegemolMusicLoop",
                "HegAreaMusic", "HegAreaMusicIntro", "HegAreaMusicBG"
            };
            foreach(string name in clips)
            {
                FiveKnights.Clips[name] = _soundBundle.LoadAsset<AudioClip>(name);
            }
        }

        public static void LoadHegemolBundle()
		{
            Log("Loading Hegemol bundle");
            if(FiveKnights.preloadedGO.TryGetValue("Hegemol", out var go) && go != null)
            {
                Log("Already loaded Hegemol");
                return;
            }

            LoadHegemolSound();

            // GameObjects
            FiveKnights.preloadedGO["Hegemol"] = _hegemolBundle.LoadAsset<GameObject>("Hegemol");
            FiveKnights.preloadedGO["Mace"] = _hegemolBundle.LoadAsset<GameObject>("Mace");
            FiveKnights.preloadedGO["Debris"] = _hegemolBundle.LoadAsset<GameObject>("Debris");
            FiveKnights.preloadedGO["Mace"].GetComponent<SpriteRenderer>().material = new Material(Shader.Find("Sprites/Default"));

            // Traitor shockwaves
            PlayMakerFSM fsm = FiveKnights.preloadedGO["Traitor"].LocateMyFSM("Mantis");
            FiveKnights.preloadedGO["TraitorSlam"] =
                fsm.GetAction<SpawnObjectFromGlobalPool>("Waves", 0).gameObject.Value;
            FiveKnights.Clips["TraitorSlam"] = fsm.GetAction<AudioPlayerOneShotSingle>("Waves", 4).audioClip.Value as AudioClip;

            // CC Dung ball
            GameObject ball = FiveKnights.preloadedGO["WhiteDef"].LocateMyFSM("Dung Defender").
                GetAction<SpawnObjectFromGlobalPool>("Throw 1", 1).gameObject.Value;
            FiveKnights.preloadedGO["DungBreakChunks"] = ball.LocateMyFSM("Ball Control").FsmVariables.FindFsmGameObject("Break Chunks").Value;

            // CC Silhouette
            foreach(var i in _miscBundle.LoadAllAssets<Sprite>().Where(x => x.name.Contains("hegemol_silhouette_")))
            {
                ArenaFinder.Sprites[i.name] = i;
            }

            FiveKnights.Materials["flash"] = _miscBundle.LoadAsset<Material>("UnlitFlashMat");

            Log("Finished loading Hegemol bundle");
        }

        public static void LoadZemerSound()
        {
            if (FiveKnights.Clips.ContainsKey("ZP2Intro")) return;
            
            // Audio clips
            string[] clips = new[]
            {
                "ZP2Intro","ZP1Loop", "ZAudAtt4", "ZAudP1DeathA", "ZAudP1DeathB", "ZAudP1DeathC", "ZAudP1DeathD",
                "ZAudP1Death2", "ZAudBow", "ZAudCounter", "ZAudAtt5", "ZP1Intro", "ZAudAtt2", "ZP2Loop",
                "ZAudLaser", "ZAudHoriz", "ZAudAtt3", "ZAudAtt6","AudBasicSlash1",
                "AudBigSlash", "AudBigSlash2", "AudLand", "AudDashIntro", "AudDash", "AudBasicSlash2",
                "breakable_wall_hit_1", "breakable_wall_hit_2", "Zem_Area",  "ZAudAtt7", "ZAudAtt8",
                "ZAudAtt9", "ZAudAtt10", "ZAudAtt11", "ZAudAtt12", "ZAudAtt13", 
                "ZAudAtt14", "ZAudAtt15", "ZAudAtt16",
                "ZAudMid", "ZAudTalk1", "ZAudTalk1B", "ZAudTalk2", "ZAudTalk3", "ZAudTalk4",
                "ZAudExplode"
            };
            foreach(var name in clips)
            {
                FiveKnights.Clips[name] = _soundBundle.LoadAsset<AudioClip>(name);
            }
        }

        public static void LoadZemerBundle()
		{
            Log("Loading Zemer bundle");
            if(FiveKnights.preloadedGO.TryGetValue("Zemer", out var go) && go != null)
            {
                Log("Already loaded Zemer");
                return;
            }

            LoadZemerSound();
            
            
            FiveKnights.Clips["NeedleSphere"] = (AudioClip)FiveKnights.preloadedGO["HornetSphere"].LocateMyFSM("Control")
                .GetAction<AudioPlaySimple>("Sphere A", 0).oneShotClip.Value;

            // Animation clips
            foreach(var c in _zemerBundle.LoadAllAssets<AnimationClip>())
            {
                Log($"Name of anim adding is {c.name}");
                FiveKnights.AnimClips[c.name] = c;
            }

            // GameObjects
            foreach(GameObject asset in _zemerBundle.LoadAllAssets<GameObject>())
            {
                if(asset.name == "Zemer") FiveKnights.preloadedGO["Zemer"] = asset;
                else if(asset.name == "TChild") FiveKnights.preloadedGO["TChild"] = asset;
                else if(asset.name == "NewSlash") FiveKnights.preloadedGO["SlashBeam"] = asset;
                else if(asset.name == "NewSlash2") FiveKnights.preloadedGO["SlashBeam2"] = asset;
                else if(asset.name == "SlashRingController") FiveKnights.preloadedGO["SlashRingController"] = asset;
                else if(asset.name == "SlashRingControllerNew") FiveKnights.preloadedGO["SlashRingControllerNew"] = asset;
                else if(asset.name == "AllFlowers") FiveKnights.preloadedGO["AllFlowers"] = asset;

                if(asset.GetComponent<SpriteRenderer>() == null)
                {
                    foreach(SpriteRenderer sr in asset.GetComponentsInChildren<SpriteRenderer>(true))
                    {
                        sr.material = new Material(Shader.Find("Sprites/Default"));
                    }
                }
                else asset.GetComponent<SpriteRenderer>().material = new Material(Shader.Find("Sprites/Default"));
            }
            #region Ze'mer damage
            GameObject zemer = FiveKnights.preloadedGO["Zemer"];
            foreach(Transform i in FiveKnights.preloadedGO["SlashBeam"].transform)
            {
                i.gameObject.AddComponent<DamageHero>().damageDealt = 1;
                i.gameObject.layer = 22;
            }
            foreach(Transform i in FiveKnights.preloadedGO["TChild"].transform)
            {
                i.gameObject.AddComponent<DamageHero>().damageDealt = 1;
                i.gameObject.layer = 22;
            }
            foreach(Transform i in FiveKnights.preloadedGO["SlashBeam2"].transform)
            {
                i.GetComponent<SpriteRenderer>().material = new Material(Shader.Find("Sprites/Default"));

                i.Find("HB1").gameObject.AddComponent<DamageHero>().damageDealt = 1;
                i.Find("HB2").gameObject.AddComponent<DamageHero>().damageDealt = 1;

                i.Find("HB1").gameObject.layer = 22;
                i.Find("HB2").gameObject.layer = 22;
            }
            foreach(Transform tRing in FiveKnights.preloadedGO["SlashRingControllerNew"].transform)
            {
                foreach(Transform t in tRing)
                {
                    foreach(PolygonCollider2D i in t.GetComponentsInChildren<PolygonCollider2D>(true))
                    {
                        i.gameObject.AddComponent<DamageHero>().damageDealt = 1;
                        i.gameObject.layer = 22;
                        i.gameObject.AddComponent<ParryTink>();
                    }
                }
            }
            foreach(Transform tRing in FiveKnights.preloadedGO["SlashRingController"].transform)
            {
                foreach(Transform t in tRing)
                {
                    foreach(PolygonCollider2D i in t.GetComponentsInChildren<PolygonCollider2D>(true))
                    {
                        i.gameObject.AddComponent<DamageHero>().damageDealt = 1;
                        i.gameObject.layer = 22;
                        i.gameObject.AddComponent<ParryTink>();
                    }
                }
            }
            foreach(SpriteRenderer i in zemer.GetComponentsInChildren<SpriteRenderer>(true))
            {
                i.material = new Material(Shader.Find("Sprites/Default"));

                var bc = i.gameObject.GetComponent<BoxCollider2D>();

                if(bc == null)
                    continue;
                bc.isTrigger = true;
                bc.gameObject.AddComponent<DamageHero>().damageDealt = 1;
                i.gameObject.AddComponent<Pogoable>().tar = zemer;
                bc.gameObject.layer = 22;
                if(!i.name.Contains("Zemer")) i.gameObject.AddComponent<ParryTink>();
            }
            foreach(PolygonCollider2D i in zemer.GetComponentsInChildren<PolygonCollider2D>(true))
            {
                i.isTrigger = true;
                i.gameObject.AddComponent<DamageHero>().damageDealt = 1;
                i.gameObject.AddComponent<ParryTink>();
                i.gameObject.AddComponent<Pogoable>().tar = zemer;
                i.gameObject.layer = 22;
            }

            var bcMultiDashHB = zemer.transform.Find("MultiDashParryHB").gameObject;
            bcMultiDashHB.AddComponent<ParryTink>();
            bcMultiDashHB.AddComponent<Pogoable>().tar = zemer;
            bcMultiDashHB.layer = 22;
            bcMultiDashHB.SetActive(false);
            #endregion

            // Set materials
            FiveKnights.preloadedGO["SlashBeam"].GetComponent<SpriteRenderer>().material =
                new Material(Shader.Find("Sprites/Default"));
            FiveKnights.Materials["flash"] = _miscBundle.LoadAsset<Material>("UnlitFlashMat");
            zemer.GetComponent<SpriteRenderer>().material = FiveKnights.Materials["flash"];

            // Traitor shockwave
            PlayMakerFSM fsm = FiveKnights.preloadedGO["Traitor"].LocateMyFSM("Mantis");
            FiveKnights.preloadedGO["TraitorSlam"] =
                fsm.GetAction<SpawnObjectFromGlobalPool>("Waves", 0).gameObject.Value;
            FiveKnights.Clips["TraitorSlam"] = fsm.GetAction<AudioPlayerOneShotSingle>("Waves", 4).audioClip.Value as AudioClip;

            // CC Sprites
            ArenaFinder.Sprites["ZemParticPetal"] = _miscBundle.LoadAsset<Sprite>("petal-test");
            ArenaFinder.Sprites["ZemParticDung"] = _miscBundle.LoadAsset<Sprite>("dung-test");
            foreach(Sprite i in _miscBundle.LoadAllAssets<Sprite>().Where(x => x.name.Contains("Zem_Sil_")))
            {
                ArenaFinder.Sprites[i.name] = i;
            }

            Log("Finished loading Zemer bundle");
        }

        public static void LoadTisoBundle()
		{
            Log("Loading Tiso bundle");
            if(FiveKnights.preloadedGO.TryGetValue("Tiso", out var go) && go != null)
            {
                Log("Already loaded Tiso");
                return;
            }

            // Audio clips
            TisoAudio.TisoAud = new Dictionary<string, AudioClip>();
            string[] clips =
            {
                "AudSpikeHitWall", "AudTisoJump", "AudTisoLand", "AudTisoShoot", "AudTisoSpin", "AudTisoThrowShield",
                "AudTisoWalk", "AudTisoDeath", "AudTisoRoar", "AudTisoYell", "AudLand"
            };
            foreach(string audName in clips)
            {
                TisoAudio.TisoAud[audName] = _soundBundle.LoadAsset<AudioClip>(audName);
            }
            for(int i = 1; i < 7; i++)
            {
                TisoAudio.TisoAud[$"AudTiso{i}"] = _soundBundle.LoadAsset<AudioClip>($"AudTiso{i}");
            }

            // THE ONLY THING THEY FEAR IS TISO
            FiveKnights.Clips["TisoMusicStart"] = _soundBundle.LoadAsset<AudioClip>("TisoMusicStart");
            FiveKnights.Clips["TisoMusicLoop"] = _soundBundle.LoadAsset<AudioClip>("TisoMusicLoop");

            // The man himself
            FiveKnights.preloadedGO["Tiso"] = _tisoBundle.LoadAsset<GameObject>("Tiso");
            FiveKnights.Materials["flash"] = _miscBundle.LoadAsset<Material>("UnlitFlashMat");

            Log("Finished loading Tiso bundle");
        }

		private static void Log(object o) => Modding.Logger.Log("[FiveKnights][BossLoader] " + o);
    }
}