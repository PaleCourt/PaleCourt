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
        private const int SmallShotSpeed = 50;

        private const float ATTACK_COOLDOWN_REGULAR = 0.41f;
        private const float ATTACK_COOLDOWN_32 = 0.25f;
        private const float ATTACK_DURATION_REGULAR = 0.36f;
        private const float ATTACK_DURATION_32 = 0.25f;
        private const float DASH_COOLDOWN_REGULAR = 0.6f;
        private const float DASH_COOLDOWN_31 = 0.4f;
        private const float DASH_SPEED_REGULAR = 20.0f;
        private const float DASH_SPEED_16 = 28.0f;
        private const float RUN_SPEED_REGULAR = 8.3f;
        private const float RUN_SPEED_37 = 10.0f;
        private const float RUN_SPEED_31_37 = 11.4f;

        private const float ATTACK_COOLDOWN_44_L1 = ATTACK_COOLDOWN_REGULAR / 1.1f;
        private const float ATTACK_DURATION_44_L1 = ATTACK_DURATION_REGULAR / 1.1f;
        private const float DASH_COOLDOWN_44_L1 = DASH_COOLDOWN_REGULAR / 1.1f;
        private const float DASH_SPEED_44_L1 = DASH_SPEED_REGULAR * 1.1f;
        private const float DASH_SPEED_16_44_L1 = DASH_SPEED_16 * 1.1f;
        private const float RUN_SPEED_44_L1 = RUN_SPEED_REGULAR * 1.1f;

        private const float ATTACK_COOLDOWN_32_44_L1 = ATTACK_COOLDOWN_32 / 1.1f;
        private const float ATTACK_DURATION_32_44_L1 = ATTACK_DURATION_32 / 1.1f;
        private const float DASH_COOLDOWN_31_44_L1 = DASH_COOLDOWN_31 / 1.1f;
        private const float DASH_SPEED_31_44_L1 = DASH_COOLDOWN_31 / 1.1f;
        private const float RUN_SPEED_37_44_L1 = RUN_SPEED_37 * 1.1f;
        private const float RUN_SPEED_31_37_44_L1 = RUN_SPEED_31_37 * 1.1f;

        private const float ATTACK_COOLDOWN_44_L2 = ATTACK_COOLDOWN_REGULAR / 1.2f;
        private const float ATTACK_DURATION_44_L2 = ATTACK_DURATION_REGULAR / 1.2f;
        private const float DASH_COOLDOWN_44_L2 = DASH_COOLDOWN_REGULAR / 1.2f;
        private const float DASH_SPEED_44_L2 = DASH_SPEED_REGULAR * 1.2f;
        private const float RUN_SPEED_44_L2 = RUN_SPEED_REGULAR * 1.2f;

        private const float ATTACK_COOLDOWN_32_44_L2 = ATTACK_COOLDOWN_32 / 1.2f;
        private const float ATTACK_DURATION_32_44_L2 = ATTACK_DURATION_32 / 1.2f;
        private const float DASH_COOLDOWN_31_44_L2 = DASH_COOLDOWN_31 / 1.2f;
        private const float DASH_SPEED_16_44_L2 = DASH_SPEED_16 * 1.2f;
        private const float RUN_SPEED_37_44_L2 = RUN_SPEED_37 * 1.2f;
        private const float RUN_SPEED_31_37_44_L2 = RUN_SPEED_31_37 * 1.2f;

        public static int SmallShotDamage = 20;

        private HeroController _hc;// = HeroController.instance;
        private PlayerData _pd;// = PlayerData.instance;

        private AudioSource _audio;

        private List<NailSlash> _nailSlashes;

        private PlayMakerFSM _spellControl;

        private PlayMakerFSM _blastControl;
        private PlayMakerFSM _pvControl;
        private PlayMakerFSM _radControl;
        private GameObject _audioPlayerActor;
        private GameObject _knightBall;
        private GameObject _plume;
        private GameObject _smallShot;

        private tk2dSpriteAnimator _knightBallAnim;
        private tk2dSpriteAnimator _hcAnim;

        public void Awake()
        {
            On.HeroController.Awake += On_HeroController_Awake;
            On.HeroController.TakeDamage += On_HeroController_TakeDamage;
            On.HeroController.AddHealth += On_HeroController_AddHealth;
            On.HeroController.MaxHealth += On_HeroController_MaxHealth;
            On.CharmIconList.GetSprite += CharmIconList_GetSprite;
            ModHooks.CharmUpdateHook += ModHooks_CharmUpdate;
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

        public void On_HeroController_Awake(On.HeroController.orig_Awake orig, HeroController self)
        {
            orig(self);
            _hc = self;
            _pd = PlayerData.instance;

            Log("Amulets Awake");

            Log("Dash Cooldown: " + self.DASH_COOLDOWN);
            Log("Dash Cooldown Charm: " + self.DASH_COOLDOWN_CH);
            Log("Dash Speed: " + self.DASH_SPEED);
            Log("Dash Speed Sharp: " + self.DASH_SPEED_SHARP);

            //RepositionCharmsInInventory();

            _nailSlashes = new List<NailSlash>
            {
                HeroController.instance.normalSlash,
                HeroController.instance.alternateSlash,
                HeroController.instance.downSlash,
                HeroController.instance.upSlash,
            };

            _spellControl = self.gameObject.LocateMyFSM("Spell Control");

            _knightBall = Instantiate(FiveKnights.preloadedGO["Knight Ball"], self.transform);
            Vector3 localScale = _knightBall.transform.localScale;
            localScale.x *= -1;
            _knightBall.transform.localScale = localScale;
            _knightBall.transform.position += Vector3.left * 5.0f;
            _knightBallAnim = _knightBall.GetComponent<tk2dSpriteAnimator>();
            _hcAnim = self.GetComponent<tk2dSpriteAnimator>();

            CloneAndParentVoidAttacks(self);

            _pvControl = Instantiate(FiveKnights.preloadedGO["PV"].LocateMyFSM("Control"), self.transform);
            GameObject blast = Instantiate(FiveKnights.preloadedGO["Blast"]);
            blast.SetActive(true);
            _blastControl = blast.LocateMyFSM("Control");
            _plume = _pvControl.GetAction<SpawnObjectFromGlobalPool>("Plume Gen", 0).gameObject.Value;
            _smallShot = _pvControl.GetAction<FlingObjectsFromGlobalPoolTime>("SmallShot LowHigh").gameObject.Value;

            _radControl = Instantiate(FiveKnights.preloadedGO["Radiance"].LocateMyFSM("Control"), self.transform);

            //_pd.CalculateNotchesUsed();

            Log("Waiting for Audio Player Actor...");
            GameObject fireballParent = _spellControl.GetAction<SpawnObjectFromGlobalPool>("Fireball 2", 3).gameObject.Value;
            PlayMakerFSM fireballCast = fireballParent.LocateMyFSM("Fireball Cast");
            _audioPlayerActor = fireballCast.GetAction<AudioPlayerOneShotSingle>("Cast Right", 3).audioPlayer.Value;
            _audio = _audioPlayerActor.GetComponent<AudioSource>();
            Log("Got Audio");
            _audio.pitch = 1.5f;

            CreateCharmSpellEffects();
            ModifyFury();

#if DEBUG
            FiveKnights.Instance.SaveSettings.upgradedCharm_10 = true;

            FiveKnights.Instance.SaveSettings.gotCharms[0] = true;
            FiveKnights.Instance.SaveSettings.gotCharms[1] = true;
            FiveKnights.Instance.SaveSettings.gotCharms[2] = true;
            FiveKnights.Instance.SaveSettings.gotCharms[3] = true;

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
#endif
        }

        private GameObject _royalAura;

        private void CreateCharmSpellEffects()
        {
            _spellControl.CopyState("Fireball 1", "Fireball 1 SmallShots");
            _spellControl.CopyState("Fireball 2", "Fireball 2 SmallShots");

            _spellControl.RemoveAction<SpawnObjectFromGlobalPool>("Fireball 1 SmallShots");
            _spellControl.RemoveAction<SpawnObjectFromGlobalPool>("Fireball 2 SmallShots");
            _spellControl.InsertMethod("Fireball 1 SmallShots", 3, () => ShootSmallShots(-10, 10));
            _spellControl.InsertMethod("Fireball 2 SmallShots", 3, () => ShootSmallShots(-20, 20));

            _spellControl.CopyState("Quake1 Land", "Q1 Land Plumes");
            _spellControl.CopyState("Q2 Land", "Q2 Land Plumes");
            _spellControl.ChangeTransition("Q2 Land Plumes", "FINISHED", "Quake Finish");
            _spellControl.InsertCoroutine("Q1 Land Plumes", 0, SpawnPlumes);
            _spellControl.InsertCoroutine("Q2 Land Plumes", 0, SpawnPlumes);

            _spellControl.CopyState("Focus", "Focus Blast");
            _spellControl.CopyState("Focus Heal", "Focus Heal Blast");
            _spellControl.CopyState("Start MP Drain", "Start MP Drain Blast");
            _spellControl.CopyState("Focus Heal 2", "Focus Heal 2 Blast");
            _spellControl.InsertCoroutine("Focus Blast", 0, PureVesselBlastFadeIn);
            _spellControl.InsertCoroutine("Focus Heal Blast", 0, PureVesselBlast);
            _spellControl.InsertCoroutine("Start MP Drain Blast", 0, PureVesselBlastFadeIn);
            _spellControl.InsertCoroutine("Focus Heal 2 Blast", 0, PureVesselBlast);
            _spellControl.InsertMethod("Cancel All", 0, CancelBlast);

            _spellControl.CopyState("Scream Antic1", "Scream Antic1 Blasts");
            _spellControl.CopyState("Scream Burst 1", "Scream Burst 1 Blasts");
            _spellControl.CopyState("Scream Antic2", "Scream Antic2 Blasts");
            _spellControl.CopyState("Scream Burst 2", "Scream Burst 2 Blasts");
            _spellControl.ChangeTransition("Scream Antic1 Blasts", "FINISHED", "Scream Burst 1 Blasts");
            _spellControl.ChangeTransition("Scream Antic2 Blasts", "FINISHED", "Scream Burst 2 Blasts");

            _spellControl.InsertMethod("Focus Cancel", 0, CancelBlast);
            _spellControl.InsertMethod("Focus Cancel 2", 0, CancelBlast);

            _spellControl.RemoveAction<AudioPlay>("Scream Antic1 Blasts");
            _spellControl.RemoveAction<CreateObject>("Scream Burst 1 Blasts");
            _spellControl.RemoveAction<ActivateGameObject>("Scream Burst 1 Blasts");
            _spellControl.RemoveAction<ActivateGameObject>("Scream Burst 1 Blasts");
            _spellControl.RemoveAction<SendEventByName>("Scream Burst 1 Blasts");
            _spellControl.RemoveAction<SendEventByName>("Scream Burst 1 Blasts");
            _spellControl.InsertCoroutine("Scream Burst 1 Blasts", 0, () => ScreamBlasts(2));

            _spellControl.RemoveAction<AudioPlay>("Scream Antic2 Blasts");
            _spellControl.RemoveAction<CreateObject>("Scream Burst 2 Blasts");
            _spellControl.RemoveAction<ActivateGameObject>("Scream Burst 2 Blasts");
            _spellControl.RemoveAction<ActivateGameObject>("Scream Burst 2 Blasts");
            _spellControl.RemoveAction<SendEventByName>("Scream Burst 2 Blasts");
            _spellControl.RemoveAction<SendEventByName>("Scream Burst 2 Blasts");
            _spellControl.InsertCoroutine("Scream Burst 2 Blasts", 0, () => ScreamBlasts(4));
        }

        private void ShootSmallShots(int angleMin, int angleMax)
        {
            for (int angle = angleMin; angle <= angleMax; angle += 10)
            {
                GameObject smallShot =
                    Instantiate(_smallShot, HeroController.instance.transform.position, Quaternion.identity);
                smallShot.SetActive(true);
                smallShot.layer = 17;
                Destroy(smallShot.GetComponent<DamageHero>());
                Destroy(smallShot.LocateMyFSM("Control"));
                if (angle != 0) Destroy(smallShot.GetComponent<AudioSource>());
                smallShot.FindGameObjectInChildren("Dribble L").layer = 9;
                smallShot.FindGameObjectInChildren("Glow").layer = 9;
                smallShot.FindGameObjectInChildren("Beam").layer = 9;

                /*DamageEnemies damageEnemies = smallShot.AddComponent<DamageEnemies>();
                damageEnemies.enabled = true;
                damageEnemies.attackType = AttackTypes.Spell;
                damageEnemies.ignoreInvuln = false;
                damageEnemies.damageDealt = 40;*/

                Rigidbody2D rb = smallShot.GetComponent<Rigidbody2D>();
                rb.isKinematic = true;
                float xVel = SmallShotSpeed * Mathf.Cos(Mathf.Deg2Rad * angle) * -HeroController.instance.transform.localScale.x;
                float yVel = SmallShotSpeed * Mathf.Sin(Mathf.Deg2Rad * angle);
                rb.velocity = new Vector2(xVel, yVel);
                smallShot.AddComponent<SmallShot>();
                Destroy(smallShot, 5);
            }
        }

        private IEnumerator ScreamBlasts(int numBlasts)
        {
            List<GameObject> blasts = new List<GameObject>();
            GameObject blastMain = Instantiate
            (
                FiveKnights.preloadedGO["Blast"],
                HeroController.instance.transform.position + Vector3.up * 4.0f,
                Quaternion.identity
            );
            blastMain.SetActive(true);
            blasts.Add(blastMain);
            Destroy(blastMain.FindGameObjectInChildren("hero_damager"));
            Animator anim = blastMain.GetComponent<Animator>();
            int hash = anim.GetCurrentAnimatorStateInfo(0).fullPathHash;
            anim.PlayInFixedTime(hash, -1, 0.8f);
            CircleCollider2D blastCollider = blastMain.AddComponent<CircleCollider2D>();
            blastCollider.radius = 2.5f;
            blastCollider.isTrigger = true;
            DamageEnemies damageEnemies = blastMain.AddComponent<DamageEnemies>();
            damageEnemies.damageDealt = 30;
            damageEnemies.attackType = AttackTypes.Spell;
            damageEnemies.ignoreInvuln = false;
            damageEnemies.enabled = true;
            AudioPlayerOneShotSingle("Burst", 1.2f, 1.5f);

            yield return new WaitForSeconds(0.1f);

            for (int i = 0; i < numBlasts; i++)
            {
                GameObject blast = Instantiate
                (
                    FiveKnights.preloadedGO["Blast"],
                    HeroController.instance.transform.position + Vector3.up * Random.Range(4, 10) + Vector3.right * Random.Range(-6, 6),
                    Quaternion.identity
                );
                blast.SetActive(true);
                blasts.Add(blast);
                Destroy(blast.FindGameObjectInChildren("hero_damager"));
                anim = blast.GetComponent<Animator>();
                anim.PlayInFixedTime(hash, -1, 0.75f);
                blastCollider = blastMain.AddComponent<CircleCollider2D>();
                blastCollider.radius = 2.5f;
                blastCollider.isTrigger = true;
                damageEnemies = blast.AddComponent<DamageEnemies>();
                damageEnemies.damageDealt = 30;
                damageEnemies.attackType = AttackTypes.Spell;
                damageEnemies.ignoreInvuln = false;
                damageEnemies.enabled = true;
                AudioPlayerOneShotSingle("Burst", 1.2f, 1.5f);
                yield return new WaitForSeconds(0.1f);
            }

            foreach (GameObject blast in blasts)
            {
                Destroy(blast);
            }
        }

        private void ModifyFury()
        {
            PlayMakerFSM fury = _hc.gameObject.FindGameObjectInChildren("Charm Effects").LocateMyFSM("Fury");
            Log("Fury Color: " + fury.GetAction<Tk2dSpriteSetColor>("Activate", 17).color.Value);
            Color furyColor = fury.GetAction<Tk2dSpriteSetColor>("Activate", 18).color.Value;
            fury.InsertMethod
            (
                "Activate",
                17,
                () =>
                {
                    Color color = FiveKnights.Instance.SaveSettings.equippedCharms[3] ? Color.black : furyColor;
                    fury.GetAction<Tk2dSpriteSetColor>("Activate", 17).color.Value = color;
                    fury.GetAction<Tk2dSpriteSetColor>("Activate", 18).color.Value = color;
                    fury.GetAction<Tk2dSpriteSetColor>("Activate", 19).color.Value = color;
                }
            );
        }

        private void ModHooks_CharmUpdate(PlayerData playerData, HeroController hc)
        {
            Log("Charm Update");

            if (playerData.GetBool("equippedCharm_" + Charms.DefendersCrest) && FiveKnights.Instance.SaveSettings.upgradedCharm_10)
            {
                StartCoroutine(FindAndAddComponentToDung());
                /*if (_royalAura != null) Destroy(_royalAura);
                _royalAura = Instantiate(FiveKnights.preloadedGO["Royal Aura"]);
                Vector3 pos = hc.transform.position;
                Transform auraTransform = _royalAura.transform;
                auraTransform.SetPosition2D(pos);
                auraTransform.SetPositionZ(pos.z + 1.0f);
                auraTransform.parent = gameObject.transform;
                _royalAura.FindGameObjectInChildren("Smoke 0").AddComponent<RoyalAura>();*/
            }
            else
            {
                if (_royalAura != null) Destroy(_royalAura);
            }

            if (FiveKnights.Instance.SaveSettings.equippedCharms[0])
            {
                ChangeSlashScale(3, true);
            }
            else
            {
                ChangeSlashScale(1.6f);
            }

            if (FiveKnights.Instance.SaveSettings.equippedCharms[1])
            {
                _spellControl.ChangeTransition("Level Check 3", "LEVEL 1", "Scream Antic1 Blasts");
                _spellControl.ChangeTransition("Level Check 3", "LEVEL 2", "Scream Antic2 Blasts");

                _spellControl.ChangeTransition("Slug?", "FINISHED", "Focus Blast");
                _spellControl.ChangeTransition("Set HP Amount", "FINISHED", "Focus Heal Blast");
                _spellControl.ChangeTransition("Speedup?", "FINISHED", "Start MP Drain Blast");
                _spellControl.ChangeTransition("Set HP Amount 2", "FINISHED", "Focus Heal 2 Blast");
            }
            else
            {
                _spellControl.ChangeTransition("Level Check 3", "LEVEL 1", "Scream Antic1");
                _spellControl.ChangeTransition("Level Check 3", "LEVEL 2", "Scream Antic2");

                _spellControl.ChangeTransition("Slug?", "FINISHED", "Focus");
                _spellControl.ChangeTransition("Set HP Amount", "FINISHED", "Focus Heal");
                _spellControl.ChangeTransition("Speedup?", "FINISHED", "Start MP Drain");
                _spellControl.ChangeTransition("Set HP Amount 2", "FINISHED", "Focus Heal 2");
            }

            if (FiveKnights.Instance.SaveSettings.equippedCharms[2])
            {
                _spellControl.ChangeTransition("Quake1 Down", "HERO LANDED", "Q1 Land Plumes");
                _spellControl.ChangeTransition("Quake2 Down", "HERO LANDED", "Q2 Land Plumes");

                _spellControl.ChangeTransition("Level Check", "LEVEL 1", "Fireball 1 SmallShots");
                _spellControl.ChangeTransition("Level Check", "LEVEL 2", "Fireball 2 SmallShots");
            }
            else
            {
                _spellControl.ChangeTransition("Quake1 Down", "HERO LANDED", "Quake1 Land");
                _spellControl.ChangeTransition("Quake2 Down", "HERO LANDED", "Q2 Land");

                _spellControl.ChangeTransition("Level Check", "LEVEL 1", "Fireball 1");
                _spellControl.ChangeTransition("Level Check", "LEVEL 2", "Fireball 2");
            }

            if (!FiveKnights.Instance.SaveSettings.equippedCharms[3])
            {
                ResetHeroControllerProperties();
            }
        }

        private enum HeroUpdateType
        {
            Reset,
            Modify
        }

        private void ModifyHeroControllerProperties(HeroUpdateType t)
        {
            foreach (NailSlash nailSlash in _nailSlashes)
                nailSlash.SetFury(t == HeroUpdateType.Modify);

            Color color = t switch
            {
                HeroUpdateType.Reset => Color.white,
                HeroUpdateType.Modify => Color.black,
                _ => throw new InvalidEnumArgumentException()
            };

            foreach (GameObject slash in new GameObject[]
            {
                _hc.slashPrefab,
                _hc.slashAltPrefab,
                _hc.downSlashPrefab,
                _hc.upSlashPrefab,
                _hc.wallSlashPrefab
            })
            {
                slash.GetComponent<tk2dSprite>().color = color;
            }

            GameObject attacks = HeroController.instance.gameObject.FindGameObjectInChildren("Attacks");

            foreach (string child in new[] { "Cyclone Slash", "Dash Slash", "Great Slash" })
            {
                attacks.FindGameObjectInChildren(child).GetComponent<tk2dSprite>().color = color;
                foreach (var item in attacks.FindGameObjectInChildren(child).GetComponentsInChildren<tk2dSprite>())
                    item.color = color;
            }
        }

        private void ModifyHeroControllerPropertiesLevel1()
        {
            _pd.nailDamage = Mathf.FloorToInt((5 + 4 * _pd.nailSmithUpgrades) * 1.25f);

            if (_pd.equippedCharm_16)
            {
                _hc.DASH_SPEED_SHARP = DASH_SPEED_16_44_L1;
            }
            else
            {
                _hc.DASH_SPEED = DASH_SPEED_44_L1;
            }

            if (_pd.equippedCharm_37)
            {
                if (_pd.equippedCharm_31)
                {
                    _hc.RUN_SPEED_CH_COMBO = RUN_SPEED_31_37_44_L1;
                }
                else
                {
                    _hc.RUN_SPEED_CH = RUN_SPEED_37_44_L1;
                }
            }

            if (_pd.equippedCharm_32)
            {
                _hc.ATTACK_COOLDOWN_TIME_CH = ATTACK_COOLDOWN_32_44_L1;
                _hc.ATTACK_DURATION_CH = ATTACK_DURATION_32_44_L1;
            }

            if (_pd.equippedCharm_31)
            {
                _hc.DASH_COOLDOWN_CH = DASH_COOLDOWN_31_44_L1;
            }

            _hc.DASH_SPEED = DASH_SPEED_44_L1;

            ModifyHeroControllerProperties(HeroUpdateType.Modify);

            On.HeroController.SoulGain += On_HeroController_SoulGain_L1;
            On.HeroController.SoulGain -= On_HeroController_SoulGain_L2;
        }

        private void ModifyHeroControllerPropertiesLevel2()
        {
            _pd.nailDamage = Mathf.FloorToInt((5 + 4 * _pd.nailSmithUpgrades) * 1.5f);

            if (_pd.equippedCharm_16)
                _hc.DASH_SPEED_SHARP = DASH_SPEED_16_44_L2;
            else
                _hc.DASH_SPEED = DASH_SPEED_44_L2;

            if (_pd.equippedCharm_37)
            {
                if (_pd.equippedCharm_31)
                    _hc.RUN_SPEED_CH_COMBO = RUN_SPEED_31_37_44_L2;
                else
                    _hc.RUN_SPEED_CH = RUN_SPEED_37_44_L2;
            }

            if (_pd.equippedCharm_32)
            {
                _hc.ATTACK_COOLDOWN_TIME_CH = ATTACK_COOLDOWN_32_44_L2;
                _hc.ATTACK_DURATION_CH = ATTACK_DURATION_32_44_L2;
            }

            if (_pd.equippedCharm_31)
            {
                _hc.DASH_COOLDOWN_CH = DASH_COOLDOWN_31_44_L2;
            }

            _hc.DASH_SPEED = DASH_SPEED_44_L2;

            ModifyHeroControllerProperties(HeroUpdateType.Modify);

            On.HeroController.SoulGain -= On_HeroController_SoulGain_L1;
            On.HeroController.SoulGain += On_HeroController_SoulGain_L2;
        }

        void On_HeroController_SoulGain_L1(On.HeroController.orig_SoulGain orig, HeroController self)
        {
            int charge;
            if (_pd.GetInt("MPCharge") < _pd.GetInt("maxMP"))
                charge = 4;
            else
                charge = 2;
            _pd.AddMPCharge(charge);
            int mpReserve = _pd.GetInt("MPReserve");
            GameCameras.instance.soulOrbFSM.SendEvent("MP GAIN");
            if (_pd.GetInt("MPReserve") == mpReserve)
                return;
            GameManager.instance.soulVessel_fsm.SendEvent("MP RESERVE UP");
        }

        static void On_HeroController_SoulGain_L2(On.HeroController.orig_SoulGain orig, HeroController self)
        {
            // Gain no soul at all when striking enemies
        }

        private void ResetHeroControllerProperties()
        {
            _hc.ATTACK_COOLDOWN_TIME = ATTACK_COOLDOWN_REGULAR;
            _hc.ATTACK_COOLDOWN_TIME_CH = ATTACK_COOLDOWN_32;
            _hc.ATTACK_DURATION = ATTACK_DURATION_REGULAR;
            _hc.ATTACK_DURATION_CH = ATTACK_DURATION_32;
            _hc.DASH_COOLDOWN = DASH_COOLDOWN_REGULAR;
            _hc.DASH_COOLDOWN_CH = DASH_COOLDOWN_31;
            _hc.DASH_SPEED = DASH_SPEED_REGULAR;
            _hc.DASH_SPEED_SHARP = DASH_SPEED_16;
            _hc.RUN_SPEED = RUN_SPEED_REGULAR;
            _hc.RUN_SPEED_CH = RUN_SPEED_37;
            _hc.RUN_SPEED_CH_COMBO = RUN_SPEED_31_37;

            _pd.nailDamage = 5 + 4 * _pd.nailSmithUpgrades;

            ModifyHeroControllerProperties(HeroUpdateType.Reset);

            On.HeroController.SoulGain -= On_HeroController_SoulGain_L1;
            On.HeroController.SoulGain -= On_HeroController_SoulGain_L2;
        }

        private int _shadeSlashNum = 1;

        private static void AddFsm(GameObject o, HeroController _hc)
        {
            // for hitbox viewing experience
            var tempFsm = o.AddComponent<PlayMakerFSM>();
            var fsm = _hc.gameObject.Find("AltSlash").LocateMyFSM("damages_enemy");
            foreach (var fi in typeof(PlayMakerFSM).GetFields(BindingFlags.Instance | BindingFlags.NonPublic |
                                                              BindingFlags.Public))
            {
                fi.SetValue(tempFsm, fi.GetValue(fsm));
            }
        }

        private void TendrilAttack()
        {
            var rb = _hc.GetComponent<Rigidbody2D>();
            var mr = _hc.GetComponent<MeshRenderer>();

            IEnumerator SlashAntic()
            {
                _hc.RelinquishControl();

                rb.velocity = Vector2.zero;
                rb.isKinematic = true;

                AudioPlayerOneShotSingle("Shade Slash Antic");

                mr.enabled = false;

                _knightBall.SetActive(true);

                yield return new WaitForSeconds(_knightBallAnim.PlayAnimGetTime("Slash" + _shadeSlashNum + " Antic"));

                StartCoroutine(Slash());
            }

            IEnumerator Slash()
            {
                GameObject shadeSlash = Instantiate(new GameObject("Shade Slash"), _knightBall.transform);
                shadeSlash.layer = 17;
                shadeSlash.tag = "Nail Attack";
                shadeSlash.transform.localPosition = new Vector3(0f, 0f, 0f);
                shadeSlash.transform.localScale = new Vector3(1f, 1f, 1f);
                shadeSlash.SetActive(false);

                var slashPoly = shadeSlash.AddComponent<PolygonCollider2D>();

                AddFsm(shadeSlash, _hc);

                slashPoly.points = new[]
                {
                    new Vector2(0.0f, -2.0f),
                    new Vector2(3.5f, -2.0f),
                    new Vector2(3.5f, 0.0f),
                    new Vector2(3.0f, 1.0f),
                    new Vector2(0.0f, 2.0f),
                    new Vector2(-3f, 0.0f), // to have parts of the player covered with a hitbox
                };

                slashPoly.offset = new Vector2(0.0f, 0.0f);
                slashPoly.isTrigger = true;

                var damageEnemies = shadeSlash.AddComponent<DamageEnemies>();
                damageEnemies.direction = _hc.transform.localScale.x;
                damageEnemies.attackType = AttackTypes.Nail;
                damageEnemies.ignoreInvuln = false;
                damageEnemies.damageDealt = 75;

                shadeSlash.AddComponent<ShadeSlash>().audioPlayer = _audioPlayerActor;

                shadeSlash.SetActive(true);

                yield return new WaitForSeconds(_knightBallAnim.PlayAnimGetTime("Slash" + _shadeSlashNum));

                Destroy(shadeSlash);

                mr.enabled = true;

                _knightBall.SetActive(false);

                if (_shadeSlashNum == 1)
                    _shadeSlashNum++;
                else
                    _shadeSlashNum--;

                _hc.RegainControl();

                rb.isKinematic = false;
            }

            StartCoroutine(SlashAntic());
        }

        private void VerticalTendrilAttack(bool up)
        {
            var rb = _hc.GetComponent<Rigidbody2D>();
            string animName = up ? "Up" : "Down";

            IEnumerator SlashAntic()
            {
                _hc.RelinquishControl();
                _hc.StopAnimationControl();

                rb.velocity = Vector2.zero;
                rb.isKinematic = true;

                AudioPlayerOneShotSingle("Shade Slash Antic");

                _hcAnim.Play(animName + "Slash Void");
                _hcAnim.Play(_hcAnim.GetClipByName(animName + "Slash Void"));

                yield return new WaitWhile(() => _hcAnim.CurrentFrame < 1);

                StartCoroutine(Slash());
            }

            IEnumerator Slash()
            {
                GameObject shadeSlash = Instantiate(new GameObject("Shade Slash"), _hc.transform);
                shadeSlash.layer = 17;
                shadeSlash.tag = "Nail Attack";
                shadeSlash.transform.localPosition = new Vector3(0f, up ? 1.0f : -2.0f, 0f);
                shadeSlash.transform.localScale = new Vector3(2, 2, 2);
                shadeSlash.SetActive(false);

                var slashPoly = shadeSlash.AddComponent<PolygonCollider2D>();
                shadeSlash.AddComponent<MeshRenderer>();
                shadeSlash.AddComponent<MeshFilter>();
                var slashSprite = shadeSlash.AddComponent<tk2dSprite>();
                var slashAnim = shadeSlash.AddComponent<tk2dSpriteAnimator>();

                AddFsm(shadeSlash, _hc);

                if (up)
                    slashPoly.points = new[]
                    {
                        new Vector2(-1f, 0f),
                        new Vector2(-0.75f, 1.5f),
                        new Vector2(-0.5f, 2.0f),
                        new Vector2(0f, 2.25f),
                        new Vector2(0.5f, 2.0f),
                        new Vector2(0.75f, 1.5f),
                        new Vector2(1f, 0f),
                        new Vector2(0f, 0.5f)
                    };
                else
                    slashPoly.points = new[]
                    {
                        new Vector2(-1f, -0f),
                        new Vector2(-1.25f, -0.5f),
                        new Vector2(-0.875f, -1.5f),
                        new Vector2(-0.5f, -1.9f),
                        new Vector2(0f, -2.2f),
                        new Vector2(0.5f, -2.0f),
                        new Vector2(0.875f, -1.5f),
                        new Vector2(1.25f, -0.5f),
                        new Vector2(1f, -0f),
                        new Vector2(0f, 0.5f)
                    };

                slashPoly.offset = new Vector2(0.0f, up ? -1f : 0.75f);
                slashPoly.isTrigger = true;

                slashSprite.Collection = _hc.GetComponent<tk2dSprite>().Collection;
                slashAnim.Library = _hcAnim.Library;

                var damageEnemies = shadeSlash.AddComponent<DamageEnemies>();
                damageEnemies.direction = _hc.transform.localScale.x;
                damageEnemies.attackType = AttackTypes.Nail;
                damageEnemies.ignoreInvuln = false;
                damageEnemies.damageDealt = 75;

                shadeSlash.AddComponent<ShadeSlash>().audioPlayer = _audioPlayerActor;

                shadeSlash.SetActive(true);

                yield return new WaitForSeconds(slashAnim.PlayAnimGetTime(animName + "Slash Effect" + (_pd.GetBool("equippedCharm_13") ? " M" : "")));

                Destroy(shadeSlash);

                //if (!up)
                //    yield return new WaitWhile(() => _hcAnim.Playing);
                yield return new WaitWhile(() => _hcAnim.Playing);

                _hc.StartAnimationControl();
                _hc.RegainControl();

                rb.isKinematic = false;
            }

            StartCoroutine(SlashAntic());
        }

        private void WallTendrilAttack()
        {
            var rb = _hc.GetComponent<Rigidbody2D>();

            IEnumerator Slash()
            {
                _hc.RelinquishControl();
                _hc.StopAnimationControl();

                rb.velocity = Vector2.zero;
                rb.isKinematic = true;

                AudioPlayerOneShotSingle("Shade Slash Antic");

                _hcAnim.Play(_hcAnim.GetClipByName("WallSlash Void"));

                GameObject shadeSlash = Instantiate(new GameObject("Shade Slash"), _hc.transform);
                shadeSlash.layer = 17;
                shadeSlash.tag = "Nail Attack";
                shadeSlash.transform.localPosition = new Vector3(0f, 1.0f, 0f);
                shadeSlash.transform.localScale = new Vector3(1.0f, 1.0f, 1.0f);
                shadeSlash.SetActive(false);

                var slashPoly = shadeSlash.AddComponent<PolygonCollider2D>();
                shadeSlash.AddComponent<MeshRenderer>();
                shadeSlash.AddComponent<MeshFilter>();
                var slashSprite = shadeSlash.AddComponent<tk2dSprite>();
                var slashAnim = shadeSlash.AddComponent<tk2dSpriteAnimator>();

                AddFsm(shadeSlash, _hc);

                slashPoly.points = new[]
                {
                    new Vector2(-1.5f, 2.0f),
                    new Vector2(1.0f, 1.5f),
                    new Vector2(3.0f, -1.0f),
                    new Vector2(1.0f, -2.0f),
                    new Vector2(-1.5f, -1.0f),
                };

                slashPoly.offset = new Vector2(1.0f, 0.0f);
                slashPoly.isTrigger = true;

                slashSprite.Collection = _hc.GetComponent<tk2dSprite>().Collection;
                slashAnim.Library = _hcAnim.Library;

                var damageEnemies = shadeSlash.AddComponent<DamageEnemies>();
                damageEnemies.direction = _hc.transform.localScale.x;
                damageEnemies.attackType = AttackTypes.Nail;
                damageEnemies.ignoreInvuln = false;
                damageEnemies.damageDealt = 75;

                shadeSlash.AddComponent<ShadeSlash>().audioPlayer = _audioPlayerActor;

                shadeSlash.SetActive(true);

                yield return new WaitForSeconds(slashAnim.PlayAnimGetTime("Slash Effect"));

                Destroy(shadeSlash);

                _hc.StartAnimationControl();
                _hc.RegainControl();

                rb.isKinematic = false;
            }

            StartCoroutine(Slash());
        }

        private void On_HeroController_TakeDamage
        (
            On.HeroController.orig_TakeDamage orig,
            HeroController self,
            GameObject go,
            CollisionSide collisionSide,
            int damageAmount,
            int hazardType
        )
        {
            int oldHealth = _pd.health;

            orig(self, go, collisionSide, damageAmount, hazardType);

            if (_pd.health == oldHealth) return;

            if (!FiveKnights.Instance.SaveSettings.equippedCharms[3]) return;

            if (_pd.health <= 1)
            {
                Log("Damaged to level 2");
                ModifyHeroControllerPropertiesLevel2();
                On.HeroController.Attack += On_HeroController_Attack;
            }
            else if (_pd.health <= _pd.maxHealth / 2)
            {
                Log("Damaged to level 1");
                ModifyHeroControllerPropertiesLevel1();
            }
        }

        private void On_HeroController_AddHealth(On.HeroController.orig_AddHealth orig, HeroController self, int amount)
        {
            orig(self, amount);

            if (!FiveKnights.Instance.SaveSettings.equippedCharms[3]) return;

            if (_pd.health > _pd.maxHealth / 2)
            {
                Log("Healed to no effect");
                ResetHeroControllerProperties();
            }
            else if (_pd.health > 1 && _pd.health <= _pd.maxHealth / 2)
            {
                Log("Healed to level 1");
                ModifyHeroControllerPropertiesLevel1();
                On.HeroController.Attack -= On_HeroController_Attack;
            }
        }

        private void On_HeroController_MaxHealth(On.HeroController.orig_MaxHealth orig, HeroController self)
        {
            orig(self);

            if (!FiveKnights.Instance.SaveSettings.equippedCharms[3]) return;

            Log("Reset HeroController Properties");
            ResetHeroControllerProperties();
            On.HeroController.Attack -= On_HeroController_Attack;
        }

        //private static readonly string[] CUSTOM_BACKBOARDS = new[]
        //{
        //    "BB 1",
        //    "BB 12",
        //    "BB 23",
        //    "BB 34"
        //};

        //private static string BackboardToCharm(string name)
        //{
        //    return name switch
        //    {
        //        "BB 1" => "41",
        //        "BB 12" => "42",
        //        "BB 23" => "43",
        //        "BB 34" => "44",
        //        _ => throw new ArgumentException("Backboard does not match custom charm!")
        //    };
        //}

        //private void On_InvCharmBackboard_OnEnable(On.InvCharmBackboard.orig_OnEnable orig, InvCharmBackboard self)
        //{
        //    if (!CUSTOM_BACKBOARDS.Contains(self.gameObject.name))
        //    {
        //        orig(self);

        //        return;
        //    }

        //    self.charmObject.transform.localPosition = self.transform.localPosition - new Vector3(0, 0, 1f / 1000f);

        //    string charm = BackboardToCharm(self.gameObject.name);

        //    bool got_charm = PlayerData.instance.GetBool($"gotCharm_{charm}");
        //    bool new_charm = PlayerData.instance.GetBool($"newCharm_{charm}");
        //    bool blanked = self.GetAttr<InvCharmBackboard, bool>("blanked");

        //    if (got_charm && new_charm)
        //        self.newOrb.SetActive(true);

        //    if (got_charm && !blanked)
        //    {
        //        self.GetAttr<InvCharmBackboard, SpriteRenderer>("spriteRenderer").sprite = self.blankSprite;
        //        self.SetAttr("blanked", true);
        //    }

        //    if (got_charm || !blanked)
        //        return;

        //    self.GetAttr<InvCharmBackboard, SpriteRenderer>("spriteRenderer").sprite = self.activeSprite;
        //    self.SetAttr("blanked", false);
        //}

        //private void On_InvCharmBackboard_SelectCharm
        //(
        //    On.InvCharmBackboard.orig_SelectCharm orig,
        //    InvCharmBackboard self
        //)
        //{
        //    if (!CUSTOM_BACKBOARDS.Contains(self.gameObject.name))
        //    {
        //        orig(self);

        //        return;
        //    }

        //    string charm = BackboardToCharm(self.gameObject.name);

        //    if (!PlayerData.instance.GetBool("newCharm_{charm}"))
        //        return;

        //    PlayerData.instance.SetBool($"newCharm_{charm}", false);

        //    self.newOrb.GetComponent<SimpleFadeOut>().FadeOut();
        //}

        private IEnumerator FindAndAddComponentToDung()
        {
            yield return new WaitWhile(() => !GameObject.Find("Dung"));
            // Destroy(GameObject.Find("Dung"));
            GameObject dung = GameObject.Find("Dung");
            if (!dung.GetComponent<Dung>()) dung.AddComponent<Dung>();
        }

        private IEnumerator SpawnPlumes()
        {
            for (float x = 2; x <= 10; x += 2)
            {
                Vector2 pos = HeroController.instance.transform.position;
                float plumeY = pos.y - 1.8f;

                GameObject plumeL = Instantiate(_plume, new Vector2(pos.x - x, plumeY), Quaternion.identity);
                plumeL.SetActive(true);
                plumeL.AddComponent<Plume>();

                GameObject plumeR = Instantiate(_plume, new Vector2(pos.x + x, plumeY), Quaternion.identity);
                plumeR.SetActive(true);
                plumeR.AddComponent<Plume>();
            }

            yield return new WaitForSeconds(0.25f);
            AudioPlayerOneShotSingle("Plume Up", 1.5f, 1.5f);
        }

        private void Update()
        {
            //GameObject cursor = GameManager.instance.inventoryFSM.gameObject.FindGameObjectInChildren("Charms").FindGameObjectInChildren("Cursor");
            //Log("Cursor pos: " + cursor.transform.position);
            // Log("Equipped Charms: " + PureAmulets.Settings.equippedCharm_41 + " " +
            //     PureAmulets.Settings.equippedCharm_42 + " " + PureAmulets.Settings.equippedCharm_43 + " " +
            //     PureAmulets.Settings.equippedCharm_44);
        }

        private void On_HeroController_Attack(On.HeroController.orig_Attack origAttack, HeroController hc, AttackDirection dir)
        {
            InputHandler ih = InputHandler.Instance;
            
            if (hc.cState.wallSliding)
            {
                WallTendrilAttack();
            }
            else if
            (
                ih.ActionButtonToPlayerAction(HeroActionButton.DOWN) && !hc.CheckTouchingGround()
                //|| ih.ActionButtonToPlayerAction(HeroActionButton.UP)
            )
            {
                //origAttack(hc, dir);
                VerticalTendrilAttack(false);
            }
            else if (ih.ActionButtonToPlayerAction(HeroActionButton.UP))
            {
                //origAttack(hc, dir);
                VerticalTendrilAttack(true);
            }
            else
            {
                TendrilAttack();
            }
        }

        private GameObject _blast;

        private IEnumerator PureVesselBlastFadeIn()
        {
            AudioPlayerOneShotSingle("Focus Charge", 1.2f, 1.5f);
            _blast = Instantiate(FiveKnights.preloadedGO["Blast"], HeroController.instance.transform);
            _blast.transform.localPosition += Vector3.up * 0.25f;
            _blast.SetActive(true);
            Destroy(_blast.FindGameObjectInChildren("hero_damager"));

            if (_pd.GetBool("equippedCharm_" + Charms.DeepFocus))
            {
                _blast.transform.localScale *= 2.5f;
            }
            else
            {
                _blast.transform.localScale *= 1.5f;
            }

            Animator anim = _blast.GetComponent<Animator>();
            anim.speed = 1;
            if (_pd.GetBool("equippedCharm_" + Charms.QuickFocus))
            {
                anim.speed *= 1.5f;
            }

            if (_pd.GetBool("equippedCharm_" + Charms.DeepFocus))
            {
                anim.speed -= anim.speed * 0.35f;
            }

            yield return null;
        }

        private IEnumerator PureVesselBlast()
        {
            Log("Pure Vessel Blast");
            _blast.layer = 17;
            Animator anim = _blast.GetComponent<Animator>();
            anim.speed = 1;
            int hash = anim.GetCurrentAnimatorStateInfo(0).fullPathHash;
            anim.PlayInFixedTime(hash, -1, 0.8f);

            Log("Adding CircleCollider2D");
            CircleCollider2D blastCollider = _blast.AddComponent<CircleCollider2D>();
            blastCollider.radius = 2.5f;
            if (_pd.GetBool("equippedCharm_" + Charms.DeepFocus))
            {
                blastCollider.radius *= 2.5f / 1.5f;
            }

            blastCollider.offset = Vector3.up;
            blastCollider.isTrigger = true;
            Log("Adding DebugColliders");
            //_blast.AddComponent<DebugColliders>();
            Log("Adding DamageEnemies");
            DamageEnemies damageEnemies = _blast.AddComponent<DamageEnemies>();
            damageEnemies.damageDealt = 50;
            damageEnemies.attackType = AttackTypes.Spell;
            damageEnemies.ignoreInvuln = false;
            damageEnemies.enabled = true;
            Log("Playing AudioClip");
            AudioPlayerOneShotSingle("Burst", 1.5f, 1.5f);
            yield return new WaitForSeconds(0.1f);
            Destroy(_blast);
        }

        private void CancelBlast()
        {
            if (_blast != null) Destroy(_blast);
            _audio.Stop();
        }

        private void ChangeSlashScale(float scale, bool mantis = false)
        {
            Vector3 slashScale = new Vector3(scale, scale, 1);

            foreach (NailSlash nailSlash in _nailSlashes)
            {
                nailSlash.SetMantis(mantis);
                nailSlash.scale = slashScale;
            }
        }

        private void AudioPlayerOneShotSingle(AudioClip clip, float pitchMin = 1.0f, float pitchMax = 1.0f, float time = 1.0f, float volume = 1.0f)
        {
            GameObject actorInstance = _audioPlayerActor.Spawn(HeroController.instance.transform.position, Quaternion.Euler(Vector3.up));
            AudioSource audio = actorInstance.GetComponent<AudioSource>();
            audio.pitch = Random.Range(pitchMin, pitchMax);
            audio.volume = volume;
            audio.PlayOneShot(clip);
        }

        private void AudioPlayerOneShotSingle(string clipName, float pitchMin = 1.0f, float pitchMax = 1.0f, float time = 1.0f, float volume = 1.0f)
        {
            AudioClip GetAudioClip()
            {
                switch (clipName)
                {
                    case "Burst":
                        return (AudioClip)_pvControl.GetAction<AudioPlayerOneShotSingle>("Focus Burst", 8).audioClip.Value;
                    case "Focus Charge":
                        return (AudioClip)_pvControl.GetAction<AudioPlayerOneShotSingle>("Focus Charge", 2).audioClip.Value;
                    case "Plume Up":
                        return (AudioClip)_pvControl.GetAction<AudioPlayerOneShotSingle>("Plume Up", 1).audioClip.Value;
                    case "Shade Slash Antic":
                        return (AudioClip)_radControl.GetAction<AudioPlayerOneShotSingle>("Antic", 1).audioClip.Value;
                    case "Shade Slash":
                        return (AudioClip)_radControl.GetAction<AudioPlayerOneShotSingle>("Slash", 1).audioClip.Value;
                    case "Small Burst":
                        return (AudioClip)_blastControl.GetAction<AudioPlayerOneShotSingle>("Sound", 1).audioClip.Value;
                    default:
                        return null;
                }
            }

            AudioPlayerOneShotSingle(GetAudioClip(), pitchMin, pitchMax, time, volume);
        }

        private void CloneAndParentVoidAttacks(HeroController self)
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
            foreach (tk2dSpriteDefinition def in collection.spriteCollection.spriteDefinitions)
            {
                def.material.shader = shader;
                knightSpriteDefs.Add(def);
            }
            heroSprite.Collection.spriteDefinitions = knightSpriteDefs.ToArray();
            List<tk2dSpriteAnimationClip> knightClips = knightAnim.Library.clips.ToList();
            foreach (tk2dSpriteAnimationClip clip in animation.clips)
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

            // Create states to test for activated Abyssal Bloom
            nailArts.CreateState("Bloom Activated CSlash?");
            nailArts.CreateState("Bloom Activated DSlash?");
            nailArts.CreateState("Bloom Activated GSlash?");

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

            // Change Knight animation clips
            nailArts.GetAction<Tk2dPlayAnimationWithEvents>("Cyclone Start Void").clipName = "NA Cyclone Start Void";
            nailArts.GetAction<Tk2dPlayAnimation>("Cyclone Spin Void").clipName = "NA Cyclone Void";
            nailArts.GetAction<Tk2dPlayAnimation>("Cyclone Extend Void").clipName = "NA Cyclone Void";
            nailArts.GetAction<Tk2dPlayAnimationWithEvents>("Cyclone End Void").clipName = "NA Cyclone End Void";
            nailArts.GetAction<Tk2dPlayAnimationWithEvents>("Dash Slash Void").clipName = "NA Dash Slash Void";
            nailArts.GetAction<Tk2dPlayAnimationWithEvents>("G Slash Void").clipName = "NA Big Slash Void";

            // Insert testing methods for testing states
            nailArts.InsertMethod("Bloom Activated CSlash?", 0, () =>
            {
                nailArts.SetState(FiveKnights.Instance.SaveSettings.equippedCharms[3] && _pd.health <= 10 ? "Cyclone Start Void" : "Cyclone Start");
            });
            nailArts.InsertMethod("Bloom Activated DSlash?", 0, () =>
            {
                nailArts.SetState(FiveKnights.Instance.SaveSettings.equippedCharms[3] && _pd.health <= 10 ? "Dash Slash Void" : "Dash Slash");
            });
            nailArts.InsertMethod("Bloom Activated GSlash?", 0, () =>
            {
                Log($"PureAmulets.Settings.equippedCharm_44: {FiveKnights.Instance.SaveSettings.equippedCharms[3]}, health: {_pd.health <= 10}");
                nailArts.SetState(FiveKnights.Instance.SaveSettings.equippedCharms[3] && _pd.health <= 10 ? "G Slash Void" : "G Slash");
            });

            // Insert activation and deactivation of void nail arts
            nailArts.InsertMethod("Activate Slash Void", 0, () =>
            {
                cycloneSlashVoid.SetActive(true);
                cycloneSlashVoid.GetComponent<tk2dSpriteAnimator>().Play("Cyclone Slash Effect Void");
            });
            nailArts.InsertMethod("Cyclone End Void", 2, () => cycloneSlashVoid.SetActive(false));
            nailArts.InsertMethod("Dash Slash Void", 0, () =>
            {
                dashSlashVoid.SetActive(true);
                dashSlashVoid.GetComponent<tk2dSpriteAnimator>().Play("Dash Slash Effect Void");
            });
            nailArts.InsertMethod("D Slash End Void", 0, () => dashSlashVoid.SetActive(false));
            nailArts.InsertMethod("G Slash Void", 0, () =>
            {
                greatSlashVoid.SetActive(true);
                greatSlashVoid.GetComponent<tk2dSpriteAnimator>().Play("Great Slash Effect Void");
            });
            nailArts.InsertMethod("G Slash End Void", 0, () => greatSlashVoid.SetActive(false));

            // Remove activating old nail art effects
            nailArts.RemoveAction<ActivateGameObject>("Activate Slash Void");
            nailArts.RemoveAction<ActivateGameObject>("Dash Slash Void");
            nailArts.RemoveAction<ActivateGameObject>("G Slash Void");

#if DEBUG
            foreach (FsmState s in nailArts.FsmStates)
            {
                nailArts.InsertMethod(s.Name, 0, () => { Modding.Logger.Log($"[Nail Arts] - Entered {s.Name}"); });
                nailArts.AddAction(s.Name, new InvokeMethod(() => { Modding.Logger.Log($"[Nail Arts] - Left {s.Name}"); }));
            }
#endif

            //self.gameObject.scene.Log();

        }

        private void OnDestroy()
        {
            On.HeroController.Awake -= On_HeroController_Awake;
            On.HeroController.TakeDamage -= On_HeroController_TakeDamage;
            On.HeroController.AddHealth -= On_HeroController_AddHealth;
            On.HeroController.MaxHealth -= On_HeroController_MaxHealth;
            //On.CharmIconList.Awake -= CharmIconList_Awake;
            ModHooks.CharmUpdateHook -= ModHooks_CharmUpdate;
        }

        private static void Log(object message) => Modding.Logger.Log("[FiveKnights][Amulets] " + message);
    }
}
