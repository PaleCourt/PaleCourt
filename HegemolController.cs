using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Modding;
using ModCommon;
using System.Collections;
using System.Reflection;
using HutongGames.PlayMaker;
using HutongGames.PlayMaker.Actions;
using HutongGames.Utility;
using JetBrains.Annotations;
using ModCommon.Util;
using Object = UnityEngine.Object;
using Random = System.Random;

namespace FiveKnights
{
    public class HegemolController : MonoBehaviour
    {
        private const float LeftX = 61.0f;
        private const float RightX = 91.0f;
        private const float GroundX = 7.4f;

        private GameObject _ogrim;
        private GameObject _pv;

        private AudioSource _audio;
        private HealthManager _hm;
        private PlayMakerFSM _control;
        private PlayMakerFSM _dungDefender;
        private Random _random;

        private void Awake()
        {
            Log("Hegemol Awake");;

            _pv = Instantiate(FiveKnights.preloadedGO["PV"], Vector2.down * 10, Quaternion.identity);
            _pv.SetActive(true);
            PlayMakerFSM control = _pv.LocateMyFSM("Control");
            control.RemoveTransition("Pause", "Set Phase HP");

            _ogrim = FiveKnights.preloadedGO["WD"];
            _dungDefender = _ogrim.LocateMyFSM("Dung Defender");

            _random = new Random();
            
            _control = gameObject.LocateMyFSM("FalseyControl");
            _audio = GetComponent<AudioSource>();
            _hm = GetComponent<HealthManager>();

            On.EnemyHitEffectsArmoured.RecieveHitEffect += OnReceiveHitEffect;
            On.HealthManager.TakeDamage += OnTakeDamage;
        }

        private IEnumerator Start()
        {
            while (HeroController.instance == null) yield return null;
            
            _hm.hp = 1600;

            AssignFields();

            //Stuff for getting hegemol to work
            GetComponent<tk2dSprite>().GetCurrentSpriteDef().material.mainTexture = FiveKnights.SPRITES[0].texture;

            _control.Fsm.GetFsmFloat("Run Speed").Value = 15.0f;
            
            _control.RemoveAction<SpawnObjectFromGlobalPool>("S Attack Recover");
            _control.InsertCoroutine("S Attack Recover", 0, DungWave);
            _control.RemoveAction<AudioPlayerOneShot>("Voice?");
            _control.RemoveAction<AudioPlayerOneShot>("Voice? 2");
            
            _control.SetState("Init");
            yield return new WaitWhile(() => _control.ActiveStateName != "Dormant");
            _control.SendEvent("BATTLE START");
            while (true)
            {
                Log("hhh");
                if (_control.ActiveStateName.Contains("Hero Pos"))//JA Check Hero Pos
                {
                    float diff = HeroController.instance.transform.position.x - transform.position.x;
                    if (diff > 0) _control.SendEvent("RIGHT");
                    else _control.SendEvent("LEFT");
                }
                yield return new WaitForEndOfFrame();
            }
        }

        private void FixedUpdate()
        {
            
        }

        private void OnReceiveHitEffect(On.EnemyHitEffectsArmoured.orig_RecieveHitEffect orig, EnemyHitEffectsArmoured self, float attackDirection)
        {
            self.GetAttr<EnemyHitEffectsArmoured, SpriteFlash>("spriteFlash").flashFocusHeal();
            FSMUtility.SendEventToGameObject(gameObject, "DAMAGE FLASH", true);
            EnemyHitEffectsUninfected hitEffects = _pv.GetComponent<EnemyHitEffectsUninfected>();
            AudioSource audioPlayerPrefab = hitEffects.GetAttr<EnemyHitEffectsUninfected, AudioSource>("audioPlayerPrefab");
            AudioEvent enemyDamage = hitEffects.GetAttr<EnemyHitEffectsUninfected, AudioEvent>("enemyDamage");
            enemyDamage.SpawnAndPlayOneShot(audioPlayerPrefab, self.transform.position);
            self.SetAttr("didFireThisFrame", true);
            GameObject slashEffectGhost1 = hitEffects.GetAttr<EnemyHitEffectsUninfected, GameObject>("slashEffectGhost1");
            GameObject slashEffectGhost2 = hitEffects.GetAttr<EnemyHitEffectsUninfected, GameObject>("slashEffectGhost2");
            GameObject uninfectedHitPt = hitEffects.GetAttr<EnemyHitEffectsUninfected, GameObject>("uninfectedHitPt");
            Vector3 effectOrigin = hitEffects.GetAttr<EnemyHitEffectsUninfected, Vector3>("effectOrigin");
            GameObject go = uninfectedHitPt.Spawn(self.transform.position + effectOrigin);
            switch (DirectionUtils.GetCardinalDirection(attackDirection))
            {
                case 0:
                    go.transform.SetRotation2D(-45f);
                    FlingUtils.SpawnAndFling(new FlingUtils.Config()
                    {
                      Prefab = slashEffectGhost1,
                      AmountMin = 2,
                      AmountMax = 3,
                      SpeedMin = 20f,
                      SpeedMax = 35f,
                      AngleMin = -40f,
                      AngleMax = 40f,
                      OriginVariationX = 0.0f,
                      OriginVariationY = 0.0f
                    }, transform, effectOrigin);
                FlingUtils.SpawnAndFling(new FlingUtils.Config()
                {
                  Prefab = slashEffectGhost2,
                  AmountMin = 2,
                  AmountMax = 3,
                  SpeedMin = 10f,
                  SpeedMax = 35f,
                  AngleMin = -40f,
                  AngleMax = 40f,
                  OriginVariationX = 0.0f,
                  OriginVariationY = 0.0f
                }, transform, effectOrigin);
                break;
              case 1:
                go.transform.SetRotation2D(45f);
                FlingUtils.SpawnAndFling(new FlingUtils.Config()
                {
                  Prefab = slashEffectGhost1,
                  AmountMin = 2,
                  AmountMax = 3,
                  SpeedMin = 20f,
                  SpeedMax = 35f,
                  AngleMin = 50f,
                  AngleMax = 130f,
                  OriginVariationX = 0.0f,
                  OriginVariationY = 0.0f
                }, transform, effectOrigin);
                FlingUtils.SpawnAndFling(new FlingUtils.Config()
                {
                  Prefab = slashEffectGhost2,
                  AmountMin = 2,
                  AmountMax = 3,
                  SpeedMin = 10f,
                  SpeedMax = 35f,
                  AngleMin = 50f,
                  AngleMax = 130f,
                  OriginVariationX = 0.0f,
                  OriginVariationY = 0.0f
                }, transform, effectOrigin);
                break;
              case 2:
                go.transform.SetRotation2D(-225f);
                FlingUtils.SpawnAndFling(new FlingUtils.Config()
                {
                  Prefab = slashEffectGhost1,
                  AmountMin = 2,
                  AmountMax = 3,
                  SpeedMin = 20f,
                  SpeedMax = 35f,
                  AngleMin = 140f,
                  AngleMax = 220f,
                  OriginVariationX = 0.0f,
                  OriginVariationY = 0.0f
                }, transform, effectOrigin);
                FlingUtils.SpawnAndFling(new FlingUtils.Config()
                {
                  Prefab = slashEffectGhost2,
                  AmountMin = 2,
                  AmountMax = 3,
                  SpeedMin = 10f,
                  SpeedMax = 35f,
                  AngleMin = 140f,
                  AngleMax = 220f,
                  OriginVariationX = 0.0f,
                  OriginVariationY = 0.0f
                }, transform, effectOrigin);
                break;
              case 3:
                go.transform.SetRotation2D(225f);
                FlingUtils.SpawnAndFling(new FlingUtils.Config()
                {
                  Prefab = slashEffectGhost1,
                  AmountMin = 2,
                  AmountMax = 3,
                  SpeedMin = 20f,
                  SpeedMax = 35f,
                  AngleMin = 230f,
                  AngleMax = 310f,
                  OriginVariationX = 0.0f,
                  OriginVariationY = 0.0f
                }, transform, effectOrigin);
                FlingUtils.SpawnAndFling(new FlingUtils.Config()
                {
                  Prefab = slashEffectGhost2,
                  AmountMin = 2,
                  AmountMax = 3,
                  SpeedMin = 10f,
                  SpeedMax = 35f,
                  AngleMin = 230f,
                  AngleMax = 310f,
                  OriginVariationX = 0.0f,
                  OriginVariationY = 0.0f
                }, transform, effectOrigin);
                break;
            }
        }

        private void OnTakeDamage(On.HealthManager.orig_TakeDamage orig, HealthManager self, HitInstance hitInstance)
        {
            if (self.name.Contains("False Knight Dream"))
            {
                if (hitInstance.AttackType == AttackTypes.Nail)
                {
                    // Manually gain soul when striking Hegemol
                    int soulGain;
                    if (PlayerData.instance.MPCharge >= 99)
                    {
                        soulGain = 6;
                        if (PlayerData.instance.equippedCharm_20) soulGain += 2;
                        if (PlayerData.instance.equippedCharm_21) soulGain += 4;
                    }
                    else
                    {
                        soulGain = 11;
                        if (PlayerData.instance.equippedCharm_20) soulGain += 3;
                        if (PlayerData.instance.equippedCharm_21) soulGain += 8;
                    }
                    HeroController.instance.AddMPCharge(soulGain);
                }
            }

            orig(self, hitInstance);
        }
        
        private void AssignFields()
        {
            HealthManager ogrimHealth = _ogrim.GetComponent<HealthManager>();
            foreach (FieldInfo fi in typeof(HealthManager).GetFields(BindingFlags.Instance | BindingFlags.NonPublic)
                .Where(x => x.Name.Contains("Prefab")))
            {
                fi.SetValue(_hm, fi.GetValue(ogrimHealth));
            }
        }
        
        private IEnumerator DungWave()
        {
            Transform trans = transform;
            Vector2 pos = trans.position;
            float scaleX = trans.localScale.x;
            float xLeft = pos.x + 5 * scaleX - 2;
            float xRight = pos.x + 5 * scaleX + 2;
            while (xLeft >= LeftX || xRight <= RightX)
            {
                PlayAudioClip("Dung Pillar", 0.9f, 1.1f);
                
                GameObject dungPillarR = Instantiate(FiveKnights.preloadedGO["pillar"], new Vector2(xRight, 12.0f), Quaternion.identity);
                dungPillarR.SetActive(true);
                dungPillarR.AddComponent<DungPillar>();
                
                GameObject dungPillarL = Instantiate(FiveKnights.preloadedGO["pillar"], new Vector2(xLeft, 12.0f), Quaternion.identity);
                dungPillarL.SetActive(true);
                Vector3 pillarRScale = dungPillarR.transform.localScale;
                dungPillarL.transform.localScale = new Vector3(-pillarRScale.x, pillarRScale.y, pillarRScale.z);
                dungPillarL.AddComponent<DungPillar>();

                xLeft -= 2;
                xRight += 2;
                
                yield return new WaitForSeconds(0.1f);
            }
        }

        public void PlayAudioClip(string clipName, float pitchMin = 1.0f, float pitchMax = 1.0f, float time = 0.0f)
        {
            AudioClip GetAudioClip()
            {
                switch (clipName)
                {
                    case "Dung Pillar":
                        return (AudioClip) _dungDefender.GetAction<AudioPlayerOneShotSingle>("Pillar", 0).audioClip
                            .Value;
                    default:
                        return null;
                }
            }

            _audio.pitch = (float) (_random.NextDouble() * pitchMax) + pitchMin;
            _audio.time = time; 
            _audio.PlayOneShot(GetAudioClip());
        }
        
        private void OnDestroy()
        {
            On.EnemyHitEffectsArmoured.RecieveHitEffect -= OnReceiveHitEffect;
            On.HealthManager.TakeDamage -= OnTakeDamage;
        }

        private void Log(object o)
        {
            Modding.Logger.Log("[Hegemol] " + o);
        }
    }
}
