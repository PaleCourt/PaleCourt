using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Modding;
using ModCommon;
using System.Collections;
using System.Collections.Specialized;
using System.Reflection;
using HutongGames.PlayMaker;
using HutongGames.PlayMaker.Actions;
using HutongGames.Utility;
using JetBrains.Annotations;
using ModCommon.Util;
using Bounds = UnityEngine.Bounds;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

namespace FiveKnights
{
    public class HegemolController : MonoBehaviour
    {
        private const int Health = 2400;
        private const float LeftX = 61.0f;
        private const float RightX = 91.0f;
        private const float GroundX = 7.4f;
        private const float DigInWalkSpeed = 8.0f;

        private GameObject _mace;
        private GameObject _ogrim;
        private GameObject _pv;

        private AudioSource _audio;
        private BoxCollider2D _collider;
        private HealthManager _hm;
        private PlayMakerFSM _control;
        private PlayMakerFSM _dd;
        private Rigidbody2D _rb;
        private tk2dSprite _sprite;
        private tk2dSpriteAnimator _anim;

        private void Awake()
        {
            Log("Hegemol Awake");

            gameObject.name = "Hegemol";
            
            _pv = Instantiate(FiveKnights.preloadedGO["PV"], Vector2.down * 10, Quaternion.identity);
            _pv.SetActive(true);
            PlayMakerFSM control = _pv.LocateMyFSM("Control");
            control.RemoveTransition("Pause", "Set Phase HP");

            _ogrim = FiveKnights.preloadedGO["WD"];
            _dd = _ogrim.LocateMyFSM("Dung Defender");

            _control = gameObject.LocateMyFSM("FalseyControl");
            _anim = GetComponent<tk2dSpriteAnimator>();
            _collider = GetComponent<BoxCollider2D>();
            _sprite = GetComponent<tk2dSprite>();
            _audio = GetComponent<AudioSource>();
            _hm = GetComponent<HealthManager>();
            _rb = GetComponent<Rigidbody2D>();

            On.EnemyHitEffectsArmoured.RecieveHitEffect += OnReceiveHitEffect;
            On.HealthManager.TakeDamage += OnTakeDamage;
        }
        
        private IEnumerator Start()
        {
            while (HeroController.instance == null) yield return null;

            _hm.hp = Health;

            _mace = Instantiate(FiveKnights.preloadedGO["Mace"], transform);
            _mace.AddComponent<Mace>();
            _mace.SetActive(false);
            
            tk2dSpriteCollectionData fcCollectionData = _sprite.Collection;
            List<tk2dSpriteDefinition> fcSpriteDefs = fcCollectionData.spriteDefinitions.ToList();

            GameObject collectionPrefab = FiveKnights.preloadedGO["Hegemol Collection Prefab"];
            tk2dSpriteCollection collection = collectionPrefab.GetComponent<tk2dSpriteCollection>();

            foreach (tk2dSpriteDefinition def in collection.spriteCollection.spriteDefinitions)
            {
                def.material.shader = fcSpriteDefs[0].material.shader;
                fcSpriteDefs.Add(def);
            }

            fcCollectionData.spriteDefinitions = fcSpriteDefs.ToArray();
            
            List<tk2dSpriteAnimationClip> clips = _anim.Library.clips.ToList();

            foreach (var clip in clips)
            {
                Log("Clip: " + clip.name + " " + clip.wrapMode + " " + clip.loopStart);
            }
            
            clips = new List<tk2dSpriteAnimationClip>();
            
            GameObject animationPrefab = FiveKnights.preloadedGO["Hegemol Animation Prefab"];
            tk2dSpriteAnimation animation = animationPrefab.GetComponent<tk2dSpriteAnimation>();

            foreach (tk2dSpriteAnimationClip clip in animation.clips)
            {
                clips.Add(clip);
            }

            _anim.Library.clips = clips.ToArray();

            AssignFields();

            _control.Fsm.GetFsmFloat("Run Speed").Value = 20.0f;

            _control.RemoveAction<SpawnObjectFromGlobalPool>("S Attack Recover");
            _control.InsertCoroutine("S Attack Recover", 0, DungWave);
            _control.RemoveAction<AudioPlayerOneShot>("Voice?");
            _control.RemoveAction<AudioPlayerOneShot>("Voice? 2");
            _control.GetAction<SendRandomEvent>("Move Choice").AddToSendRandomEvent("Dig Antic", 1);
            _control.GetAction<SendRandomEvent>("Move Choice").AddToSendRandomEvent("Toss Antic", 1);
            _control.GetAction<SetGravity2dScale>("Start Fall", 12).gravityScale.Value = 3.0f;
            _control.InsertMethod("Start Fall", _control.GetState("Start Fall").Actions.Length, () => _anim.Play("Intro Fall"));
            _control.GetAction<Tk2dPlayAnimation>("State 1").clipName.Value = "Intro Land";
            _control.CreateState("Intro Greet");
            _control.InsertCoroutine("Intro Greet", 0, IntroGreet);
            _control.ChangeTransition("State 1", "FINISHED", "Intro Greet");
            // NOTE: Transition from Intro Greet does not actually work
            _control.AddTransition("Intro Greet", "FINISHED", "Idle");
            
            _collider.offset = new Vector2(0.0f, 2.6f);

            var hitterCol = gameObject.FindGameObjectInChildren("Hitter").GetComponent<BoxCollider2D>();
            hitterCol.offset = new Vector2(hitterCol.offset.x, 3.0f);
            gameObject.FindGameObjectInChildren("Hitter").AddComponent<DebugColliders>();
            gameObject.AddComponent<DebugColliders>();
            
            AddDig();
            AddGroundPunch();
            
            Log("Collider Offset: " + _collider.offset);
            
            yield return new WaitForSeconds(2.0f);

            _control.SetState("Init");
            yield return new WaitWhile(() => _control.ActiveStateName != "Dormant");

            gameObject.PrintSceneHierarchyTree();
            
            _control.SendEvent("BATTLE START");
            while (true)
            {
                if (_control.ActiveStateName.Contains("Hero Pos"))//JA Check Hero Pos
                {
                    float diff = HeroController.instance.transform.position.x - transform.position.x;
                    if (diff > 0) _control.SendEvent("RIGHT");
                    else _control.SendEvent("LEFT");
                }
                yield return new WaitForEndOfFrame();
            }
        }

        private IEnumerator IntroGreet()
        {
            float roarTime = 3.0f;

            _anim.Play("Intro Greet");

            yield return new WaitWhile(() => _anim.IsPlaying("Intro Greet"));

            Log("Getting Roar Emitter");
            PlayMakerFSM dd = FiveKnights.preloadedGO["WD"].LocateMyFSM("Dung Defender");
            GameObject roarEmitterObj = dd.GetAction<CreateObject>("Roar?", 10).gameObject.Value;
            Log("Spawning Roar Emitter");
            GameObject roarEmitter = Instantiate(roarEmitterObj, transform.position, Quaternion.identity);
            roarEmitter.SetActive(true);
            PlayMakerFSM emitter = roarEmitter.LocateMyFSM("emitter");
            emitter.SetState("Init");
            roarEmitter.GetComponent<DisableAfterTime>().waitTime = roarTime;

            //Log("Sending Camera Shake");
            //GameCameras.instance.cameraShakeFSM.SendEvent("MedRumble");

            PlayMakerFSM roarLock = HeroController.instance.gameObject.LocateMyFSM("Roar Lock");
            roarLock.Fsm.GetFsmGameObject("Roar Object").Value = gameObject;
            roarLock.SendEvent("ROAR ENTER");

            yield return new WaitForSeconds(roarTime);

            Destroy(roarEmitter);
            roarLock.SendEvent("ROAR EXIT");

            _control.SetState("Check");
        }
        
        private void AddDig()
        {
            string[] states =
            {
                "Dig Antic",
                "Dig In",
                "Dig Run",
                "Dig Out",
            };

            _control.CreateStates(states, "Idle");

            IEnumerator DigAntic()
            {
                Log("Dig Antic");
                _anim.Play("Dig Antic");
                
                yield return new WaitWhile(() => _anim.IsPlaying("Dig Antic"));
            }

            _control.InsertCoroutine("Dig Antic", 0, DigAntic);

            IEnumerator DigIn()
            {
                Log("Dig In");
                _anim.Play("Dig In");
                _audio.Play("Mace Slam");
                _rb.velocity = Vector2.right * transform.localScale.x * DigInWalkSpeed;
                
                yield return new WaitWhile(() => _anim.IsPlaying("Dig In"));
            }

            _control.InsertCoroutine("Dig In", 0, DigIn);

            IEnumerator DigRun()
            {
                Log("Dig Run");
                _anim.Play("Dig Run");

                yield return new WaitForSeconds(1.0f);
            }
            
            _control.InsertCoroutine("Dig Run", 0, DigRun);
            
            IEnumerator DigOut()
            {
                Log("Dig Out");
                _anim.Play("Dig Out");
                _audio.Play("Mace Swing");
                _rb.velocity = Vector2.zero;

                Vector2 pos = transform.position + transform.localScale.x * Vector3.right * 5.0f + Vector3.down * 5.0f;
                float valMin = 15.0f;
                float valMax = 40.0f;

                GameObject dungBall1 = Instantiate(FiveKnights.preloadedGO["ball"], pos, Quaternion.identity);
                dungBall1.SetActive(true);
                dungBall1.GetComponent<Rigidbody2D>().velocity = new Vector2(transform.localScale.x * Random.Range(valMin, valMax), 2 * Random.Range(valMin, valMax));

                GameObject dungBall2 = Instantiate(FiveKnights.preloadedGO["ball"], pos, Quaternion.identity);
                dungBall2.SetActive(true);
                dungBall2.GetComponent<Rigidbody2D>().velocity = new Vector2(transform.localScale.x * Random.Range(valMin, valMax), 2 * Random.Range(valMin, valMax));
                
                GameObject dungBall3 = Instantiate(FiveKnights.preloadedGO["ball"], pos, Quaternion.identity);
                dungBall3.SetActive(true);
                dungBall3.GetComponent<Rigidbody2D>().velocity = new Vector2(transform.localScale.x * Random.Range(valMin, valMax), 2 * Random.Range(valMin, valMax));

                GameObject hitter = Instantiate(new GameObject("Hitter"), transform);
                hitter.SetActive(true);
                hitter.layer = 17;
                PolygonCollider2D hitterPoly = hitter.AddComponent<PolygonCollider2D>();
                hitterPoly.isTrigger = true;
                hitterPoly.points = new []
                {
                    new Vector2(3.66f, -1.23f),
                    new Vector2(0.3f, 2.39f),
                    new Vector2(0.1f, -4.27f),
                    new Vector2(4.51f, -3.91f),
                    new Vector2(6.13f, -2.88f),
                    new Vector2(5.26f, -1.39f),
                    new Vector2(4.52f, -1.13f),
                };

                hitter.AddComponent<DamageHero>().damageDealt = 2;
                hitter.AddComponent<DebugColliders>();
                
                GameCameras.instance.cameraShakeFSM.SendEvent("AverageShake");

                yield return new WaitForSeconds(1.0f / 12);

                hitterPoly.points = new[]
                {
                    new Vector2(1.76f, 1.96f),
                    new Vector2(2.48f, -1.69f),
                    new Vector2(-3.35f, 1.49f),
                    new Vector2(-4.19f, 0.95f),
                    new Vector2(-5.07f, 1.19f),
                    new Vector2(-5.59f, 2.44f),
                    new Vector2(-3.63f, 3.76f),
                    new Vector2(-1.33f, 3.95f),
                    new Vector2(1.03f, 3.14f),
                };

                yield return new WaitForSeconds(1.0f / 12);

                hitterPoly.points = new[]
                {
                    new Vector2(-6.33f, 1.33f),
                    new Vector2(-4.91f, 1.56f),
                    new Vector2(2.17f, -0.96f),
                    new Vector2(2.12f, -1.21f),
                    new Vector2(-4.28f, -0.73f),
                    new Vector2(-4.77f, -1.91f),
                    new Vector2(-6.21f, -1.57f),
                    new Vector2(-6.63f, -0.8f),
                };
                
                yield return new WaitWhile(() => _anim.IsPlaying("Dig Out"));

                Destroy(hitter);
            }
            
            _control.InsertCoroutine("Dig Out", 0, DigOut);
        }

        private void AddGroundPunch()
        {
            string[] states =
            {
                "Toss Antic",
                "Toss",
                "Punch Antic",
                "Punching",
            };

            _control.CreateStates(states, "Idle");

            IEnumerator TossAntic()
            {
                _anim.Play("Toss Antic");
                _rb.velocity = Vector2.zero;
                
                yield return new WaitWhile(() => _anim.IsPlaying("Toss Antic"));
            }

            _control.InsertCoroutine("Toss Antic", 0, TossAntic);
            
            IEnumerator Toss()
            {
                _anim.Play("Toss");

                yield return new WaitWhile(() => _anim.IsPlaying("Toss"));
            }

            _control.InsertCoroutine("Toss", 0, Toss);
            
            IEnumerator PunchAntic()
            {
                _anim.Play("Punch Antic");
                _mace.SetActive(true);   
                
                yield return new WaitWhile(() => _anim.IsPlaying("Punch Antic"));
            }

            _control.InsertCoroutine("Punch Antic", 0, PunchAntic);
            
            IEnumerator Punching()
            {
                _anim.Play("Punching");
                
                yield return new WaitForSeconds(5.0f);
            }
            
            _control.InsertCoroutine("Punching", 0, Punching);
        }

        private void OnReceiveHitEffect(On.EnemyHitEffectsArmoured.orig_RecieveHitEffect orig, EnemyHitEffectsArmoured self, float attackDirection)
        {
            self.GetAttr<EnemyHitEffectsArmoured, SpriteFlash>("spriteFlash").flashFocusHeal();
            FSMUtility.SendEventToGameObject(gameObject, "DAMAGE FLASH", true);
            EnemyHitEffectsUninfected hitEffects = _pv.GetComponent<EnemyHitEffectsUninfected>();
            AudioSource audioPlayerPrefab = hitEffects.GetAttr<EnemyHitEffectsUninfected, AudioSource>("audioPlayerPrefab");
            AudioEvent enemyDamage = GetComponent<EnemyHitEffectsArmoured>().GetAttr<EnemyHitEffectsArmoured, AudioEvent>("enemyDamage");
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

            if (_hm.hp <= 0)
            {
                HegemolDeath();
            }
        }

        private void HegemolDeath()
        {
            Log("Hegemol Death");
            Destroy(gameObject);
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
            float pillarSpacing = 2;
            while (xLeft >= LeftX || xRight <= RightX)
            {
                _audio.Play("Dung Pillar", 0.9f, 1.1f);
                
                GameObject dungPillarR = Instantiate(FiveKnights.preloadedGO["pillar"], new Vector2(xRight, 12.0f), Quaternion.identity);
                dungPillarR.SetActive(true);
                dungPillarR.AddComponent<DungPillar>();
                
                GameObject dungPillarL = Instantiate(FiveKnights.preloadedGO["pillar"], new Vector2(xLeft, 12.0f), Quaternion.identity);
                dungPillarL.SetActive(true);
                Vector3 pillarRScale = dungPillarR.transform.localScale;
                dungPillarL.transform.localScale = new Vector3(-pillarRScale.x, pillarRScale.y, pillarRScale.z);
                dungPillarL.AddComponent<DungPillar>();

                xLeft -= pillarSpacing;
                xRight += pillarSpacing;
                
                yield return new WaitForSeconds(0.1f);
            }
        }
        
        private void OnDestroy()
        {
            On.EnemyHitEffectsArmoured.RecieveHitEffect -= OnReceiveHitEffect;
            On.HealthManager.TakeDamage -= OnTakeDamage;
        }

        private void Log(object o)
        {
            Modding.Logger.Log("[Hegemol Controller] " + o);
        }
    }
}
