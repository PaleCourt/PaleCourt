using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using FiveKnights.BossManagement;
using FiveKnights.Dryya;
using FiveKnights.Hegemol;
using FiveKnights.Isma;
using FiveKnights.Zemer;
using HutongGames.PlayMaker;
using HutongGames.PlayMaker.Actions;
using SFCore.Utils;
using Modding;
using TMPro;
using UnityEngine;
using UnityEngine.Audio;

namespace FiveKnights
{
    public class OWBossManager : MonoBehaviour
    {
        private HealthManager _hm;
        private PlayMakerFSM _fsm;
        private GameObject _dd; 
        private tk2dSpriteAnimator _tk;
        public MusicPlayer _ap;
        public MusicPlayer _ap2;
        public static OWBossManager Instance;
        

        private IEnumerator Start()
        {
            Instance = this;
            var oldDung = GameObject.Find("White Defender");
            if (oldDung!= null)
            {
                Destroy(oldDung);
            }
            
            _dd = Instantiate(FiveKnights.preloadedGO["WhiteDef"]);
            FiveKnights.preloadedGO["WD"] = _dd;
            _dd.SetActive(false);
            _hm = _dd.GetComponent<HealthManager>();
            _fsm = _dd.LocateMyFSM("Dung Defender");
            _tk = _dd.GetComponent<tk2dSpriteAnimator>();
            OnDestroy();
            Log("Curr Boss " + CustomWP.boss);
            
            if (CustomWP.boss == CustomWP.Boss.Isma)
            {
                GameCameras.instance.cameraShakeFSM.FsmVariables.FindFsmBool("RumblingMed").Value = false;
                CreateIsma();
                Log("Made arena");
                yield return new WaitWhile(() => HeroController.instance == null);
                yield return new WaitWhile(()=> HeroController.instance.transform.position.x < 110f);
                IsmaController ic = FiveKnights.preloadedGO["Isma2"].GetComponent<IsmaController>();
                ic.onlyIsma = true;
                ic.gameObject.SetActive(true);
                PlayMusic(FiveKnights.Clips["LoneIsmaMusic"]);
                yield return new WaitWhile(() => ic != null);
                PlayMusic(null);

                yield return new WaitForSeconds(1.0f);
                WinRoutine("ISMA_OUTRO_1a","ISMA_OUTRO_1b", OWArenaFinder.PrevIsmScene);
                
                Log("Done with Isma boss");
                Destroy(this);
            }
            else if (CustomWP.boss == CustomWP.Boss.Dryya)
            {
                DryyaSetup dc = CreateDryya();
                yield return new WaitWhile(() => HeroController.instance == null);
                yield return new WaitWhile(()=> HeroController.instance.transform.position.x < 422.5f);
                dc.gameObject.SetActive(true);
                var bc = dc.GetComponent<BoxCollider2D>();
                bc.enabled = false;
                yield return new WaitWhile(() => dc.transform.position.y > 103f);
                bc.enabled = true;
                
                var rb = dc.GetComponent<Rigidbody2D>();
                yield return new WaitWhile(()=> rb.velocity.y != 0f);
                PlayMusic(FiveKnights.Clips["DryyaMusic"]);
                yield return new WaitWhile(() => dc != null);
                PlayMusic(null);
                HeroController.instance.RelinquishControl();
                PlayerData.instance.disablePause = true;
                
                yield return new WaitForSeconds(1.0f);
                /*GameObject.Find("Dream Exit Particle Field").GetComponent<ParticleSystem>().Play();
                GameObject transDevice = Instantiate(_dd.transform.Find("Corpse White Defender(Clone)").gameObject);
                transDevice.SetActive(true);
                var fsm = transDevice.LocateMyFSM("Control");
                GameManager.instance.TimePasses();
                GameManager.instance.ResetSemiPersistentItems();
                HeroController.instance.EnterWithoutInput(true);
                fsm.GetAction<BeginSceneTransition>("New Scene", 6).preventCameraFadeOut = true;
                fsm.GetAction<BeginSceneTransition>("New Scene", 6).sceneName = OWArenaFinder.PrevIsmScene;
                fsm.GetAction<BeginSceneTransition>("New Scene", 6).entryGateName = "door_dreamReturn";
                fsm.GetAction<BeginSceneTransition>("New Scene", 6).visualization.Value = GameManager.SceneLoadVisualizations.Dream;
                fsm.GetAction<BeginSceneTransition>("New Scene", 6).entryDelay = 0;
                fsm.GetAction<Wait>("Fade Out", 4).time.Value += 2f;
                PlayMakerFSM fsm2 = GameObject.Find("Blanker White").LocateMyFSM("Blanker Control");
                fsm2.FsmVariables.FindFsmFloat("Fade Time").Value = 0;
                fsm.SetState("Fade Out");*/
                WinRoutine("DRYYA_OUTRO_1a","DRYYA_OUTRO_1b", OWArenaFinder.PrevDryScene);
                Log("Done with Isma boss");
                Destroy(this);
            }
            else if (CustomWP.boss == CustomWP.Boss.Hegemol)
            {
                HegemolController hegemolCtrl = CreateHegemol();
                GameCameras.instance.cameraShakeFSM.FsmVariables.FindFsmBool("RumblingMed").Value = false;
                yield return new WaitWhile(() => HeroController.instance == null);
                yield return new WaitWhile(()=> HeroController.instance.transform.position.x < 432f);
                PlayMusic(FiveKnights.Clips["HegemolMusic"]);
                hegemolCtrl.gameObject.SetActive(false);
                yield return new WaitWhile(() => hegemolCtrl != null);
                var bsc = BossSceneController.Instance;
                GameObject transition = Instantiate(bsc.transitionPrefab);
                PlayMakerFSM transitionsFSM = transition.LocateMyFSM("Transitions");
                transitionsFSM.SetState("Out Statue");
                yield return new WaitForSeconds(1.0f);
                bsc.gameObject.LocateMyFSM("Dream Return").SendEvent("DREAM RETURN");
                Destroy(this);
            }
            else if (CustomWP.boss == CustomWP.Boss.Ze)
            {
                ZemerController zc = CreateZemer();
                GameObject child = Instantiate(FiveKnights.preloadedGO["TChild"]);
                var tChild = child.AddComponent<TChildCtrl>();
                child.SetActive(true);

                yield return new WaitWhile(() => !tChild.helpZemer);
                GameObject zem = zc.gameObject;
                zem.SetActive(true);

                yield return new WaitWhile(() => zc != null);
                ZemerControllerP2 zc2 = zem.GetComponent<ZemerControllerP2>();
                yield return new WaitWhile(() => zc2 != null);
                Log("? idk dude");
                WinRoutine("ZEM_OUTRO_1a","ZEM_OUTRO_1b", OWArenaFinder.PrevZemScene);
                Destroy(this);
            }
        }

        private void WinRoutine(string msg1Key, string msg2Key, string area, bool dungAnimation = true)
        {
            HeroController.instance.RelinquishControl();
            GameObject dreambye = GameObject.Find("Dream Exit Particle Field");
            if (dreambye != null)
            {
                dreambye.GetComponent<ParticleSystem>().Play();
            }
            GameObject transDevice = Instantiate(_dd.transform.Find("Corpse White Defender(Clone)").gameObject);
            transDevice.SetActive(true);
            var fsm = transDevice.LocateMyFSM("Control");
            GameObject text = fsm.GetAction<SetTextMeshProAlignment>("New Scene", 1).gameObject.GameObject.Value;
            TextMeshPro tmp = text.GetComponent<TextMeshPro>();
            fsm.GetAction<Wait>("Fade Out", 4).time.Value += 2f;
            PlayMakerFSM fsm2 = GameObject.Find("Blanker White").LocateMyFSM("Blanker Control");
            fsm2.FsmVariables.FindFsmFloat("Fade Time").Value = 0;
            fsm.RemoveAction("Fade Out", 0);
            fsm.ChangeTransition("Take Control", "FINISHED", "Outro Msg 1a");
            if (dungAnimation)
            {
                IEnumerator PlayDungAnimation()
                {
                    GameObject animObj = new GameObject("DungAnim", typeof(MeshRenderer), typeof(MeshFilter), typeof(tk2dSprite));
                    animObj.transform.position = tmp.transform.position - new Vector3(0f, 2f, 0f);
                    var dungAnim = animObj.AddComponent<tk2dSpriteAnimator>();
                    dungAnim.Library = FiveKnights.preloadedGO["Crest Anim Prefab"].GetComponent<tk2dSpriteAnimation>();
                    foreach (var clip in dungAnim.Library.clips)
                        Log("Clip name " + clip.name);
                    yield return dungAnim.PlayAnimWait(dungAnim.Library.clips[0].name);
                }
                FsmState state = fsm.CreateState("Dung Transformation");
                state.Actions = new FsmStateAction[] { new InvokeCoroutine(PlayDungAnimation, true) };
                fsm.ChangeTransition("Outro Msg 1b", "CONVO_FINISH", "Dung Transformation");
                fsm.AddTransition("Dung Transformation", FsmEvent.Finished, "New Scene");
            }
            else
                fsm.ChangeTransition("Outro Msg 1b", "CONVO_FINISH", "New Scene");
            tmp.color = Color.black;
            tmp.alignment = TextAlignmentOptions.Center;
            fsm.GetAction<CallMethodProper>("Outro Msg 1a", 0).parameters[0].stringValue = msg1Key;
            fsm.GetAction<CallMethodProper>("Outro Msg 1b", 0).parameters[0].stringValue = msg2Key;
            fsm.GetAction<BeginSceneTransition>("New Scene", 6).preventCameraFadeOut = true;
            fsm.GetAction<BeginSceneTransition>("New Scene", 6).sceneName = area;
            fsm.GetAction<BeginSceneTransition>("New Scene", 6).entryGateName = "door_dreamReturn";
            fsm.GetAction<BeginSceneTransition>("New Scene", 6).visualization.Value = GameManager.SceneLoadVisualizations.Default;
            fsm.GetAction<BeginSceneTransition>("New Scene", 6).entryDelay = 0;
            HeroController.instance.EnterWithoutInput(true);
            fsm.SetState("Fade Out");
            //fsm.PrintPlayMakerFSM();
        }
        
        public void PlayMusic(AudioClip clip)
        {
            MusicCue musicCue = ScriptableObject.CreateInstance<MusicCue>();
            MusicCue.MusicChannelInfo channelInfo = new MusicCue.MusicChannelInfo();
            channelInfo.SetAttr("clip", clip);
            MusicCue.MusicChannelInfo[] channelInfos = new MusicCue.MusicChannelInfo[]
            {
                channelInfo, null, null, null, null, null
            };
            musicCue.SetAttr("channelInfos", channelInfos);
            var yoursnapshot = Resources.FindObjectsOfTypeAll<AudioMixer>().First(x => x.name == "Music").FindSnapshot("Main Only");
            yoursnapshot.TransitionTo(0);
            GameManager.instance.AudioManager.ApplyMusicCue(musicCue, 0, 0, false);
        }

        private void CreateIsma()
        {
            Log("Creating Isma");
            
            AssetBundle snd = ABManager.AssetBundles[ABManager.Bundle.Sound];
            // List of Isma's voice lines
            string[] arr = new[]
            {
                "IsmaAudAtt1", "IsmaAudAtt2", "IsmaAudAtt3","IsmaAudAtt4","IsmaAudAtt5",
                "IsmaAudAtt6","IsmaAudAtt7","IsmaAudAtt8","IsmaAudAtt9","IsmaAudDeath"
            };
            // Get isma's music from bundle
            FiveKnights.Clips["LoneIsmaMusic"] = snd.LoadAsset<AudioClip>("LoneIsmaMusic");
            // Loads Isma's voice lines a frame at a time, not sure why though 
            IEnumerator LoadSlow()
            {
                foreach (var i in arr)
                {
                    FiveKnights.IsmaClips[i] = snd.LoadAsset<AudioClip>(i);
                    yield return null;
                }
            }
            // This is for the flash effect when getting hit, not sure if it's used anymore
            AssetBundle misc = ABManager.AssetBundles[ABManager.Bundle.Misc];
            FiveKnights.Materials["flash"] = misc.LoadAsset<Material>("UnlitFlashMat");
            // Load voice lines 
            StartCoroutine(LoadSlow());
            
            // Load the one and only Isma
            GameObject isma = Instantiate(FiveKnights.preloadedGO["Isma"]);
            // Awful way to keep access to Isma
            FiveKnights.preloadedGO["Isma2"] = isma;
            isma.SetActive(false);
            // Setting material of spriterenderers and adding their damage
            foreach (SpriteRenderer i in isma.GetComponentsInChildren<SpriteRenderer>(true))
            {
                i.material = new Material(Shader.Find("Sprites/Default"));
                
                if (i.name == "FrontW")
                    continue;

                if (!i.gameObject.GetComponent<PolygonCollider2D>() && !i.gameObject.GetComponent<BoxCollider2D>()) 
                    continue;
                
                i.gameObject.AddComponent<DamageHero>().damageDealt = 1;
                i.gameObject.layer = 11;
            }

            foreach (PolygonCollider2D i in isma.transform.Find("Whip")
                .GetComponentsInChildren<PolygonCollider2D>(true))
            {
                i.gameObject.layer = 11;
                i.gameObject.AddComponent<DamageHero>().damageDealt = 1;
            }

            // Have to move arena up a little
            GameObject.Find("acid stuff").transform.position += new Vector3(0f, 0.18f, 0f);
            var _sr = isma.GetComponent<SpriteRenderer>();
            _sr.material = FiveKnights.Materials["flash"];

            isma.AddComponent<IsmaController>();
            isma.SetActive(false);
            
            Log("Done creating Isma");
        }
        
        private DryyaSetup CreateDryya()
        {
            Log("Creating Dryya");
            
            AssetBundle snd = ABManager.AssetBundles[ABManager.Bundle.Sound];
            FiveKnights.Clips["DryyaMusic"] = snd.LoadAsset<AudioClip>("DryyaMusic");
            
            /*AssetBundle misc = ABManager.AssetBundles[ABManager.Bundle.Misc];
            ArenaFinder.Materials["flash"] = misc.LoadAsset<Material>("UnlitFlashMat");
            foreach (var i in misc.LoadAllAssets<Sprite>().Where(x => x.name.Contains("Dryya_Silhouette_")))
            {
                ArenaFinder.Sprites[i.name] = i;
            }*/

            Vector2 pos = new Vector2(457.6f, 112.5f);
            GameObject dryya = Instantiate(FiveKnights.preloadedGO["Dryya"], pos, Quaternion.identity);
            dryya.SetActive(false);
            Log("Done creating dryya");
            return dryya.AddComponent<DryyaSetup>();
        }

        private HegemolController CreateHegemol()
        {
            Log("Creating Hegemol");
            
            AssetBundle snd = ABManager.AssetBundles[ABManager.Bundle.Sound];
            FiveKnights.Clips["HegemolMusic"] = snd.LoadAsset<AudioClip>("HegemolMusic");
            
            AssetBundle misc = ABManager.AssetBundles[ABManager.Bundle.Misc];
            foreach (var i in misc.LoadAllAssets<Sprite>().Where(x => x.name.Contains("hegemol_silhouette_")))
            {
                ArenaFinder.Sprites[i.name] = i;
            }
            
            GameObject hegemol = Instantiate(FiveKnights.preloadedGO["fk"], new Vector2(87, 23), Quaternion.identity);
            hegemol.SetActive(false);
            Log("Adding HegemolController component");
            return hegemol.AddComponent<HegemolController>();
        }
        
        private ZemerController CreateZemer()
        {
            Log("Creating Zemer");
            
            AssetBundle snd = ABManager.AssetBundles[ABManager.Bundle.Sound];
            string[] arr = new[]
            {
                "ZAudP2Death2", "ZP2Intro","ZP1Loop", "ZAudP1Death", "ZAudAtt4", "ZAudP2Death1",
                "ZAudBow", "ZAudCounter", "ZAudAtt5", "ZP1Intro", "ZAudAtt2", "ZP2Loop",
                "ZAudLaser", "ZAudHoriz", "ZAudAtt3", "ZAudAtt1", "ZAudAtt6","AudBasicSlash1", 
                "AudBigSlash", "AudBigSlash2", "AudLand", "AudDashIntro", "AudDash", "AudBasicSlash2"
            };
            
            foreach (var i in arr)
            {
                FiveKnights.Clips[i] = snd.LoadAsset<AudioClip>(i);
            }

            AssetBundle misc = ABManager.AssetBundles[ABManager.Bundle.Misc];
            ArenaFinder.Sprites["ZemParticPetal"] = misc.LoadAsset<Sprite>("petal-test");
            ArenaFinder.Sprites["ZemParticDung"] = misc.LoadAsset<Sprite>("dung-test");
            FiveKnights.Materials["flash"] = misc.LoadAsset<Material>("UnlitFlashMat");

            GameObject zemer = Instantiate(FiveKnights.preloadedGO["Zemer"]);
            zemer.SetActive(true);
            foreach (Transform i in FiveKnights.preloadedGO["SlashBeam"].transform)
            {
                i.gameObject.AddComponent<DamageHero>().damageDealt = 1;
                i.gameObject.layer = 22;
            }

            foreach (Transform i in FiveKnights.preloadedGO["TChild"].transform)
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
            foreach (SpriteRenderer i in zemer.GetComponentsInChildren<SpriteRenderer>(true))
            {
                i.material = new Material(Shader.Find("Sprites/Default"));
                
                var bc = i.gameObject.GetComponent<BoxCollider2D>();
                
                if (bc == null) 
                    continue;
                
                bc.isTrigger = true;
                bc.gameObject.AddComponent<DamageHero>().damageDealt = 1;
                bc.gameObject.layer = 22;
            }
            foreach (PolygonCollider2D i in zemer.GetComponentsInChildren<PolygonCollider2D>(true))
            { 
                i.isTrigger = true;
                i.gameObject.AddComponent<DamageHero>().damageDealt = 1;
                i.gameObject.AddComponent<Tink>();
                i.gameObject.AddComponent<Pogoable>();
                i.gameObject.layer = 22;
                
            }
            zemer.GetComponent<SpriteRenderer>();
            var zc = zemer.AddComponent<ZemerController>();
            Log("Done creating Zemer");
            zemer.SetActive(false);
            return zc;
        }

        private void OnDestroy()
        {
            _ap?.StopMusic();
            _ap2?.StopMusic();
        }

        private void Log(object o)
        {
            if (!FiveKnights.isDebug) return;
            Modding.Logger.Log("[OWBossManager] " + o);
        }
    }
}