using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using GlobalEnums;
using HutongGames.PlayMaker;
using HutongGames.PlayMaker.Actions;
using SFCore.Utils;
using Modding;
using UnityEngine;
using Random = UnityEngine.Random;

namespace FiveKnights
{
    internal partial class Amulets : MonoBehaviour
    {
        private HeroController _hc => HeroController.instance;
        private PlayerData _pd => PlayerData.instance;


        private PlayMakerFSM _spellControl;

        private PlayMakerFSM _blastControl;
        private PlayMakerFSM _pvControl;
        private GameObject _audioPlayerActor;

        private bool _activated = false;

        public void Awake()
        {
            On.HeroController.Start += On_HeroController_Start;
            On.CharmIconList.GetSprite += CharmIconList_GetSprite;
            ModHooks.CharmUpdateHook += CharmUpdate;
        }

        private Sprite CharmIconList_GetSprite(On.CharmIconList.orig_GetSprite orig, CharmIconList self, int id)
        {
            if (FiveKnights.Instance.SaveSettings.upgradedCharm_10)
            {
                //Log("Upgraded Defender's Crest");
                self.spriteList[10] = FiveKnights.SPRITES["Kings_Honour"];
            }
            else
            {
                self.spriteList[10] = FiveKnights.SPRITES["Defenders_Crest"];
            }
            return orig(self, id);
        }

        public void On_HeroController_Start(On.HeroController.orig_Start orig, HeroController self)
        {
            orig(self);

            Log("Amulets Start");

            Log("Dash Cooldown: " + _hc.DASH_COOLDOWN);
            Log("Dash Cooldown Charm: " + _hc.DASH_COOLDOWN_CH);
            Log("Dash Speed: " + _hc.DASH_SPEED);
            Log("Dash Speed Sharp: " + _hc.DASH_SPEED_SHARP);

            //RepositionCharmsInInventory();

            _pvControl = Instantiate(FiveKnights.preloadedGO["PV"].LocateMyFSM("Control"), _hc.transform);
            GameObject blast = Instantiate(FiveKnights.preloadedGO["Blast"]);
            blast.SetActive(false);
            _blastControl = blast.LocateMyFSM("Control");

            //_pd.CalculateNotchesUsed();

            Log("Waiting for Audio Player Actor...");
            _spellControl = _hc.spellControl;
            GameObject fireballParent = _spellControl.GetAction<SpawnObjectFromGlobalPool>("Fireball 2", 3).gameObject.Value;
            PlayMakerFSM fireballCast = fireballParent.LocateMyFSM("Fireball Cast");


            // King's Honour
            _hc.gameObject.AddComponent<RoyalAura>().enabled = false;

            // Mark of Purity
            _hc.gameObject.AddComponent<PurityTimer>().enabled = false;
            _hc.gameObject.AddComponent<AutoSwing>().enabled = false;

            // Boon of Hallownest
            _hc.gameObject.AddComponent<BoonSpells>().enabled = false;
            InsertCharmSpellEffectsInFsm();
            
            // Vessels Lament
            _hc.gameObject.AddComponent<LamentControl>().enabled = false;

            // Abyssal Bloom - the order of these is specific because each one tries to get a reference to the previous
            _hc.gameObject.AddComponent<ModifyBloomProps>().enabled = true;
            _hc.gameObject.AddComponent<AbyssalBloom>().enabled = false;
            _hc.gameObject.AddComponent<CheckBloomStage>().enabled = true;
            AddVoidAttacks(_hc);
            ModifyFuryForBloom();
            ModifySpellsForBloom();

            // FiveKnights.Instance.SaveSettings.upgradedCharm_10 = true;

            /*FiveKnights.Instance.SaveSettings.gotCharms[0] = true;
            FiveKnights.Instance.SaveSettings.gotCharms[1] = true;
            FiveKnights.Instance.SaveSettings.gotCharms[2] = true;
            FiveKnights.Instance.SaveSettings.gotCharms[3] = true;*/

            /*PureAmulets.Settings.newCharm_41 = true;
            PureAmulets.Settings.newCharm_42 = true;
            PureAmulets.Settings.newCharm_43 = true;
            PureAmulets.Settings.newCharm_44 = true;*/

            Log("Got Charm 41: " + FiveKnights.Instance.SaveSettings.gotCharms[0]);
            Log("Got Charm 42: " + FiveKnights.Instance.SaveSettings.gotCharms[1]);
            Log("Got Charm 43: " + FiveKnights.Instance.SaveSettings.gotCharms[2]);
            Log("Got Charm 44: " + FiveKnights.Instance.SaveSettings.gotCharms[3]);
            Log("New Charm 41: " + FiveKnights.Instance.SaveSettings.newCharms[0]);
            Log("New Charm 42: " + FiveKnights.Instance.SaveSettings.newCharms[1]);
            Log("New Charm 43: " + FiveKnights.Instance.SaveSettings.newCharms[2]);
            Log("New Charm 44: " + FiveKnights.Instance.SaveSettings.newCharms[3]);
            Log("Equipped Charm 41: " + FiveKnights.Instance.SaveSettings.equippedCharms[0]);
            Log("Equipped Charm 42: " + FiveKnights.Instance.SaveSettings.equippedCharms[1]);
            Log("Equipped Charm 43: " + FiveKnights.Instance.SaveSettings.equippedCharms[2]);
            Log("Equipped Charm 44: " + FiveKnights.Instance.SaveSettings.equippedCharms[3]);
            Log("Upgraded Charm 10: " + FiveKnights.Instance.SaveSettings.upgradedCharm_10);

            Log("Amulets Start Finished");
            _activated = true;
        }

        private void InsertCharmSpellEffectsInFsm()
        {
            _spellControl.CopyState("Fireball 1", "Fireball 1 SmallShots");
            _spellControl.CopyState("Fireball 2", "Fireball 2 SmallShots");

            _spellControl.RemoveAction<SpawnObjectFromGlobalPool>("Fireball 1 SmallShots");
            _spellControl.RemoveAction<SpawnObjectFromGlobalPool>("Fireball 2 SmallShots");
            _spellControl.InsertMethod("Fireball 1 SmallShots", 3, () => HeroController.instance.GetComponent<BoonSpells>().CastDaggers(false));
            _spellControl.InsertMethod("Fireball 2 SmallShots", 3, () => HeroController.instance.GetComponent<BoonSpells>().CastDaggers(true));

            _spellControl.CopyState("Quake1 Land", "Q1 Land Plumes");
            _spellControl.CopyState("Q2 Land", "Q2 Land Plumes");
            _spellControl.ChangeTransition("Q2 Land Plumes", "FINISHED", "Quake Finish");
            _spellControl.InsertMethod("Q1 Land Plumes", () => HeroController.instance.GetComponent<BoonSpells>().CastPlumes(false), 0);
            _spellControl.InsertMethod("Q2 Land Plumes", () => HeroController.instance.GetComponent<BoonSpells>().CastPlumes(true), 0);

            _spellControl.CopyState("Scream Antic1", "Scream Antic1 Blasts");
            _spellControl.CopyState("Scream Burst 1", "Scream Burst 1 Blasts");
            _spellControl.CopyState("Scream Antic2", "Scream Antic2 Blasts");
            _spellControl.CopyState("Scream Burst 2", "Scream Burst 2 Blasts");
            _spellControl.ChangeTransition("Scream Antic1 Blasts", "FINISHED", "Scream Burst 1 Blasts");
            _spellControl.ChangeTransition("Scream Antic2 Blasts", "FINISHED", "Scream Burst 2 Blasts");

            _spellControl.RemoveAction<AudioPlay>("Scream Antic1 Blasts");
            _spellControl.RemoveAction<CreateObject>("Scream Burst 1 Blasts");
            _spellControl.RemoveAction<ActivateGameObject>("Scream Burst 1 Blasts");
            _spellControl.RemoveAction<ActivateGameObject>("Scream Burst 1 Blasts");
            _spellControl.RemoveAction<SendEventByName>("Scream Burst 1 Blasts");
            _spellControl.RemoveAction<SendEventByName>("Scream Burst 1 Blasts");
            _spellControl.InsertMethod("Scream Burst 1 Blasts", 0, () => HeroController.instance.GetComponent<BoonSpells>().CastBlasts(false));

            _spellControl.RemoveAction<AudioPlay>("Scream Antic2 Blasts");
            _spellControl.RemoveAction<CreateObject>("Scream Burst 2 Blasts");
            _spellControl.RemoveAction<ActivateGameObject>("Scream Burst 2 Blasts");
            _spellControl.RemoveAction<ActivateGameObject>("Scream Burst 2 Blasts");
            _spellControl.RemoveAction<SendEventByName>("Scream Burst 2 Blasts");
            _spellControl.RemoveAction<SendEventByName>("Scream Burst 2 Blasts");
            _spellControl.InsertMethod("Scream Burst 2 Blasts", 0, () => HeroController.instance.GetComponent<BoonSpells>().CastBlasts(true));


            _spellControl.CopyState("Focus", "Focus Blast");
            _spellControl.CopyState("Focus Heal", "Focus Heal Blast");
            _spellControl.CopyState("Start MP Drain", "Start MP Drain Blast");
            _spellControl.CopyState("Focus Heal 2", "Focus Heal 2 Blast");
            _spellControl.InsertCoroutine("Focus Blast", 0, PureVesselBlastFadeIn);
            _spellControl.InsertCoroutine("Focus Heal Blast", 0, PureVesselBlast);
            _spellControl.InsertCoroutine("Start MP Drain Blast", 0, PureVesselBlastFadeIn);
            _spellControl.InsertCoroutine("Focus Heal 2 Blast", 0, PureVesselBlast);

            _spellControl.InsertMethod("Cancel All", 0, CancelBlast);
            _spellControl.InsertMethod("Focus Cancel", 0, CancelBlast);
            _spellControl.InsertMethod("Focus Cancel 2", 0, CancelBlast);
        }

        private void AddVoidAttacks(HeroController self)
        {
            GameObject attacks = self.gameObject.FindGameObjectInChildren("Attacks");

            Shader shader = self.GetComponent<tk2dSprite>().Collection.spriteDefinitions[0].material.shader;

            GameObject collectionPrefab = FiveKnights.preloadedGO["Bloom Sprite Prefab"];
            tk2dSpriteCollection collection = collectionPrefab.GetComponent<tk2dSpriteCollection>();
            GameObject animationPrefab = FiveKnights.preloadedGO["Bloom Anim Prefab"];
            tk2dSpriteAnimation animation = animationPrefab.GetComponent<tk2dSpriteAnimation>();

            // Knight sprites and animations
            var heroSprite = self.GetComponent<tk2dSprite>();
            var knightAnim = self.GetComponent<tk2dSpriteAnimator>();
            tk2dSpriteCollectionData collectionData = heroSprite.Collection;
            List<tk2dSpriteDefinition> knightSpriteDefs = collectionData.spriteDefinitions.ToList();
            foreach(tk2dSpriteDefinition def in collection.spriteCollection.spriteDefinitions)
            {
                def.material.shader = shader;
                knightSpriteDefs.Add(def);
            }
            heroSprite.Collection.spriteDefinitions = knightSpriteDefs.ToArray();
            List<tk2dSpriteAnimationClip> knightClips = knightAnim.Library.clips.ToList();
            foreach(tk2dSpriteAnimationClip clip in animation.clips)
            {
                knightClips.Add(clip);
            }
            knightAnim.Library.clips = knightClips.ToArray();

            GameObject cycloneSlashVoid = Instantiate(attacks.FindGameObjectInChildren("Cyclone Slash"), attacks.transform);
            cycloneSlashVoid.name = "Cyclone Slash Void";
            cycloneSlashVoid.GetComponent<tk2dSpriteAnimator>().DefaultClipId = knightAnim.GetClipIdByName("Cyclone Slash Effect Void");

            GameObject dashSlashVoid = Instantiate(attacks.FindGameObjectInChildren("Dash Slash"), attacks.transform);
            dashSlashVoid.name = "Dash Slash Void";
            dashSlashVoid.GetComponent<tk2dSpriteAnimator>().DefaultClipId = knightAnim.GetClipIdByName("Dash Slash Effect Void");

            GameObject greatSlashVoid = Instantiate(attacks.FindGameObjectInChildren("Great Slash"), attacks.transform);
            greatSlashVoid.name = "Great Slash Void";
            greatSlashVoid.GetComponent<tk2dSpriteAnimator>().DefaultClipId = knightAnim.GetClipIdByName("Great Slash Effect Void");

            // Nail Arts FSM
            PlayMakerFSM nailArts = self.gameObject.LocateMyFSM("Nail Arts");
            if(nailArts.FsmStates[0].Fsm == null)
            {
                nailArts.Preprocess();
            }

            // Create states to test for activated Abyssal Bloom
            nailArts.AddState("Bloom Activated CSlash?");
            nailArts.AddState("Bloom Activated DSlash?");
            nailArts.AddState("Bloom Activated GSlash?");

            // Clone Cyclone Slash states
            nailArts.CopyState("Cyclone Start", "Cyclone Start Void");
            nailArts.CopyState("Hover Start", "Hover Start Void");
            nailArts.CopyState("Activate Slash", "Activate Slash Void");
            nailArts.CopyState("Play Audio", "Play Audio Void");
            nailArts.CopyState("Cyclone Spin", "Cyclone Spin Void");
            nailArts.CopyState("Cyclone Extend", "Cyclone Extend Void");
            nailArts.CopyState("Cyclone End", "Cyclone End Void");

            // Clone Dash Slash states
            nailArts.CopyState("Dash Slash", "Dash Slash Void");
            nailArts.CopyState("DSlash Move End", "DSlash Move End Void");
            nailArts.CopyState("D Slash End", "D Slash End Void");

            // Clone Great Slash states
            nailArts.CopyState("G Slash", "G Slash Void");
            nailArts.CopyState("Stop Move", "Stop Move Void");
            nailArts.CopyState("G Slash End", "G Slash End Void");

            // Change transitions for Cyclone Slash Void
            nailArts.ChangeTransition("Flash", "FINISHED", "Bloom Activated CSlash?");
            nailArts.ChangeTransition("Cyclone Start Void", "FINISHED", "Activate Slash Void");
            nailArts.ChangeTransition("Cyclone Start Void", "BUTTON DOWN", "Hover Start Void");
            nailArts.ChangeTransition("Hover Start Void", "FINISHED", "Cyclone Start Void");
            nailArts.ChangeTransition("Activate Slash Void", "FINISHED", "Play Audio Void");
            nailArts.ChangeTransition("Play Audio Void", "FINISHED", "Cyclone Spin Void");
            nailArts.ChangeTransition("Cyclone Spin Void", "BUTTON DOWN", "Cyclone Extend Void");
            nailArts.ChangeTransition("Cyclone Spin Void", "END", "Cyclone End Void");
            nailArts.ChangeTransition("Cyclone Extend Void", "END", "Cyclone End Void");
            nailArts.ChangeTransition("Cyclone Extend Void", "WAIT", "Cyclone Spin Void");

            // Change transitions for Dash Slash Void
            nailArts.ChangeTransition("Left 2", "FINISHED", "Bloom Activated DSlash?");
            nailArts.ChangeTransition("Right 2", "FINISHED", "Bloom Activated DSlash?");
            nailArts.ChangeTransition("Dash Slash Void", "FINISHED", "DSlash Move End Void");
            nailArts.ChangeTransition("DSlash Move End Void", "FINISHED", "D Slash End Void");

            // Change transitions for Great Slash Void
            nailArts.ChangeTransition("Left", "FINISHED", "Bloom Activated GSlash?");
            nailArts.ChangeTransition("Right", "FINISHED", "Bloom Activated GSlash?");
            nailArts.ChangeTransition("G Slash Void", "FINISHED", "Stop Move Void");
            nailArts.ChangeTransition("Stop Move Void", "FINISHED", "G Slash End Void");

            // Make transitions for void narts
            nailArts.AddTransition("Bloom Activated CSlash?", "VOID", "Cyclone Start Void");
            nailArts.AddTransition("Bloom Activated CSlash?", "NORMAL", "Cyclone Start");
            nailArts.AddTransition("Bloom Activated DSlash?", "VOID", "Dash Slash Void");
            nailArts.AddTransition("Bloom Activated DSlash?", "NORMAL", "Dash Slash");
            nailArts.AddTransition("Bloom Activated GSlash?", "VOID", "G Slash Void");
            nailArts.AddTransition("Bloom Activated GSlash?", "NORMAL", "G Slash");
            nailArts.AddMethod("Bloom Activated CSlash?", () =>
            {
                nailArts.SetState(FiveKnights.Instance.SaveSettings.equippedCharms[3] && _pd.health <= 1 ? "Cyclone Start Void" : "Cyclone Start");
            });
            nailArts.AddMethod("Bloom Activated DSlash?", () =>
            {
                nailArts.SetState(FiveKnights.Instance.SaveSettings.equippedCharms[3] && _pd.health <= 1 ? "Dash Slash Void" : "Dash Slash");
            });
            nailArts.AddMethod("Bloom Activated GSlash?", () =>
            {
                nailArts.SetState(FiveKnights.Instance.SaveSettings.equippedCharms[3] && _pd.health <= 1 ? "G Slash Void" : "G Slash");
            });

            // Change Knight animation clips
            nailArts.GetAction<Tk2dPlayAnimationWithEvents>("Cyclone Start Void").clipName = "NA Cyclone Start Void";
            nailArts.GetAction<Tk2dPlayAnimation>("Cyclone Spin Void").clipName = "NA Cyclone Void";
            nailArts.GetAction<Tk2dPlayAnimation>("Cyclone Extend Void").clipName = "NA Cyclone Void";
            nailArts.GetAction<Tk2dPlayAnimationWithEvents>("Cyclone End Void").clipName = "NA Cyclone End Void";
            nailArts.GetAction<Tk2dPlayAnimationWithEvents>("Dash Slash Void").clipName = "NA Dash Slash Void";
            nailArts.GetAction<Tk2dPlayAnimationWithEvents>("G Slash Void").clipName = "NA Big Slash Void";

			//// Insert testing methods for testing states
			//nailArts.InsertMethod("Bloom Activated CSlash?", 0, () =>
			//{
			//	nailArts.SetState(FiveKnights.Instance.SaveSettings.equippedCharms[3] && _pd.health <= 10 ? "Cyclone Start Void" : "Cyclone Start");
			//});
			//nailArts.InsertMethod("Bloom Activated DSlash?", 0, () =>
			//{
			//	nailArts.SetState(FiveKnights.Instance.SaveSettings.equippedCharms[3] && _pd.health <= 10 ? "Dash Slash Void" : "Dash Slash");
			//});
			//nailArts.InsertMethod("Bloom Activated GSlash?", 0, () =>
			//{
			//	Log($"PureAmulets.Settings.equippedCharm_44: {FiveKnights.Instance.SaveSettings.equippedCharms[3]}, health: {_pd.health <= 10}");
			//	nailArts.SetState(FiveKnights.Instance.SaveSettings.equippedCharms[3] && _pd.health <= 10 ? "G Slash Void" : "G Slash");
			//});

			// Insert activation and deactivation of void nail arts
			nailArts.InsertMethod("Activate Slash Void", 0, () =>
            {
                cycloneSlashVoid.SetActive(true);
                cycloneSlashVoid.GetComponent<tk2dSpriteAnimator>().Play("Cyclone Slash Effect Void");
            });
            nailArts.InsertMethod("Cyclone End Void", 2, () => cycloneSlashVoid.SetActive(false));
            nailArts.AddMethod("Cancel All", () => cycloneSlashVoid.SetActive(false));
            nailArts.InsertMethod("Dash Slash Void", 0, () =>
            {
                dashSlashVoid.SetActive(true);
                dashSlashVoid.GetComponent<tk2dSpriteAnimator>().Play("Dash Slash Effect Void");
            });
            nailArts.InsertMethod("D Slash End Void", 0, () => dashSlashVoid.SetActive(false));
            nailArts.AddMethod("Cancel All", () => dashSlashVoid.SetActive(false));
            nailArts.InsertMethod("G Slash Void", 0, () =>
            {
                greatSlashVoid.SetActive(true);
                greatSlashVoid.GetComponent<tk2dSpriteAnimator>().Play("Great Slash Effect Void");
            });
            nailArts.InsertMethod("G Slash End Void", 0, () => greatSlashVoid.SetActive(false));
            nailArts.AddMethod("Cancel All", () => greatSlashVoid.SetActive(false));

            // Remove activating old nail art effects
            nailArts.RemoveAction<ActivateGameObject>("Activate Slash Void");
            nailArts.RemoveAction<ActivateGameObject>("Dash Slash Void");
            nailArts.RemoveAction<ActivateGameObject>("G Slash Void");

            nailArts.Log();

            StartCoroutine(ResetVoidNarts(new GameObject[] { cycloneSlashVoid, dashSlashVoid, greatSlashVoid }));
        }

        private IEnumerator ResetVoidNarts(GameObject[] narts)
		{
            // This is necessary because otherwise the very first void nail art will always be a normal one
            foreach(GameObject nart in narts)
			{
                nart.SetActive(true);
			}
            yield return new WaitForEndOfFrame();
            foreach(GameObject nart in narts)
            {
                nart.SetActive(false);
            }
        }

        private void ModifyFuryForBloom()
        {
            PlayMakerFSM fury = _hc.gameObject.FindGameObjectInChildren("Charm Effects").LocateMyFSM("Fury");
            Log("Fury Color: " + fury.GetAction<Tk2dSpriteSetColor>("Activate", 17).color.Value);
            Color furyColor = fury.GetAction<Tk2dSpriteSetColor>("Activate", 18).color.Value;
            fury.InsertMethod("Activate", 17, () =>
            {
                Color color = FiveKnights.Instance.SaveSettings.equippedCharms[3] ? Color.black : furyColor;
                fury.GetAction<Tk2dSpriteSetColor>("Activate", 18).color.Value = color;
                fury.GetAction<Tk2dSpriteSetColor>("Activate", 19).color.Value = color;
                fury.GetAction<Tk2dSpriteSetColor>("Activate", 20).color.Value = color;
                _hc.GetComponent<AbyssalBloom>().SetFury(true);
            });
            fury.InsertMethod("Stay Furied", 4, () => _hc.GetComponent<AbyssalBloom>().SetFury(true));
            fury.InsertMethod("Deactivate", 21, () => _hc.GetComponent<AbyssalBloom>().SetFury(false));
        }

        private void ModifySpellsForBloom()
		{
			_spellControl.InsertMethod("Wallside?", () =>
			{
				_hc.GetComponent<AbyssalBloom>().CancelTendrilAttack();
				_hc.GetComponent<AbyssalBloom>().CancelVerticalTendrilAttack();
				_hc.GetComponent<AbyssalBloom>().CancelWallTendrilAttack();
			}, 0);
			_spellControl.InsertMethod("Quake Antic", () =>
            {
                _hc.GetComponent<AbyssalBloom>().CancelTendrilAttack();
                _hc.GetComponent<AbyssalBloom>().CancelVerticalTendrilAttack();
                _hc.GetComponent<AbyssalBloom>().CancelWallTendrilAttack();
            }, 0);
            _spellControl.InsertMethod("Level Check 3", () =>
            {
                _hc.GetComponent<AbyssalBloom>().CancelTendrilAttack();
                _hc.GetComponent<AbyssalBloom>().CancelVerticalTendrilAttack();
                _hc.GetComponent<AbyssalBloom>().CancelWallTendrilAttack();
            }, 0);
        }

        private void CharmUpdate(PlayerData playerData, HeroController hc)
        {
            Log("Amulets Charm Update");
            if(!_activated)
			{
                Log("Amulets Start not run yet, exiting Charm Update");
                return;
			}

            _hc.GetComponent<RoyalAura>().enabled = 
                playerData.GetBool("equippedCharm_" + Charms.DefendersCrest) && FiveKnights.Instance.SaveSettings.upgradedCharm_10;

            _hc.GetComponent<PurityTimer>().enabled = FiveKnights.Instance.SaveSettings.equippedCharms[0];
            _hc.GetComponent<AutoSwing>().enabled = FiveKnights.Instance.SaveSettings.equippedCharms[0];

            _hc.GetComponent<LamentControl>().enabled = FiveKnights.Instance.SaveSettings.equippedCharms[1];
            if (FiveKnights.Instance.SaveSettings.equippedCharms[1])
            {
                _spellControl.ChangeTransition("Slug?", "FINISHED", "Focus Blast");
                _spellControl.ChangeTransition("Set HP Amount", "FINISHED", "Focus Heal Blast");
                _spellControl.ChangeTransition("Speedup?", "FINISHED", "Start MP Drain Blast");
                _spellControl.ChangeTransition("Set HP Amount 2", "FINISHED", "Focus Heal 2 Blast");
            }
            else
            {
                _spellControl.ChangeTransition("Slug?", "FINISHED", "Focus");
                _spellControl.ChangeTransition("Set HP Amount", "FINISHED", "Focus Heal");
                _spellControl.ChangeTransition("Speedup?", "FINISHED", "Start MP Drain");
                _spellControl.ChangeTransition("Set HP Amount 2", "FINISHED", "Focus Heal 2");
            }

            // Set this to disabled first so it can check for flukenest to override daggers
            _hc.GetComponent<BoonSpells>().enabled = false;
            _hc.GetComponent<BoonSpells>().enabled = FiveKnights.Instance.SaveSettings.equippedCharms[2];

            _hc.GetComponent<AbyssalBloom>().enabled = FiveKnights.Instance.SaveSettings.equippedCharms[3];
        }

        private GameObject _blastKnight;

        private IEnumerator PureVesselBlastFadeIn()
        {
            this.PlayAudio((AudioClip)_pvControl.GetAction<AudioPlayerOneShotSingle>("Focus Charge", 2).audioClip.Value);
            _blastKnight = Instantiate(FiveKnights.preloadedGO["Blast"], HeroController.instance.transform);
            _blastKnight.transform.localPosition += Vector3.up * 0.25f;
            _blastKnight.SetActive(true);
            Destroy(_blastKnight.FindGameObjectInChildren("hero_damager"));

            if (_pd.GetBool("equippedCharm_" + Charms.DeepFocus))
            {
                _blastKnight.transform.localScale *= 2.5f;
            }
            else
            {
                _blastKnight.transform.localScale *= 1.5f;
            }

            Animator anim = _blastKnight.GetComponent<Animator>();
            anim.speed = 1;
            if (_pd.GetBool("equippedCharm_" + Charms.QuickFocus))
            {
                anim.speed *= 1.3f;
            }

            if (_pd.GetBool("equippedCharm_" + Charms.DeepFocus))
            {
                anim.speed -= anim.speed * 0.35f;
            }

            yield return null;
        }

        private IEnumerator PureVesselBlast()
        {
            if (_blastKnight != null)
            {
                Log("Pure Vessel Blast");
                _blastKnight.layer = 17;
                Animator anim = _blastKnight.GetComponent<Animator>();
                anim.speed = 1;
                int hash = anim.GetCurrentAnimatorStateInfo(0).fullPathHash;
                anim.PlayInFixedTime(hash, -1, 0.8f);

                Log("Adding CircleCollider2D");
                CircleCollider2D blastCollider = _blastKnight.AddComponent<CircleCollider2D>();
                blastCollider.radius = 2.5f;
                if (_pd.GetBool("equippedCharm_" + Charms.DeepFocus))
                {
                    blastCollider.radius = 3.5f;
                }
                else
                {
                    blastCollider.radius *= 1.5f;
                }

                blastCollider.offset = Vector3.down;
                blastCollider.isTrigger = true;
                Log("Adding DebugColliders");
                //_blast.AddComponent<DebugColliders>();
                Log("Adding DamageEnemies");
                _blastKnight.AddComponent<DamageEnemies>();
                DamageEnemies damageEnemies = _blastKnight.GetComponent<DamageEnemies>();
                if (_pd.GetBool("equippedCharm_" + Charms.DeepFocus)) { damageEnemies.damageDealt = 80; }
                else { damageEnemies.damageDealt = 40; }
                damageEnemies.attackType = AttackTypes.Spell;
                damageEnemies.ignoreInvuln = false;
                damageEnemies.enabled = true;
                Log("Playing AudioClip");
                this.PlayAudio((AudioClip)_pvControl.GetAction<AudioPlayerOneShotSingle>("Focus Burst", 8).audioClip.Value, 1.5f);
                Log("Audio Clip finished");
                yield return new WaitForSeconds(.11f);
                blastCollider.enabled = false;
                yield return new WaitForSeconds(0.69f);
                Destroy(_blastKnight);
            }         
        }

        private void CancelBlast()
        {
            if (_blastKnight != null) Destroy(_blastKnight);
        }

        //Old method of setting Purity nail size, caused default nail swings to be too large
        /*private void ChangeSlashScale(float scaleX, float scaleY, float scaleZ, bool mantis = false)
        {
            Vector3 slashScale = new Vector3(scaleX, scaleY, scaleZ);

            foreach (NailSlash nailSlash in _nailSlashes)
            {
                nailSlash.SetMantis(mantis);
                nailSlash.scale = slashScale;
            }
        }*/

        //Old purity effect
        /*private void SetPuritySize()
        {
            
            foreach (NailSlash nailSlash in _nailSlashes)
            {
                if (nailSlash == null) break;
                switch (nailSlash.name)
                {
                    //Set nail size to 1.75x defaultz
                    case "Slash":
                        nailSlash.scale = new Vector3(2.835f, 2.879177f, 2.2362515f);
                        break;
                    case "AltSlash":
                        nailSlash.scale = new Vector3(2.19975f, 2.4892f, 1.9664575f);
                        break;
                    case "DownSlash":
                        nailSlash.scale = new Vector3(1.96875f, 1.974f, 1.75f);
                        break;
                    case "UpSlash":
                        nailSlash.scale = new Vector3(2.0125f, 2.45f, 2.179625f);
                        break;
                }
            }
        }
        private void RemovePuritySize()
        {
            
            foreach (NailSlash nailSlash in _nailSlashes)
            {
                if (nailSlash == null) break;
                switch (nailSlash.name)
                {
                    case "Slash":
                        nailSlash.scale = new Vector3(1.62f, 1.645244f, 1.277858f);
                        break;
                    case "AltSlash":
                        nailSlash.scale = new Vector3(1.257f, 1.4224f, 1.12369f);
                        break;
                    case "DownSlash":
                        nailSlash.scale = new Vector3(1.125f, 1.28f, 1f);
                        break;
                    case "UpSlash":
                        nailSlash.scale = new Vector3(1.15f, 1.4f, 1.2455f);
                        break;
                }
            }
        }*/

        private void OnDestroy()
        {
            Log("Destroyed Amulets");
            On.HeroController.Start -= On_HeroController_Start;
            ModHooks.CharmUpdateHook -= CharmUpdate;
        }

        private static void Log(object message) => Modding.Logger.Log("[FiveKnights][Amulets] " + message);
    }
}
