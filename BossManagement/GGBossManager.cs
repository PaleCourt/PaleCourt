using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using FiveKnights.Dryya;
using FiveKnights.Hegemol;
using FiveKnights.Isma;
using FiveKnights.Zemer;
using HutongGames.PlayMaker.Actions;
using SFCore.Utils;
using Modding;
using UnityEngine;
using UnityEngine.Audio;
using Vasi;
using Object = UnityEngine.Object;
using ReflectionHelper = Modding.ReflectionHelper;

namespace FiveKnights.BossManagement
{
    public class GGBossManager : MonoBehaviour
    {
        private HealthManager _hm;
        private PlayMakerFSM _fsm;
        public GameObject dd; 
        private tk2dSpriteAnimator _tk;
        private List<AssetBundle> _assetBundles;
        public static bool alone;
        private bool HIT_FLAG;
        public static GGBossManager Instance;
        public Dictionary<string, AnimationClip> clips;

        public List<Animator> flowersAnim;

        public IEnumerator PlayFlowers(int toFrame=-1) 
        {
            foreach (var anim in flowersAnim) anim.enabled = true;
            yield return null;
            foreach (var anim in flowersAnim)
            {
                switch (anim.transform.parent.name)
                {
                    case "FlowersA":
                        if (toFrame == -1) yield return anim.PlayToEnd();
                        else yield return anim.WaitToFrame(toFrame + 1);
                        break;
                    case "FlowersB":
                        if (toFrame == -1) yield return anim.PlayToEnd();
                        else yield return anim.WaitToFrame(toFrame);
                        break;
                    case "FlowersC":
                        if (toFrame == -1) yield return anim.PlayToEnd();
                        else yield return anim.WaitToFrame(toFrame + 2);
                        break;
                }
                anim.enabled = false;
            }
        }
        
        private IEnumerator Start()
        {
            // set damage level
            if(CustomWP.boss != CustomWP.Boss.All && CustomWP.boss != CustomWP.Boss.Ogrim)
            {
                BossSceneController.Instance.BossLevel = CustomWP.lev;
            }
            
            Instance = this;
            if (CustomWP.boss is CustomWP.Boss.All or CustomWP.Boss.Ogrim)
            {
                dd = GameObject.Find("White Defender");
                PlayerData.instance.SetBool(nameof(PlayerData.atBench), false);
            }
            _hm = dd.GetComponent<HealthManager>();
            _fsm = dd.LocateMyFSM("Dung Defender");
            _tk = dd.GetComponent<tk2dSpriteAnimator>();
            FiveKnights.preloadedGO["WD"] = dd;

            alone = true;
            _assetBundles= new List<AssetBundle>();
            Unload();
            On.HealthManager.TakeDamage += HealthManager_TakeDamage;
            ModHooks.BeforePlayerDeadHook += BeforePlayerDied;
            string dret = PlayerData.instance.dreamReturnScene;
            PlayerData.instance.dreamReturnScene = (dret == "Waterways_13") ? dret : "White_Palace_09";
            dret = PlayerData.instance.dreamReturnScene;
            PlayerData.instance.dreamReturnScene = (CustomWP.boss == CustomWP.Boss.All) ? "Dream_04_White_Defender" : dret;
            Log("Curr Boss " + CustomWP.boss);
            //Be sure to do CustomWP.Instance.wonLastFight = true; on win
            if (CustomWP.boss == CustomWP.Boss.Isma)
            {
                FiveKnights.Clips["LoneIsmaLoop"] = ABManager.AssetBundles[ABManager.Bundle.Sound].LoadAsset<AudioClip>("LoneIsmaLoop");
                FiveKnights.Clips["LoneIsmaIntro"] = ABManager.AssetBundles[ABManager.Bundle.Sound].LoadAsset<AudioClip>("LoneIsmaIntro");

                yield return LoadIsmaBundle();
                dd.SetActive(false);
                FightController.Instance.CreateIsma(true);
                IsmaController ic = FiveKnights.preloadedGO["Isma2"].GetComponent<IsmaController>();
                yield return new WaitWhile(() => ic != null);
                if (CustomWP.wonLastFight)
                {
                    int lev = CustomWP.lev + 1;
                    var box = (object) FiveKnights.Instance.SaveSettings.CompletionIsma;
                    var fi = ReflectionHelper.GetFieldInfo(typeof(BossStatue.Completion), $"completedTier{lev}");
                    fi.SetValue(box, true);
                    FiveKnights.Instance.SaveSettings.CompletionIsma = (BossStatue.Completion) box;
                }
                var bsc = BossSceneController.Instance;
                GameObject transition = Instantiate(bsc.transitionPrefab);
                PlayMakerFSM transitionsFSM = transition.LocateMyFSM("Transitions");
                transitionsFSM.SetState("Out Statue");
                yield return new WaitForSeconds(1.0f);
                bsc.gameObject.LocateMyFSM("Dream Return").SendEvent("DREAM RETURN");
                Destroy(this);
            }
            else if (CustomWP.boss == CustomWP.Boss.Ogrim)
            {
                AssetBundle snd = ABManager.AssetBundles[ABManager.Bundle.Sound];
                FiveKnights.Clips["OgrismaMusic"] = snd.LoadAsset<AudioClip>("OgrismaMusic");

                yield return LoadIsmaBundle();
                yield return OgrimIsmaFight();
                
                if (CustomWP.wonLastFight)
                {
                    int lev = CustomWP.lev + 1;
                    var box = (object) FiveKnights.Instance.SaveSettings.CompletionIsma2;
                    var fi = ReflectionHelper.GetFieldInfo(typeof(BossStatue.Completion), $"completedTier{lev}");
                    fi.SetValue(box, true);
                    FiveKnights.Instance.SaveSettings.CompletionIsma2 = (BossStatue.Completion) box;
                }
                var bsc = BossSceneController.Instance;
                GameObject transition = Instantiate(bsc.transitionPrefab);
                PlayMakerFSM transitionsFSM = transition.LocateMyFSM("Transitions");
                transitionsFSM.SetState("Out Statue");
                yield return new WaitForSeconds(1.0f);
                bsc.gameObject.LocateMyFSM("Dream Return").SendEvent("DREAM RETURN");

				Destroy(this);
            }
            else if (CustomWP.boss == CustomWP.Boss.Dryya)
            {
                yield return LoadDryyaBundle();
                
                dd.SetActive(false);
                DryyaSetup dc = FightController.Instance.CreateDryya();
                yield return new WaitWhile(() => dc != null);
                if (CustomWP.wonLastFight)
                {
                    int lev = CustomWP.lev + 1;
                    var box = (object) FiveKnights.Instance.SaveSettings.CompletionDryya;
                    var fi = ReflectionHelper.GetFieldInfo(typeof(BossStatue.Completion), $"completedTier{lev}");
                    fi.SetValue(box, true);
                    FiveKnights.Instance.SaveSettings.CompletionDryya = (BossStatue.Completion) box;
                }
                yield return new WaitForSeconds(5.0f);

                var bsc = BossSceneController.Instance;
                GameObject transition = Instantiate(bsc.transitionPrefab);
                PlayMakerFSM transitionsFSM = transition.LocateMyFSM("Transitions");
                transitionsFSM.SetState("Out Statue");
                yield return new WaitForSeconds(1.0f);
                bsc.gameObject.LocateMyFSM("Dream Return").SendEvent("DREAM RETURN");

                yield return null;

                Destroy(this);
            }
            else if (CustomWP.boss == CustomWP.Boss.Hegemol)
            {
                yield return LoadHegemolBundle();
                
                dd.SetActive(false);
                HegemolController hegemolCtrl = FightController.Instance.CreateHegemol();
                GameCameras.instance.cameraShakeFSM.FsmVariables.FindFsmBool("RumblingMed").Value = false;
                yield return new WaitWhile(() => hegemolCtrl != null);
                if (CustomWP.wonLastFight)
                {
                    int lev = CustomWP.lev + 1;
                    var box = (object) FiveKnights.Instance.SaveSettings.CompletionHegemol;
                    var fi = ReflectionHelper.GetFieldInfo(typeof(BossStatue.Completion), $"completedTier{lev}");
                    fi.SetValue(box, true);
                    FiveKnights.Instance.SaveSettings.CompletionHegemol = (BossStatue.Completion) box;
                }
                var bsc = BossSceneController.Instance;
                GameObject transition = Instantiate(bsc.transitionPrefab);
                PlayMakerFSM transitionsFSM = transition.LocateMyFSM("Transitions");
                transitionsFSM.SetState("Out Statue");
                yield return new WaitForSeconds(1.0f);
                bsc.gameObject.LocateMyFSM("Dream Return").SendEvent("DREAM RETURN");
                Destroy(this);
            }
            else if (CustomWP.boss == CustomWP.Boss.Ze || CustomWP.boss == CustomWP.Boss.Mystic)
            {
                yield return LoadZemerBundle();
                dd.SetActive(false);
                GameCameras.instance.cameraShakeFSM.FsmVariables.FindFsmBool("RumblingMed").Value = false;
                yield return null;
                ZemerController zc = FightController.Instance.CreateZemer();
                GameObject zem = zc.gameObject;
                yield return new WaitWhile(() => zc != null);
                if (zem == null)
                {
                    Log("Zem did not exist so destroying");
                    Destroy(this);
                    yield break;
                }
                ZemerControllerP2 zc2 = zem.GetComponent<ZemerControllerP2>();
                yield return new WaitWhile(() => zc2 != null);
                if (CustomWP.wonLastFight)
                {
                    int lev = CustomWP.lev + 1;
                    if (CustomWP.boss == CustomWP.Boss.Ze)
                    {
                        var box = (object) FiveKnights.Instance.SaveSettings.CompletionZemer;
                        var fi = ReflectionHelper.GetFieldInfo(typeof(BossStatue.Completion), $"completedTier{lev}");
                        fi.SetValue(box, true);
                        FiveKnights.Instance.SaveSettings.CompletionZemer = (BossStatue.Completion) box;
                    }
                    else
                    {
                        var box = (object) FiveKnights.Instance.SaveSettings.CompletionZemer2;
                        var fi = ReflectionHelper.GetFieldInfo(typeof(BossStatue.Completion), $"completedTier{lev}");
                        fi.SetValue(box, true);
                        FiveKnights.Instance.SaveSettings.CompletionZemer2 = (BossStatue.Completion) box;
                    }
                }
                var bsc = BossSceneController.Instance;
                GameObject transition = Instantiate(bsc.transitionPrefab);
                PlayMakerFSM transitionsFSM = transition.LocateMyFSM("Transitions");
                transitionsFSM.SetState("Out Statue");
                yield return new WaitForSeconds(1.0f);
                bsc.gameObject.LocateMyFSM("Dream Return").SendEvent("DREAM RETURN");

                Destroy(this);
            }
            else if (CustomWP.boss == CustomWP.Boss.All)
            {
                yield return null;
                //bool flag = false;
                //StartCoroutine(Wow());
                var a1 = StartCoroutine(LoadHegemolBundle());
                var a2 = StartCoroutine(LoadIsmaBundle());
                var a3 = StartCoroutine(LoadDryyaBundle());
                var a4 = StartCoroutine(LoadZemerBundle());

                yield return a1;
                yield return a2;
                yield return a3;
                yield return a4;
                

                GameObject flowers = Instantiate(FiveKnights.preloadedGO["AllFlowers"]);
                flowersAnim = new List<Animator>();
                flowers.SetActive(true);
                foreach (Transform t in flowers.transform)
                {
                    foreach (Animator anim in t.GetComponentsInChildren<Animator>(true))
                    {
                        flowersAnim.Add(anim);
                        switch (anim.transform.parent.name)
                        {
                            case "FlowersA":
                                anim.Play("F1Grow", -1, 0f);
                                break;
                            case "FlowersB":
                                anim.Play("F2Grow", -1, 0f);
                                break;
                            case "FlowersC":
                                anim.Play("F3Grow", -1, 0f);
                                break;
                        }
                        anim.enabled = true;
                    }
                }
                yield return null;

                foreach (Animator anim in flowersAnim)
                {
                    anim.enabled = false;
                }

                AssetBundle snd = ABManager.AssetBundles[ABManager.Bundle.Sound];
                FiveKnights.Clips["OgrismaMusic"] = snd.LoadAsset<AudioClip>("OgrismaMusic");

                yield return OgrimIsmaFight();

                yield return new WaitForSeconds(1.5f);
                
                GameObject dryyaSilhouette = GameObject.Find("Silhouette Dryya");
                SpriteRenderer sr = dryyaSilhouette.GetComponent<SpriteRenderer>();
                dryyaSilhouette.transform.localScale *= 1.2f;
                DryyaSetup dc = FightController.Instance.CreateDryya();
                sr.sprite = ArenaFinder.Sprites["Dryya_Silhouette_1"];
                yield return new WaitForSeconds(0.1f);
                sr.sprite = ArenaFinder.Sprites["Dryya_Silhouette_2"];
                yield return new WaitForSeconds(0.1f);
                sr.sprite = ArenaFinder.Sprites["Dryya_Silhouette_3"];
                yield return new WaitForSeconds(0.1f);
                Destroy(dryyaSilhouette);
                yield return new WaitForSeconds(0.5f);
                
                yield return new WaitWhile(() => dc != null);

                yield return new WaitForSeconds(3f);

                GameObject hegSil = GameObject.Find("Silhouette Hegemol");
                SpriteRenderer sr2 = hegSil.GetComponent<SpriteRenderer>();
                hegSil.transform.localScale *= 1.2f;
                HegemolController hegemolCtrl = FightController.Instance.CreateHegemol();
                for (int i = 0; i <= 5; i++)
                {
                    sr2.sprite = ArenaFinder.Sprites["hegemol_silhouette_"+i];
                    yield return new WaitForSeconds(0.1f);
                }
                sr2.sprite = ArenaFinder.Sprites["hegemol_silhouette_6"];
                hegSil.AddComponent<Rigidbody2D>().gravityScale = 0;
                hegSil.GetComponent<Rigidbody2D>().velocity = new Vector2(0f,50f);
                yield return new WaitForSeconds(0.1f);
                sr2.sprite = ArenaFinder.Sprites["hegemol_silhouette_7"];
                yield return new WaitForSeconds(0.5f);
                Destroy(hegSil);
                yield return new WaitWhile(() => hegemolCtrl != null);

                yield return new WaitForSeconds(1.5f);

                // Silhouette is handled in Zemer code now
                ZemerController zc = FightController.Instance.CreateZemer();
                GameObject zem = zc.gameObject;

                yield return new WaitForSeconds(0.5f);
                
                // Grow flowers if in pantheon
                StartCoroutine(PlayFlowers( 7));

                //Destroy(zemSil);
                yield return new WaitWhile(() => zc != null);
                ZemerControllerP2 zc2 = zem.GetComponent<ZemerControllerP2>();
                FiveKnights.Instance.SaveSettings.CompletionZemer2.isUnlocked = true;
                
                yield return new WaitWhile(() => zc2 != null);
                
                Log("Won!");
                
                PlayMakerFSM pm = GameCameras.instance.tk2dCam.gameObject.LocateMyFSM("CameraFade");
                pm.SendEvent("FADE OUT");
                yield return null;
                HeroController.instance.MaxHealth();
                yield return new WaitForSeconds(0.5f);
                GameManager.instance.BeginSceneTransition(new GameManager.SceneLoadInfo
                {
                    SceneName = "hidden_reward_room",
                    EntryGateName = "door_reward_room",
                    Visualization = GameManager.SceneLoadVisualizations.Default,
                    WaitForSceneTransitionCameraFade = false,
                    PreventCameraFadeOut = true,
                    EntryDelay = 0
                });
                Destroy(this);
            }
        }

        private IEnumerator OgrimIsmaFight()
        {
			On.HeroController.TakeDamage += HeroControllerTakeDamage;

            // Set variables and edit FSM
            dd = GameObject.Find("White Defender");
            dd.GetComponent<DamageHero>().damageDealt = 1;
            dd.Find("Throw Swipe").gameObject.GetComponent<DamageHero>().damageDealt = 1;
            _hm = dd.GetComponent<HealthManager>();
            _fsm = dd.LocateMyFSM("Dung Defender");
            _tk = dd.GetComponent<tk2dSpriteAnimator>();
            alone = false;
            _hm.hp = 950;
            _fsm.GetAction<Wait>("Rage Roar", 9).time = 1.5f;
            _fsm.FsmVariables.FindFsmBool("Raged").Value = true;
            yield return new WaitForSeconds(1f);

            // Begin fight
            GameCameras.instance.cameraFadeFSM.Fsm.SetState("FadeIn");
            PlayMakerFSM burrow = GameObject.Find("Burrow Effect").LocateMyFSM("Burrow Effect");
            yield return new WaitWhile(() => _hm.hp > 600);
            HIT_FLAG = false;

            // Transition to phase 2
			yield return new WaitWhile(() => !HIT_FLAG);
            FiveKnights.Instance.SaveSettings.CompletionIsma2.isUnlocked = true;
            PlayMusic(null, 1f);
            if(dd.transform.position.y < 9f) dd.transform.position = new Vector3(dd.transform.position.x, 9f, dd.transform.position.z);
			PlayerData.instance.isInvincible = true;
            dd.layer = (int)GlobalEnums.PhysLayers.CORPSE;
            _fsm.SetState("Stun Set");

            // Disable his burrow and ground spikes
            burrow.enabled = true;
            burrow.SendEvent("BURROW END");
            foreach(PlayMakerFSM pillar in dd.Find("Slam Pillars").GetComponentsInChildren<PlayMakerFSM>())
			{
                if(pillar.ActiveStateName == "Up" || pillar.ActiveStateName == "Hit")
                {
                    pillar.gameObject.GetComponent<MeshRenderer>().enabled = false;
                    pillar.SetState("Dormant");
                    pillar.FsmVariables.FindFsmGameObject("Chunks").Value.GetComponent<ParticleSystem>().Play();
                }
                else
				{
                    pillar.enabled = false;
                }
            }

            yield return new WaitWhile(() => _fsm.ActiveStateName != "Stun Land");
            _fsm.enabled = false;
            burrow.enabled = false;

            // Delay Isma appearing slightly
            yield return new WaitForSeconds(1f);
            FightController.Instance.CreateIsma(false);
            IsmaController ic = FiveKnights.preloadedGO["Isma2"].GetComponent<IsmaController>();
            
            // Grow flowers if in pantheon
            if (flowersAnim != null)
            {
                StartCoroutine(PlayFlowers(2));
            }

            // After Isma falls down
            yield return new WaitWhile(() => !ic.introDone);
			_fsm.enabled = true;
            _fsm.SetState("Stun Recover");
            yield return null;

            // WD scream
            // This is to prevent WD from entering any other state after Stun Recover
            _fsm.InsertMethod("Idle", 1, () => _fsm.SetState("Rage Roar"));
            yield return new WaitWhile(() => _fsm.ActiveStateName == "Stun Recover");
            yield return new WaitWhile(() => _fsm.ActiveStateName == "Rage Roar");
            _fsm.RemoveAction("Idle", 1);
            dd.layer = (int)GlobalEnums.PhysLayers.ENEMIES;
            PlayerData.instance.isInvincible = false;
            burrow.enabled = true;
            foreach(PlayMakerFSM pillar in dd.Find("Slam Pillars").GetComponentsInChildren<PlayMakerFSM>())
            {
                pillar.enabled = true;
            }
            yield return new WaitWhile(() => !_fsm.ActiveStateName.Contains("Tunneling"));
            yield return new WaitWhile(() => ic != null);

            On.HeroController.TakeDamage -= HeroControllerTakeDamage;
        }

		private void HeroControllerTakeDamage(On.HeroController.orig_TakeDamage orig, HeroController self, GameObject go, GlobalEnums.CollisionSide damageSide, int damageAmount, int hazardType)
		{
            orig(self, go, damageSide, damageAmount > 1 ? 1 : damageAmount, hazardType);
        }

		public void BeforePlayerDied()
        {
            Log("RAN");
        }
        
        private void HealthManager_TakeDamage(On.HealthManager.orig_TakeDamage orig, HealthManager self, HitInstance hitInstance)
        {
            if (self.name.Contains("White Defender"))
            {
                HIT_FLAG = true;
            }
            orig(self, hitInstance);
        }

        private void OnCollisionEnter2D(Collision2D c)
        {
            if (!_tk.IsPlaying("Roll")) return;
            if (c.gameObject.layer == 8 && c.gameObject.name.Contains("Front"))
            {
                _fsm.SetState("RJ Wall");
            }
        }

        public void PlayMusic(AudioClip clip, float vol = 0f)
        {
            MusicCue musicCue = ScriptableObject.CreateInstance<MusicCue>();
            MusicCue.MusicChannelInfo channelInfo = new MusicCue.MusicChannelInfo();
            Mirror.SetField(channelInfo, "clip", clip);
            //channelInfo.SetAttr("clip", clip);
            MusicCue.MusicChannelInfo[] channelInfos = new MusicCue.MusicChannelInfo[]
            {
                channelInfo, null, null, null, null, null
            };
            Mirror.SetField(musicCue, "channelInfos", channelInfos);
            //musicCue.SetAttr("channelInfos", channelInfos);
            var yoursnapshot = Resources.FindObjectsOfTypeAll<AudioMixer>().First(x => x.name == "Music").FindSnapshot("Main Only");
            yoursnapshot.TransitionTo(0);
            GameManager.instance.AudioManager.ApplyMusicCue(musicCue, 0, 0, false);
        }

        private void Log(object o)
        {
            Modding.Logger.Log("[GGBossManager] " + o);
        }
        
        private IEnumerator LoadIsmaBundle()
        {
            Log("Loading Isma Bundle");

            AssetBundle snd = ABManager.AssetBundles[ABManager.Bundle.Sound];
            string[] arr = new string[]
            {
                "IsmaAudAgonyShoot", "IsmaAudAgonyIntro", "IsmaAudGroundWhip", "IsmaAudSeedBomb", "IsmaAudVineGrow", "IsmaAudVineHit", 
                "IsmaAudWallGrow", "IsmaAudWallHit", "IsmaAudDungHit", "IsmaAudDungBreak"
            };
            foreach(string name in arr)
            {
                FiveKnights.Clips[name] = snd.LoadAsset<AudioClip>(name);
            }

            if (FiveKnights.preloadedGO.TryGetValue("Isma", out var go) && go != null)
            {
                Log("Already loaded Isma");
                yield break;
            }

            AssetBundle ab = ABManager.AssetBundles[ABManager.Bundle.GIsma];
            List<GameObject> gos;
            
            if (CustomWP.boss == CustomWP.Boss.All)
            {  
                FiveKnights.Instance.SaveSettings.gotCharms[3] = false;
                var r1 = ab.LoadAssetAsync<GameObject>("Isma");
                var r2 = ab.LoadAssetAsync<GameObject>("Gulka");
                var r3 = ab.LoadAssetAsync<GameObject>("Plant");
                var r4 = ab.LoadAssetAsync<GameObject>("Wall");
                var r5 = ab.LoadAssetAsync<GameObject>("Fool");
                var r6 = ab.LoadAssetAsync<GameObject>("ThornPlant");

                yield return r1;
                yield return r2;
                yield return r3;
                yield return r4;
                yield return r5;
                yield return r6;

                gos = new List<GameObject>
                {
                    r1.asset as GameObject, 
                    r2.asset as GameObject, 
                    r3.asset as GameObject, 
                    r4.asset as GameObject,
                    r5.asset as GameObject,
                    r6.asset as GameObject
                };
            }
            else
            {
                gos = new List<GameObject>
                {
                    ab.LoadAsset<GameObject>("Isma"),
                    ab.LoadAsset<GameObject>("Gulka"),
                    ab.LoadAsset<GameObject>("Plant"),
                    ab.LoadAsset<GameObject>("Wall"),
                    ab.LoadAsset<GameObject>("Fool"),
                    ab.LoadAsset<GameObject>("ThornPlant")
                };
            }
            
            foreach (var c in ab.LoadAllAssets<AnimationClip>())
            {
                Log($"Name of anim adding is {c.name}");
                FiveKnights.AnimClips[c.name] = c;
            }
            
            foreach (var i in gos)
            {
                FiveKnights.preloadedGO[i.name] = i;
                if (i.GetComponent<SpriteRenderer>() == null)
                {
                    foreach (SpriteRenderer sr in i.GetComponentsInChildren<SpriteRenderer>(true))
                    {
                        sr.material = new Material(Shader.Find("Sprites/Default"));
                    }
                }
                else i.GetComponent<SpriteRenderer>().material = new Material(Shader.Find("Sprites/Default"));
            }

            if (CustomWP.boss != CustomWP.Boss.All)
            {
                AssetBundle ab2 = ABManager.AssetBundles[ABManager.Bundle.GArenaIsma];
                FiveKnights.preloadedGO["ismaBG"] = ab2.LoadAsset<GameObject>("gg_dung_set (1)");
            }

            Log("Finished Loading Isma Bundle");
        }
        
        private IEnumerator LoadDryyaBundle()
        {
            Log("Loading Dryya Bundle");
            if (FiveKnights.preloadedGO.TryGetValue("Dryya2", out var go) && go != null)
            {
                Log("broke Dryya");
                yield break;
            }

            AssetBundle dryyaAssetBundle = ABManager.AssetBundles[ABManager.Bundle.GDryya];
            
            foreach (var c in dryyaAssetBundle.LoadAllAssets<AnimationClip>())
            {
                Log($"Name of anim adding is {c.name}");
                FiveKnights.AnimClips[c.name] = c;
            }
            
            if (CustomWP.boss == CustomWP.Boss.All)
            {
                var r1 = dryyaAssetBundle.LoadAssetAsync<GameObject>("Dryya2");
                var r2 =  dryyaAssetBundle.LoadAssetAsync<GameObject>("Stab Effect");
                var r3 = dryyaAssetBundle.LoadAssetAsync<GameObject>("Dive Effect");
                var r4 = dryyaAssetBundle.LoadAssetAsync<GameObject>("Beams");
                var r5 = dryyaAssetBundle.LoadAssetAsync<GameObject>("Dagger");

                yield return r1;
                yield return r2;
                yield return r3;
                yield return r4;
                yield return r5;

                FiveKnights.preloadedGO["Dryya2"] = r1.asset as GameObject;
                FiveKnights.preloadedGO["Stab Effect"] = r2.asset as GameObject;
                FiveKnights.preloadedGO["Dive Effect"] = r3.asset as GameObject;
                FiveKnights.preloadedGO["Beams"] = r4.asset as GameObject;
                FiveKnights.preloadedGO["Dagger"] = r5.asset as GameObject;
            }
            else
            {
                FiveKnights.preloadedGO["Dryya2"] = dryyaAssetBundle.LoadAsset<GameObject>("Dryya2");
                FiveKnights.preloadedGO["Stab Effect"] = dryyaAssetBundle.LoadAsset<GameObject>("Stab Effect");
                FiveKnights.preloadedGO["Dive Effect"] = dryyaAssetBundle.LoadAsset<GameObject>("Dive Effect");
                FiveKnights.preloadedGO["Beams"] = dryyaAssetBundle.LoadAsset<GameObject>("Beams");
                FiveKnights.preloadedGO["Dagger"] = dryyaAssetBundle.LoadAsset<GameObject>("Dagger");
            }
            FiveKnights.preloadedGO["Beams"].GetComponent<SpriteRenderer>().material = new Material(Shader.Find("Sprites/Default"));
            FiveKnights.preloadedGO["Dagger"].GetComponent<SpriteRenderer>().material = new Material(Shader.Find("Sprites/Default"));

            Log("Finished Loading Dryya Bundle");
        }

        private IEnumerator LoadHegemolBundle()
        {
            Log("Loading Hegemol Bundle");
            if(FiveKnights.preloadedGO.TryGetValue("Hegemol", out var go) && go != null)
            {
                Log("Already Loaded Hegemol");
                yield break;
            }

            yield return null;

            PlayMakerFSM fsm = FiveKnights.preloadedGO["Traitor"].LocateMyFSM("Mantis");
            FiveKnights.preloadedGO["TraitorSlam"] =
                fsm.GetAction<SpawnObjectFromGlobalPool>("Waves", 0).gameObject.Value;
            FiveKnights.Clips["TraitorSlam"] = fsm.GetAction<AudioPlayerOneShotSingle>("Waves", 4).audioClip.Value as AudioClip;

            AssetBundle hegemolBundle = ABManager.AssetBundles[ABManager.Bundle.GHegemol];

            if(CustomWP.boss == CustomWP.Boss.All)
            {
                var r1 = hegemolBundle.LoadAssetAsync<GameObject>("Hegemol");
                var r2 = hegemolBundle.LoadAssetAsync<GameObject>("Mace");
                var r3 = hegemolBundle.LoadAssetAsync<GameObject>("Debris");

                yield return r1;
                yield return r2;
                yield return r3;

                FiveKnights.preloadedGO["Hegemol"] = r1.asset as GameObject;
                FiveKnights.preloadedGO["Mace"] = r2.asset as GameObject;
                FiveKnights.preloadedGO["Debris"] = r3.asset as GameObject;
            }
            else
            {
                FiveKnights.preloadedGO["Hegemol"] = hegemolBundle.LoadAsset<GameObject>("Hegemol");
                FiveKnights.preloadedGO["Mace"] = hegemolBundle.LoadAsset<GameObject>("Mace");
                FiveKnights.preloadedGO["Debris"] = hegemolBundle.LoadAsset<GameObject>("Debris");
            }
            FiveKnights.preloadedGO["Mace"].GetComponent<SpriteRenderer>().material = new Material(Shader.Find("Sprites/Default"));

            Log("Finished Loading Hegemol Bundle");
        }
        
        private IEnumerator LoadZemerBundle()
        {
            Log("Loading Zemer Bundle");
            
            if (FiveKnights.preloadedGO.TryGetValue("Zemer", out var go) && go != null)
            {
                Log("broke Zemer");
                yield break;
            }

            Object[] allassets;
            
            PlayMakerFSM fsm = FiveKnights.preloadedGO["Traitor"].LocateMyFSM("Mantis");
            FiveKnights.preloadedGO["TraitorSlam"] =
                fsm.GetAction<SpawnObjectFromGlobalPool>("Waves", 0).gameObject.Value;
            FiveKnights.Clips["TraitorSlam"] = fsm.GetAction<AudioPlayerOneShotSingle>("Waves", 4).audioClip.Value as AudioClip;
            
            FiveKnights.Clips["NeedleSphere"] = (AudioClip) FiveKnights.preloadedGO["HornetSphere"].LocateMyFSM("Control")
                .GetAction<AudioPlaySimple>("Sphere A", 0).oneShotClip.Value;

            AssetBundle ab = ABManager.AssetBundles[ABManager.Bundle.GZemer];

            if (CustomWP.boss == CustomWP.Boss.All)
            {
                var r1 = ab.LoadAllAssetsAsync<GameObject>();
                
                yield return r1;
                
                allassets = r1.allAssets;
            }
            else
            {
                allassets = ab.LoadAllAssets<GameObject>();
            }

            foreach (var o in allassets)
            {
                var i = (GameObject) o;
                if (i.name == "Zemer") FiveKnights.preloadedGO["Zemer"] = i;
                if (i.name == "TChild") FiveKnights.preloadedGO["TChild"] = i;
                else if (i.name == "NewSlash") FiveKnights.preloadedGO["SlashBeam"] = i;
                else if (i.name == "NewSlash2") FiveKnights.preloadedGO["SlashBeam2"] = i;
                else if (i.name == "SlashRingController") FiveKnights.preloadedGO["SlashRingController"] = i;
                else if (i.name == "SlashRingControllerNew") FiveKnights.preloadedGO["SlashRingControllerNew"] = i;
                else if (i.name == "AllFlowers") FiveKnights.preloadedGO["AllFlowers"] = i;
                yield return null;
                if (i.GetComponent<SpriteRenderer>() == null)
                {
                    foreach (SpriteRenderer sr in i.GetComponentsInChildren<SpriteRenderer>(true))
                    {
                        sr.material = new Material(Shader.Find("Sprites/Default"));
                    }
                }
                else i.GetComponent<SpriteRenderer>().material = new Material(Shader.Find("Sprites/Default"));
            }

            FiveKnights.preloadedGO["SlashBeam"].GetComponent<SpriteRenderer>().material =
                new Material(Shader.Find("Sprites/Default"));

            foreach (var c in ab.LoadAllAssets<AnimationClip>())
            {
                Log($"Name of anim adding is {c.name}");
                FiveKnights.AnimClips[c.name] = c;
            }

            Log("Finished Loading Zemer Bundle");
        }
        
        private void OnDestroy()
        {
            Unload();
        }

        private void Unload()
        {
            ModHooks.BeforePlayerDeadHook -= BeforePlayerDied;
            On.HealthManager.TakeDamage -= HealthManager_TakeDamage;
            On.HeroController.TakeDamage -= HeroControllerTakeDamage;
        }
    }
}