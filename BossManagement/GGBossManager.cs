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
using Object = UnityEngine.Object;
using ReflectionHelper = Modding.ReflectionHelper;
using TMPro;

namespace FiveKnights.BossManagement
{
    public class GGBossManager : MonoBehaviour
    {
        private HealthManager _hm;
        private PlayMakerFSM _fsm;
        public GameObject dd; 
        private tk2dSpriteAnimator _tk;
        public static bool alone;
        private bool HIT_FLAG;
        public static GGBossManager Instance;
        public Dictionary<string, AnimationClip> clips;

        public List<Animator> flowersAnim;
        public List<Animator> flowersGlow;

        private void StartFlowers()
        {
            GameObject flowers = Instantiate(FiveKnights.preloadedGO["AllFlowers"]);
            flowersAnim = new List<Animator>();
            flowersGlow = new List<Animator>();
            flowers.SetActive(true);
            foreach (Transform group in flowers.transform)
            {
                GameObject glowOld = group.Find("Flower").Find("Glow").gameObject;
                glowOld.SetActive(false);
                flowersGlow.Add(glowOld.GetComponent<Animator>());
                foreach (Transform flower in group)
                {
                    Animator anim = flower.GetComponent<Animator>();
                    flowersAnim.Add(anim);
                    anim.Play("Grow", -1, 0f);
                    // dont want glow for flowers that are in the front
                    if (anim.name == "Flower" || anim.GetComponent<SpriteRenderer>().color == Color.black) continue;
                    GameObject newGlow = Instantiate(glowOld, anim.transform, false);
                    newGlow.name = "Glow";
                    newGlow.transform.localPosition = new Vector3(glowOld.transform.localPosition.x,
                        glowOld.transform.localPosition.y, -0.0001f);
                    Animator anim2 = newGlow.GetComponent<Animator>();
                    flowersGlow.Add(anim2);
                    newGlow.SetActive(false);
                }
            }
        }
        
        public void PlayFlowers(int step)
        {
            var types = new[]
            {
                new [] {2, 5, 9}, 
                new [] {1, 3, 6}, 
                new [] {3, 5, 9}
            };
            
            foreach (var anim in flowersAnim)
            {
                string fName = anim.transform.parent.name;
                int fType = fName == "FlowersA" ? 0 : (fName == "FlowersB" ? 1 : 2);
                StartCoroutine(DoFlower(anim, fType));
            }

            IEnumerator DoFlower(Animator anim, int type)
            {
                anim.enabled = true;
                yield return new WaitForEndOfFrame();
                yield return anim.WaitToFrame(types[type][step]);
                anim.enabled = false;
            }
        }
        
        private IEnumerator Start()
        {
            // set damage level
            if(CustomWP.boss != CustomWP.Boss.All && CustomWP.boss != CustomWP.Boss.Ogrim)
            {
				// TODO UNCOMMENT
				BossSceneController.Instance.BossLevel = CustomWP.lev;
			}
            
            Instance = this;
			ReflectionHelper.SetProperty(GameManager.instance, nameof(GameManager.sm), Object.FindObjectOfType<SceneManager>());

            dd = Instantiate(FiveKnights.preloadedGO["WhiteDef"]);
            dd.SetActive(false);
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
            Unload();
            On.HealthManager.TakeDamage += HealthManagerTakeDamage;
			On.HealthManager.ApplyExtraDamage += HealthManagerApplyExtraDamage;
			On.HealthManager.Die += HealthManagerDie;
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

                BossLoader.LoadIsmaBundle();
                IsmaController ic = BossLoader.CreateIsma(true);
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
                BossLoader.LoadIsmaBundle();
                // Manually play music for now because the original scene is missing it
                PlayMusic(FiveKnights.Clips["OgrimMusic"], 1f);
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
                BossLoader.LoadDryyaBundle();
                DryyaSetup dc = BossLoader.CreateDryya();

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
                BossLoader.LoadHegemolBundle();
                HegemolController hegemolCtrl = BossLoader.CreateHegemol();
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
                BossLoader.LoadZemerBundle();
                GameCameras.instance.cameraShakeFSM.FsmVariables.FindFsmBool("RumblingMed").Value = false;
                yield return null;
                ZemerController zc = BossLoader.CreateZemer();
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
                BossLoader.LoadIsmaBundle();
                BossLoader.LoadDryyaBundle();
                BossLoader.LoadHegemolBundle();
                BossLoader.LoadZemerBundle();

				GameObject bscDummy = new("BSC Dummy");
				bscDummy.SetActive(false);
				BossSceneController bsc = BossSceneController.Instance = bscDummy.AddComponent<BossSceneController>();
				bsc.bosses = new HealthManager[0];
				ReflectionHelper.SetProperty(bsc, nameof(BossSceneController.BossHealthLookup), new Dictionary<HealthManager, BossSceneController.BossHealthDetails>());
                
                yield return null;

                yield return OgrimIsmaFight();

                yield return new WaitForSeconds(1.5f);
                
                GameObject dryyaSilhouette = GameObject.Find("Silhouette Dryya");
                SpriteRenderer sr = dryyaSilhouette.GetComponent<SpriteRenderer>();
                dryyaSilhouette.transform.localScale *= 1.2f;
				DryyaSetup dc = BossLoader.CreateDryya();
				sr.sprite = ArenaFinder.Sprites["Dryya_Silhouette_1"];
                yield return new WaitForSeconds(0.1f);
                sr.sprite = ArenaFinder.Sprites["Dryya_Silhouette_2"];
                yield return new WaitForSeconds(0.1f);
                sr.sprite = ArenaFinder.Sprites["Dryya_Silhouette_3"];
                yield return new WaitForSeconds(0.1f);
                Destroy(dryyaSilhouette);
                yield return new WaitForSeconds(0.5f);

				yield return new WaitWhile(() => dc != null);

				yield return new WaitForSeconds(3.5f);

                GameObject hegSil = GameObject.Find("Silhouette Hegemol");
                SpriteRenderer sr2 = hegSil.GetComponent<SpriteRenderer>();
                hegSil.transform.localScale *= 1.55f;
                hegSil.transform.position += 0.1f * Vector3.left;
                for (int i = 1; i <= 5; i++)
                {
                    sr2.sprite = ArenaFinder.Sprites["hegemol_silhouette_"+i];
                    yield return new WaitForSeconds(0.1f);
                }
                sr2.sprite = ArenaFinder.Sprites["hegemol_silhouette_6"];
                hegSil.AddComponent<Rigidbody2D>().gravityScale = 0;
                hegSil.GetComponent<Rigidbody2D>().velocity = new Vector2(0f, 50f);
                yield return new WaitForSeconds(0.1f);
                sr2.sprite = ArenaFinder.Sprites["hegemol_silhouette_7"];
                yield return new WaitForSeconds(0.5f);
                Destroy(hegSil);

                HegemolController hegemolCtrl = BossLoader.CreateHegemol();
                yield return new WaitWhile(() => hegemolCtrl != null);

				yield return new WaitForSeconds(1.5f);

                // Silhouette is handled in Zemer code now
                ZemerController zc = BossLoader.CreateZemer();
                GameObject zem = zc.gameObject;

                yield return new WaitForSeconds(0.5f);
                
                // Grow flowers if in pantheon
                PlayFlowers(1);

                yield return new WaitWhile(() => zc != null);
                ZemerControllerP2 zc2 = zem.GetComponent<ZemerControllerP2>();
                FiveKnights.Instance.SaveSettings.CompletionZemer2.isUnlocked = true;
                
                yield return new WaitWhile(() => zc2 != null);
                
                Log("Won!");
                GameManager.instance.AwardAchievement("PALE_COURT_PANTH_ACH");
                FiveKnights.Instance.SaveSettings.ChampionsCallClears++;

                yield return new WaitForSeconds(0.5f);
                CCDreamExit();
            }
        }

		private IEnumerator OgrimIsmaFight()
        {
            // This is to prevent Ogrim from dealing 2 masks of damage with certain attacks, which happens for...some reason
			On.HeroController.TakeDamage += HeroControllerTakeDamage;

            // Set variables and edit FSM
            dd = GameObject.Find("White Defender");
            dd.GetComponent<DamageHero>().damageDealt = 1;
            dd.Find("Throw Swipe").gameObject.GetComponent<DamageHero>().damageDealt = 1;
            EnemyDreamnailReaction dreamNailReaction = dd.GetComponent<EnemyDreamnailReaction>();
            Vasi.Mirror.SetField(dreamNailReaction, "convoAmount", 2);
            dreamNailReaction.SetConvoTitle("OGRIM_GG_DREAM");

            _hm = dd.GetComponent<HealthManager>();
            _fsm = dd.LocateMyFSM("Dung Defender");
            _tk = dd.GetComponent<tk2dSpriteAnimator>();
            alone = false;
            _hm.hp = 351;
            _fsm.GetAction<Wait>("Rage Roar", 9).time = 1.5f;
            _fsm.FsmVariables.FindFsmBool("Raged").Value = true;
            yield return new WaitForSeconds(1f);

            // Begin fight
            GameCameras.instance.cameraFadeFSM.Fsm.SetState("FadeIn");
            PlayMakerFSM burrow = GameObject.Find("Burrow Effect").LocateMyFSM("Burrow Effect");

            yield return new WaitWhile(() => _hm.hp > 1);
            HIT_FLAG = false;

            // Transition to phase 2
			yield return new WaitWhile(() => !HIT_FLAG);
            
            FiveKnights.Instance.SaveSettings.CompletionIsma2.isUnlocked = true;
            PlayMusic(null, 1f);
            if(dd.transform.position.y < 9f) dd.transform.position = new Vector3(dd.transform.position.x, 9f, dd.transform.position.z);
			PlayerData.instance.isInvincible = true;
            dd.layer = (int)GlobalEnums.PhysLayers.CORPSE;
            _fsm.SetState("Stun Set");
            Vasi.Mirror.SetField(dreamNailReaction, "convoAmount", 5);

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
            IsmaController ic = BossLoader.CreateIsma(false);
            
            // Grow flowers if in pantheon
            if (CustomWP.boss == CustomWP.Boss.All)
            {
                Log("made flowers");
                StartFlowers();
                PlayFlowers(0);
            }

            // After Isma falls down
            yield return new WaitWhile(() => !ic.introDone);
			_fsm.enabled = true;
            _fsm.SetState("Stun Recover");
            dd.layer = (int)GlobalEnums.PhysLayers.ENEMIES;
            yield return null;

            // WD scream
            // This is to prevent WD from entering any other state after Stun Recover
            _fsm.InsertMethod("Idle", 1, () => _fsm.SetState("Rage Roar"));
            yield return new WaitWhile(() => _fsm.ActiveStateName == "Stun Recover");
            yield return new WaitWhile(() => _fsm.ActiveStateName == "Rage Roar");
            _fsm.RemoveAction("Idle", 1);
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

        private void CCDreamExit()
		{
            HeroController.instance.RelinquishControl();
            PlayerData.instance.disablePause = true;
            GameObject dreamPts = GameObject.Find("Dream Exit Particle Field");
            if(dreamPts != null)
            {
                dreamPts.GetComponent<ParticleSystem>().Play();
            }

            // Hijacking the vanilla transition where Ogrim looks up at the other knights to do the same thing, without him looking up
            EnemyDeathEffects deathfx = dd.GetComponent<EnemyDeathEffectsUninfected>();
            GameObject corpsePrefab = Vasi.Mirror.GetField<EnemyDeathEffects, GameObject>(deathfx, "corpsePrefab");
            GameObject transition = Instantiate(corpsePrefab);
            transition.GetComponent<Renderer>().enabled = false;

            PlayMakerFSM transitionFSM = transition.LocateMyFSM("Control");
            GameObject text = transitionFSM.GetAction<SetTextMeshProAlignment>("New Scene", 1).gameObject.GameObject.Value;
            TextMeshPro tmp = text.GetComponent<TextMeshPro>();
            tmp.color = Color.black;
            tmp.alignment = TextAlignmentOptions.Center;

            // Change fade times
            transitionFSM.GetAction<Wait>("Fade Out", 4).time.Value += 2f;
            PlayMakerFSM fsm2 = GameObject.Find("Blanker White").LocateMyFSM("Blanker Control");
            fsm2.FsmVariables.FindFsmFloat("Fade Time").Value = 0;

            // Skip states that affect vanilla WD stuff
            transitionFSM.GetState("Fade Out").RemoveAction(0);
            transitionFSM.ChangeTransition("Take Control", "FINISHED", "Outro Msg 1a");
            transitionFSM.ChangeTransition("Outro Msg 1b", "CONVO_FINISH", "New Scene");

            // Set win dialogue
            transitionFSM.GetAction<CallMethodProper>("Outro Msg 1a", 0).parameters[0].stringValue = "CC_OUTRO_1a";
            transitionFSM.GetAction<CallMethodProper>("Outro Msg 1a", 0).parameters[1].stringValue = "Speech";
            transitionFSM.GetAction<CallMethodProper>("Outro Msg 1b", 0).parameters[0].stringValue = "CC_OUTRO_1b";
            transitionFSM.GetAction<CallMethodProper>("Outro Msg 1b", 0).parameters[1].stringValue = "Speech";

            // Set fields for room transition
            transitionFSM.GetAction<BeginSceneTransition>("New Scene", 6).sceneName = "Pale_Court_Credits";
            transitionFSM.GetAction<BeginSceneTransition>("New Scene", 6).entryGateName = "";

            HeroController.instance.MaxHealth();
            HeroController.instance.EnterWithoutInput(true);
            transitionFSM.SetState("Fade Out");
            Destroy(this);
        }

		private void HeroControllerTakeDamage(On.HeroController.orig_TakeDamage orig, HeroController self, GameObject go, GlobalEnums.CollisionSide damageSide, int damageAmount, int hazardType)
		{
            orig(self, go, damageSide, damageAmount > 1 ? 1 : damageAmount, hazardType);
        }

		public void BeforePlayerDied()
        {
            Log("RAN");
        }
        
        private void HealthManagerTakeDamage(On.HealthManager.orig_TakeDamage orig, HealthManager self, HitInstance hitInstance)
        {
            if (self.name.Contains("White Defender"))
            {
                HIT_FLAG = true;
            }
            orig(self, hitInstance);
        }

        private void HealthManagerApplyExtraDamage(On.HealthManager.orig_ApplyExtraDamage orig, HealthManager self, int damageAmount)
        {
            if(self.name.Contains("White Defender"))
            {
                HIT_FLAG = true;
            }
            orig(self, damageAmount);
        }

        private void HealthManagerDie(On.HealthManager.orig_Die orig, HealthManager self, float? attackDirection, AttackTypes attackType, bool ignoreEvasion)
        {
            if(self.gameObject.name.Contains("White Defender"))
            {
                self.hp = 1;
                return;
            }
            orig(self, attackDirection, attackType, ignoreEvasion);
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
            Vasi.Mirror.SetField(channelInfo, "clip", clip);
            MusicCue.MusicChannelInfo[] channelInfos = new MusicCue.MusicChannelInfo[]
            {
                channelInfo, null, null, null, null, null
            };
            Vasi.Mirror.SetField(musicCue, "channelInfos", channelInfos);
            var yoursnapshot = Resources.FindObjectsOfTypeAll<AudioMixer>().First(x => x.name == "Music").FindSnapshot("Main Only");
            yoursnapshot.TransitionTo(0);
            GameManager.instance.AudioManager.ApplyMusicCue(musicCue, 0, 0, false);
        }

        private void Log(object o)
        {
            Modding.Logger.Log("[GGBossManager] " + o);
        }
        
        private void OnDestroy()
        {
            Unload();
        }

        private void Unload()
        {
            ModHooks.BeforePlayerDeadHook -= BeforePlayerDied;
            On.HealthManager.TakeDamage -= HealthManagerTakeDamage;
            On.HealthManager.ApplyExtraDamage -= HealthManagerApplyExtraDamage;
            On.HealthManager.Die -= HealthManagerDie;
            On.HeroController.TakeDamage -= HeroControllerTakeDamage;
        }
    }
}