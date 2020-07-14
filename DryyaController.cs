using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Random = UnityEngine.Random;
using System.Reflection;
using GlobalEnums;
using HutongGames.PlayMaker;
using HutongGames.PlayMaker.Actions;
using IL.InControl.NativeProfile;
using ModCommon;
using ModCommon.Util;
using Modding;
using UnityEngine;

namespace FiveKnights
{
    public class DryyaController : MonoBehaviour
    {
        private const float GroundY = 10.5f; //Message to Jngo: This was 8.5 but 8.5 isn't high enough in GG WD arena
        private const float LeftY = 61.0f;
        private const float RightY = 91.0f;
        private const float LeftX = 61.0f;
        private const float RightX = 91.0f;
        private const float DashSpeed = 90.0f;
        private const float DiveJumpSpeed = 50.0f;
        private const float DiveSpeed = 80.0f;
        private const float EvadeSpeed = 40.0f;
        private const float SlashSpeed = 50.0f;
        private const float WalkSpeed = 15.0f;

        private int _hp = 1650;

        private PlayMakerFSM _pvControl;
        private PlayMakerFSM _kinControl;
        private PlayMakerFSM _mageLord;
        private PlayMakerFSM _control;

        private GameObject _corpse;
        private GameObject _diveShockwave;
        private GameObject _elegyBeam1;
        private GameObject _elegyBeam2;
        private GameObject _ogrim;
        private GameObject _slash1Collider1;
        private GameObject _slash1Collider2;
        private GameObject _slash2Collider1;
        private GameObject _slash2Collider2;
        private GameObject _slash3Collider1;
        private GameObject _slash3Collider2;
        private GameObject _cheekySlashCollider1;
        private GameObject _cheekySlashCollider2;
        private GameObject _cheekySlashCollider3;
        private List<GameObject> _slashes;
        private GameObject _stabFlash;
        
        private string[] _dreamNailDialogue =
        {
            "DRYYA_DIALOG_1",
            "DRYYA_DIALOG_2",
            "DRYYA_DIALOG_3",
            "DRYYA_DIALOG_4",
            "DRYYA_DIALOG_5",
        };
        
        private float AnimFPS;
        
        private void Awake()
        {
            Log("Dryya Awake");
            
            GameObject go = gameObject;
            go.SetActive(true);
            go.layer = 11;

            _corpse = gameObject.FindGameObjectInChildren("Corpse");
            _diveShockwave = gameObject.FindGameObjectInChildren("Dive Shockwave");
            _elegyBeam1 = gameObject.FindGameObjectInChildren("Elegy Beam 1");
            _elegyBeam2 = gameObject.FindGameObjectInChildren("Elegy Beam 2");
            _slash1Collider1 = gameObject.FindGameObjectInChildren("Slash 1 Collider 1");
            _slash1Collider2 = gameObject.FindGameObjectInChildren("Slash 1 Collider 2");
            _slash2Collider1 = gameObject.FindGameObjectInChildren("Slash 2 Collider 1");
            _slash2Collider2 = gameObject.FindGameObjectInChildren("Slash 2 Collider 2");
            _slash3Collider1 = gameObject.FindGameObjectInChildren("Slash 3 Collider 1");
            _slash3Collider2 = gameObject.FindGameObjectInChildren("Slash 3 Collider 2");
            _cheekySlashCollider1 = gameObject.FindGameObjectInChildren("Slash Collider 1");
            _cheekySlashCollider2 = gameObject.FindGameObjectInChildren("Slash Collider 2");
            _cheekySlashCollider3 = gameObject.FindGameObjectInChildren("Slash Collider 3");
            _slashes = new List<GameObject>
            {
                _slash1Collider1,
                _slash1Collider2,
                _slash2Collider1,
                _slash2Collider2,
                _slash3Collider1,
                _slash3Collider2,
                _cheekySlashCollider1,
                _cheekySlashCollider2,
                _cheekySlashCollider3,
            };
            
            _stabFlash = gameObject.FindGameObjectInChildren("Stab Flash");

            Log("Getting FSMs");
            _pvControl = FiveKnights.preloadedGO["PV"].LocateMyFSM("Control");
            _kinControl = FiveKnights.preloadedGO["Kin"].LocateMyFSM("IK Control");
            _mageLord = FiveKnights.preloadedGO["Mage"].LocateMyFSM("Mage Lord");
            _ogrim = FiveKnights.preloadedGO["WD"];
            _control = gameObject.LocateMyFSM("Control");

            _control.SetState("Init");
            _control.Fsm.GetFsmGameObject("Hero").Value = HeroController.instance.gameObject;
            Log("Hero name: " + _control.Fsm.GetFsmGameObject("Hero").Value.name);

            _control.InsertMethod("Phase Check", 0, () => _control.Fsm.GetFsmInt("HP").Value = _hm.hp);

            _control.InsertMethod("Counter Stance", _control.GetState("Counter Stance").Actions.Length, () =>
            {
                _hm.IsInvincible = true;
                if (transform.localScale.x == 1)
                    _hm.InvincibleFromDirection = 8;
                else if (transform.localScale.x == -1)
                    _hm.InvincibleFromDirection = 9;
                
                _audio.Play("Counter");
                _spriteFlash.flashFocusHeal();

                Vector2 fxPos = transform.position + Vector3.right * 1.3f * -transform.localScale.x + Vector3.up * 0.5f;
                Quaternion fxRot = Quaternion.Euler(0, 0, -transform.localScale.x * -60);
                GameObject counterFX = Instantiate(FiveKnights.preloadedGO["CounterFX"], fxPos, fxRot);
                counterFX.SetActive(true);
            });
            _control.InsertMethod("Counter End", 0, () => _hm.IsInvincible = false);
            _control.InsertMethod("Counter Slash End", 0, () => _hm.IsInvincible = false);
            
            _control.InsertMethod("Slash 1 Collider 1", 0, () => _audio.Play("Slash"));
            _control.InsertMethod("Slash 2 Collider 1", 0, () => _audio.Play("Slash"));
            _control.InsertMethod("Slash 3 Collider 1", 0, () => _audio.Play("Slash"));

            _control.InsertMethod("Stab", 0, () => _audio.Play("Dash"));

            _control.InsertCoroutine("Countered", 0, () => GameManager.instance.FreezeMoment(0.04f, 0.35f, 0.04f, 0f));
            _control.InsertMethod("Countered", 0, () => _audio.Play("Counter"));
            _control.InsertMethod("Counter Slash", 0, () => _audio.Play("Slash"));

            _control.InsertMethod("Dive", 0, () => _audio.Play("Dive"));
            _control.InsertMethod("Dive Land Light", 0, () => _audio.Play("Light Land"));
            _control.InsertMethod("Dive Land Heavy", 0, () => _audio.Play("Heavy Land"));
            _control.InsertMethod("Dive Land Heavy", 0, () => SpawnShockwaves(2, 50, 1));

            _control.InsertMethod("Cheeky Collider 1", 0, () => _audio.Play("Slash", 0.85f, 1.15f));
            
            Log("Disabling Burrow Effect");
            GameObject.Find("Burrow Effect").SetActive(false);
            GameCameras.instance.cameraShakeFSM.FsmVariables.FindFsmBool("RumblingMed").Value = false;

            Log("Getting Components");
            GetComponents();
            Log("Adding Components");
            AddComponents();

            Log("Adding Hooks");
            _hm.OnDeath += DeathHandler;
            On.EnemyDreamnailReaction.RecieveDreamImpact += OnReceiveDreamImpact;
            On.HealthManager.TakeDamage += OnTakeDamage;
        }

        private IEnumerator Start()
        {
            while (HeroController.instance == null) yield return null;

            Log("Dryya Start");

            AnimFPS = 1.0f / _anim.ClipFps;
            
            _dreamNailEffect = _ogrim.GetComponent<EnemyDreamnailReaction>().GetAttr<EnemyDreamnailReaction, GameObject>("dreamImpactPrefab");

            _moves = new List<Action>
            {
                DryyaCounter,
                DryyaStab,
                DryyaDive,
                DryyaElegy,
                DryyaTripleSlash,
            };

            _repeats = new Dictionary<Action, int>
            {
                [DryyaCounter] = 0,
                [DryyaStab] = 0,
                [DryyaDive] = 0,
                [DryyaElegy] = 0,
                [DryyaTripleSlash] = 0,
            };
            
            _maxRepeats = new Dictionary<Action, int>
            {
                [DryyaCounter] = 1,
                [DryyaStab] = 2,
                [DryyaDive] = 1,
                [DryyaElegy] = 1,
                [DryyaTripleSlash] = 2,
            };

            AssignFields();

            Log("Printing Out Dryya");
            gameObject.PrintSceneHierarchyTree();

            //DryyaIntro();
        }

        private void FixedUpdate()
        {
            TestWallCollisions();
            Log("Active State Name: " + _control.ActiveStateName);
        }

        private void DeathHandler()
        {
            _corpse.SetActive(true);
        }

        private GameObject _dreamNailEffect;
        private void OnReceiveDreamImpact(On.EnemyDreamnailReaction.orig_RecieveDreamImpact orig, EnemyDreamnailReaction self)
        {
            if (self.name.Contains("Dryya"))
            {
                //Instantiate(_dreamNailEffect, transform.position, Quaternion.identity);
                _dreamNailReaction.SetConvoTitle(_dreamNailDialogue[Random.Range(0, _dreamNailDialogue.Length)]);
            }

            orig(self);
        }

        // Put OnBlockedHit outside of DryyaCounter so that the event handler can be unhooked in OnDestroy if the scene changes mid-counter
        private void OnBlockedHit(On.HealthManager.orig_Hit orig, HealthManager self, HitInstance hitInstance)
        {
            if (self.name.Contains("Dryya"))
            { 
                if (hitInstance.Direction == 0 && transform.localScale.x == 1 || hitInstance.Direction == 180 && transform.localScale.x == -1)
                {
                    StartCoroutine(GameManager.instance.FreezeMoment(0.04f, 0.35f, 0.04f, 0f));
                    // Prevent code block from running every frame
                    if (!_blockedHit)
                    {
                        _blockedHit = true;
                        Log("Blocked Hit");
                        StopCoroutine(_counterRoutine);
                        StartCoroutine(Countered());
                    }    
                }
                orig(self, hitInstance);
            }
        }

        private void OnTakeDamage(On.HealthManager.orig_TakeDamage orig, HealthManager self, HitInstance hitInstance)
        {
            if (self.gameObject.name.Contains("Dryya"))
                _spriteFlash.flashFocusHeal();

            orig(self, hitInstance);
        }
        
        private tk2dSpriteAnimator _anim;
        private Rigidbody2D _rb;
        private tk2dSprite _sprite;
        private BoxCollider2D _collider;
        private void GetComponents()
        {
            Log("Getting tk2dSpriteAnimator");
            _anim = GetComponent<tk2dSpriteAnimator>();
            Log("Getting BoxCollider2D");
            _collider = GetComponent<BoxCollider2D>();
            Log("Getting Rigidbody2D");
            _rb = GetComponent<Rigidbody2D>();
            Log("Getting tk2dSprite");
            _sprite = GetComponent<tk2dSprite>();

            Log("Getting Ogrim Material");
            Shader shader = _ogrim.GetComponent<tk2dSprite>().Collection.spriteDefinitions[0].material.shader;
            Log("Ogrim Shader: " + shader.name);

            Log("Printing out Sprite Definitions");
            foreach (var sprite in _sprite.Collection.spriteDefinitions)
            {
                Log("Sprite Definition Name: " + sprite.name);
            }
    
            foreach (var anim in _anim.Library.clips)
            {
                Log("Anim Name: " + anim.name);
            }
            
            Log("Changing Sprite definition materials");
            foreach (tk2dSpriteDefinition spriteDef in _sprite.Collection.spriteDefinitions)
                spriteDef.material.shader = shader;

            Log("Getting Shockwave Sprite");
            tk2dSprite shockwaveSprite = _diveShockwave.GetComponent<tk2dSprite>();
            foreach (tk2dSpriteDefinition spriteDef in shockwaveSprite.Collection.spriteDefinitions)
                spriteDef.material.shader = Shader.Find("tk2d/BlendVertexColor");

            tk2dSprite beam1Sprite = _elegyBeam1.GetComponent<tk2dSprite>();
            foreach (tk2dSpriteDefinition spriteDef in beam1Sprite.Collection.spriteDefinitions)
                spriteDef.material.shader = Shader.Find("tk2d/BlendVertexColor");
            
            tk2dSprite beam2Sprite = _elegyBeam2.GetComponent<tk2dSprite>();
            foreach (tk2dSpriteDefinition spriteDef in beam2Sprite.Collection.spriteDefinitions)
                spriteDef.material.shader = Shader.Find("tk2d/BlendVertexColor");
            
            Log("Getting Flash Sprite");
            tk2dSprite flashSprite = _stabFlash.GetComponent<tk2dSprite>();
            foreach (tk2dSpriteDefinition spriteDef in flashSprite.Collection.spriteDefinitions)
                spriteDef.material.shader = Shader.Find("tk2d/BlendVertexColor");
        }

        private AudioSource _audio;
        private DamageHero _damageHero;
        private EnemyDeathEffectsUninfected _deathEffects;
        private EnemyDreamnailReaction _dreamNailReaction;
        private EnemyHitEffectsUninfected _hitEffects;
        private HealthManager _hm;
        private SpriteFlash _spriteFlash;
        private void AddComponents()
        {
            _audio = gameObject.AddComponent<AudioSource>();

            _deathEffects = gameObject.AddComponent<EnemyDeathEffectsUninfected>();

            _dreamNailReaction = gameObject.AddComponent<EnemyDreamnailReaction>();
            _dreamNailReaction.enabled = true;
            _dreamNailReaction.SetConvoTitle(_dreamNailDialogue[Random.Range(0, _dreamNailDialogue.Length)]);
            
            _hitEffects = gameObject.AddComponent<EnemyHitEffectsUninfected>();
            _hitEffects.enabled = true;

            _hm = gameObject.AddComponent<HealthManager>();
            _hm.enabled = true;
            _hm.hp = _hp;

            _damageHero = gameObject.AddComponent<DamageHero>();
            _damageHero.enabled = true;

            _spriteFlash = gameObject.AddComponent<SpriteFlash>();

            _diveShockwave.AddComponent<DeactivateAfter2dtkAnimation>();
            for (int i = 1; i <= 3; i++)
                _diveShockwave.FindGameObjectInChildren("Collider " + i).AddComponent<DamageHero>();

            _elegyBeam1.AddComponent<ElegyBeam>().parent = gameObject;
            _elegyBeam1.AddComponent<DamageHero>();
            
            _elegyBeam2.AddComponent<ElegyBeam>().parent = gameObject;
            _elegyBeam2.AddComponent<DamageHero>();

            PlayMakerFSM nailClashTink = FiveKnights.preloadedGO["Slash"].LocateMyFSM("nail_clash_tink");
            
            foreach (GameObject slash in _slashes)
            {
                slash.AddComponent<DamageHero>();
                PlayMakerFSM pfsm = slash.AddComponent<PlayMakerFSM>();
                foreach (FieldInfo fi in typeof(PlayMakerFSM).GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public))
                    fi.SetValue(pfsm, fi.GetValue(nailClashTink));
            }

            _stabFlash.AddComponent<DeactivateAfter2dtkAnimation>();
        }

        private void AssignFields()
        {
            EnemyDeathEffectsUninfected ogrimDeathEffects = _ogrim.GetComponent<EnemyDeathEffectsUninfected>();
            foreach (FieldInfo fi in typeof(EnemyDeathEffectsUninfected).GetFields(BindingFlags.Instance | BindingFlags.Public))
                fi.SetValue(_deathEffects, fi.GetValue(ogrimDeathEffects));
            
            EnemyHitEffectsUninfected ogrimHitEffects = _ogrim.GetComponent<EnemyHitEffectsUninfected>();
            foreach (FieldInfo fi in typeof(EnemyHitEffectsUninfected).GetFields(BindingFlags.Instance | BindingFlags.Public))
                fi.SetValue(_hitEffects, fi.GetValue(ogrimHitEffects));

            HealthManager ogrimHealth = _ogrim.GetComponent<HealthManager>();
            foreach (FieldInfo fi in typeof(HealthManager).GetFields(BindingFlags.Instance | BindingFlags.NonPublic).Where(x => x.Name.Contains("Prefab")))
                fi.SetValue(_hm, fi.GetValue(ogrimHealth));
        }

        private Action _previousMove;
        private Action _nextMove;
        private List<Action> _moves;
        private Dictionary<Action, int> _repeats;
        private Dictionary<Action, int> _maxRepeats;
        private IEnumerator IdleAndChooseNextAttack()
        {
            _anim.Play("Idle");
            _rb.velocity = Vector2.zero;

            if (_previousMove != DryyaTurn && _previousMove != DryyaWalk && _previousMove != DryyaEvade)
            {
                float waitMin = 0.01f;
                float waitMax = 0.15f;
                float waitTime = Random.Range(waitMin, waitMax);
            
                yield return new WaitForSeconds(waitTime);    
            }

            yield return null;

            if (_nextMove != null) _previousMove = _nextMove;
            int index = Random.Range(0, _moves.Count);
            _nextMove = _moves[index];
            
            // Make sure moves don't occur more than its respective max number of repeats in a row
            while (_repeats[_nextMove] >= _maxRepeats[_nextMove])
            {
                index = Random.Range(0, _moves.Count);
                _nextMove = _moves[index];
            }

            Vector2 pos = transform.position;
            Vector2 heroPos = HeroController.instance.transform.position;
            float evadeRange = 4.0f;
            if (Mathf.Sqrt(Mathf.Pow(pos.x - heroPos.x, 2) + Mathf.Pow(pos.y - heroPos.y, 2)) < evadeRange)
            {
                int randNum = Random.Range(0, 10);
                int threshold = 7;
                if (randNum < threshold)
                {
                    if (transform.localScale.x == -1 && pos.x - LeftX > 4.0f || (transform.localScale.x == 1 && RightX - pos.x > 4.0f))
                    {
                        if (_previousMove != DryyaWalk) _nextMove = DryyaEvade;
                    }
                }
            }
            else if (Mathf.Abs(pos.x - heroPos.x) <= 2.0f && heroPos.y - pos.y > 2.0f)
            {
                int randNum = Random.Range(0, 10);
                int threshold = 5;
                if (randNum < threshold)
                {
                    // Pogo Punishment
                }
            }

            // Walk if Knight is out of walk range
            float walkRange = 15.0f;
            if (Mathf.Abs(heroPos.x - pos.x) > walkRange)
            {
                if (_nextMove != DryyaDive && _nextMove != DryyaElegy) _nextMove = DryyaWalk;
            }
            
            // Turn if facing opposite of direction to Knight
            if (heroPos.x - pos.x < 0 && transform.localScale.x == -1 || heroPos.x - pos.x > 0 && transform.localScale.x == 1)
            {
                _nextMove = DryyaTurn;
            }

            // Increment or reset move repeats dictionary
            if (_moves.Contains(_nextMove))
            {
                foreach (Action move in _moves)
                {
                    if (move == _nextMove)
                    {
                        _repeats[move]++;
                    }        
                    else
                    {
                        _repeats[move] = 0;
                    }
                }
            }

            Log("Next Move: " + _nextMove.Method.Name);
            _nextMove.Invoke();
        }

        private void DryyaIntro()
        {
            IEnumerator IntroFalling()
            {
                Log("Intro Fall");
                _anim.Play("Intro Fall");
                int fallingSpeed = 50;
                _rb.velocity = new Vector2(-1, -1) * fallingSpeed;
                while (!IsGrounded())
                {
                    Log("Not Grounded");
                    yield return null;
                }

                yield return null;

                StartCoroutine(IntroLand());
            }

            IEnumerator IntroLand()
            {
                Log("Intro Land");
                SnapToGround();
                _anim.Play("Intro Land");
                _rb.velocity = Vector2.zero;
                yield return new WaitForSeconds(4 * AnimFPS);

                Log("Starting Backstep 1 Coroutine");
                StartCoroutine(IntroBackstep1());
            }

            IEnumerator IntroBackstep1()
            {
                Log("Intro Backstep 1");
                _anim.Play("Backstep 1");
                _rb.velocity = new Vector2(5, 0);
                yield return new WaitForSeconds(3 * AnimFPS);
                _rb.velocity = Vector2.zero;
                yield return new WaitForSeconds(AnimFPS);

                StartCoroutine(IntroBackstep2());
            }

            IEnumerator IntroBackstep2()
            {
                Log("Intro Backstep 2");
                _anim.Play("Backstep 2");
                _rb.velocity = new Vector2(8, 0);
                yield return new WaitForSeconds(3 * AnimFPS);

                StartCoroutine(IdleAndChooseNextAttack());
            }

            StartCoroutine(IntroFalling());
        }

        private void DryyaTurn()
        {
            IEnumerator Turn()
            {
                Log("Turn");
                _anim.Play("Turn");
                yield return new WaitForSeconds(AnimFPS);
                Flip();
                
                StartCoroutine(IdleAndChooseNextAttack());
            }
            
            StartCoroutine(Turn());
        }
        
        private void DryyaWalk()
        {
            IEnumerator IdleToWalk()
            {
                Log("Idle to Walk");
                _anim.Play("Idle to Walk");
                for (float i = 0; i < 4; i++)
                {
                    _rb.velocity = new Vector2(-transform.localScale.x * WalkSpeed * (i / 4), 0);
                    yield return new WaitForSeconds(AnimFPS);   
                }

                StartCoroutine(Walking());
            }

            IEnumerator Walking()
            {
                Log("Walk");
                _anim.Play("Walk");
                _rb.velocity = new Vector2(-transform.localScale.x * WalkSpeed, 0);
                float heroX = HeroController.instance.transform.position.x;
                float posX = transform.position.x;
                float dx = Mathf.Abs(heroX - posX); 
                while (dx >= 5.0f)
                {
                    heroX = HeroController.instance.transform.position.x;
                    posX = transform.position.x;
                    dx = Mathf.Abs(heroX - posX);
                    yield return null;
                }

                StartCoroutine(IdleAndChooseNextAttack());
            }

            StartCoroutine(IdleToWalk());
        }

        private Coroutine _counterRoutine;
        private bool _blockedHit;
        private void DryyaCounter()
        {

            IEnumerator CounterAntic()
            {
                _anim.Play("Counter Antic");
                _rb.velocity = Vector2.zero;
                yield return new WaitForSeconds(4 * AnimFPS);

                _counterRoutine = StartCoroutine(Countering());
            }

            IEnumerator Countering()
            {
                Log("Countering");
                // Yeeted from Pure Vessel's Control FSM
                _hm.IsInvincible = true;
                if (transform.localScale.x == 1)
                {
                    _hm.InvincibleFromDirection = 8;
                }
                else if (transform.localScale.x == -1)
                {
                    _hm.InvincibleFromDirection = 9;
                }
                
                _anim.Play("Countering");

                _blockedHit = false;
                On.HealthManager.Hit += OnBlockedHit;
                _audio.Play("Counter");
                _spriteFlash.flashFocusHeal();

                Vector2 fxPos = transform.position + Vector3.right * 1.3f * -transform.localScale.x + Vector3.up * 0.5f;
                Quaternion fxRot = Quaternion.Euler(0, 0, -transform.localScale.x * -60);
                GameObject counterFX = Instantiate(FiveKnights.preloadedGO["CounterFX"], fxPos, fxRot);
                counterFX.SetActive(true);


                yield return new WaitForSeconds(0.75f);

                _counterRoutine = StartCoroutine(CounterEnd());
            }

            IEnumerator CounterEnd()
            {
                _anim.Play("Counter End");
                _hm.IsInvincible = false;
                
                On.HealthManager.Hit -= OnBlockedHit;
                
                yield return new WaitForSeconds(AnimFPS);
                
                StartCoroutine(IdleAndChooseNextAttack());
            }

            _counterRoutine = StartCoroutine(CounterAntic());
        }
        
        // Put these IEnumerators outside so that they can be started in OnBlockedHit
        private IEnumerator Countered()
        {
            _anim.Play("Countered");
            _audio.Play("Counter");
            On.HealthManager.Hit -= OnBlockedHit;
            
            yield return new WaitForSeconds(AnimFPS);

            StartCoroutine(CounterAttack());
        }

        private IEnumerator CounterAttack()
        {
            Log("Counter Attack");
            _hm.IsInvincible = false;
            InstantlyFaceHero();
            _anim.Play("Slash 1 Antic");
            
            yield return new WaitWhile(() => _anim.IsPlaying("Slash 1 Antic"));
            
            _anim.Play("Slash 1");
            _audio.Play("Slash", 0.85f, 1.15f);
            _rb.velocity = new Vector2(-transform.localScale.x * SlashSpeed, 0);
            _slash1Collider1.SetActive(true);

            yield return new WaitForSeconds(AnimFPS);
            
            _rb.velocity = Vector2.zero;
            _slash1Collider1.SetActive(false);
            _slash1Collider2.SetActive(true);

            yield return new WaitForSeconds(AnimFPS);

            _slash1Collider2.SetActive(false);
            
            yield return new WaitWhile(() => _anim.IsPlaying("Slash 1"));
            
            StartCoroutine(IdleAndChooseNextAttack());
        }

        private void DryyaStab()
        {
            IEnumerator StabAntic()
            {
                Log("Stab Antic");
                _rb.velocity = Vector2.zero;
                _anim.Play("Stab Antic");
                
                yield return new WaitForSeconds(0.5f);

                StartCoroutine(Stab(0.15f));
            }

            IEnumerator Stab(float dashTime)
            {
                Log("Stab");
                _anim.Play("Stab");
                _stabFlash.SetActive(true);

                _audio.Play("Dash");
                _rb.velocity = Vector2.right * -transform.localScale.x * DashSpeed;
                
                yield return new WaitForSeconds(dashTime);

                StartCoroutine(StabEnd());
            }

            IEnumerator StabEnd()
            {
                Log("Stab End");
                _anim.Play("Stab End");
                _rb.velocity = Vector2.zero;

                yield return new WaitWhile(() => _anim.IsPlaying("Stab End"));

                StartCoroutine(IdleAndChooseNextAttack());
            }

            StartCoroutine(StabAntic());
        }

        private void DryyaDive()
        {
            IEnumerator DiveJump()
            {
                Log("Dive Jump");
                _anim.Play("Dive Jump");
                _rb.velocity = Vector2.zero;
                yield return new WaitForSeconds(3 * AnimFPS);

                StartCoroutine(DiveSpin());
            }
            IEnumerator DiveSpin()
            {
                Log("Dive Spin");
                _anim.Play("Dive Spin");
                float heroX = HeroController.instance.transform.position.x;
                float posX = transform.position.x;
                _rb.velocity = new Vector2((heroX - posX) / (2 * AnimFPS), DiveJumpSpeed);
                
                yield return new WaitForSeconds(2 * AnimFPS);
                
                StartCoroutine(DiveAntic());
            }

            IEnumerator DiveAntic()
            {
                Log("Dive Antic");
                _anim.Play("Dive Antic");
                _rb.velocity = Vector2.zero;

                yield return new WaitWhile(() => _anim.IsPlaying("Dive Antic"));

                StartCoroutine(Diving());
            }
            
            IEnumerator Diving()
            {
                Log("Diving");
                _anim.Play("Diving");
                _rb.velocity = Vector2.down * DiveSpeed;

                _audio.Play("Dive");
                
                while (!IsGrounded())
                {
                    yield return null;
                }

                yield return null;

                StartCoroutine(DiveLand());
            }

            IEnumerator DiveLand()
            {
                Log("Dive Land");
                _anim.Play("Dive Land");
                _rb.velocity = Vector2.zero;
                _audio.Play("Dive Land", 1, 1, 0.25f);
                SnapToGround();

                SpawnShockwaves(2, 50, 1);
                _diveShockwave.SetActive(true);

                yield return new WaitWhile(() => _anim.IsPlaying("Dive Land"));

                StartCoroutine(DiveRecover());
            }

            IEnumerator DiveRecover()
            {
                _anim.Play("Dive Recover");

                yield return new WaitWhile(() => _anim.IsPlaying("Dive Recover"));

                StartCoroutine(IdleAndChooseNextAttack());
            }

            StartCoroutine(DiveJump());
        }

        private void DryyaElegy()
        {
            IEnumerator ElegyAntic()
            {
                Log("Elegy Slash 1 Antic");
                _anim.Play("Elegy Slash 1 Antic");
                _rb.velocity = Vector2.zero;

                yield return new WaitForSeconds(0.5f);

                StartCoroutine(ElegySlash1());
            }

            IEnumerator ElegySlash1()
            {
                Log("Elegy Slash 1");
                _anim.Play("Elegy Slash 1");

                _elegyBeam1.SetActive(true);
                _elegyBeam1.transform.SetParent(null);
                
                yield return new WaitWhile(() => _anim.IsPlaying("Elegy Slash 1"));

                StartCoroutine(ElegySlash2());
            }

            IEnumerator ElegySlash2()
            {
                Log("Elegy Slash 2");
                _anim.Play("Elegy Slash 2");

                _elegyBeam2.SetActive(true);
                _elegyBeam2.transform.SetParent(null);
                
                yield return new WaitWhile(() => _anim.IsPlaying("Elegy Slash 2"));

                StartCoroutine(ElegyEnd());
            }

            IEnumerator ElegyEnd()
            {
                Log("Elegy End");
                _anim.Play("Elegy End");

                yield return new WaitWhile(() => _anim.IsPlaying("Elegy End"));

                StartCoroutine(IdleAndChooseNextAttack());
            }

            StartCoroutine(ElegyAntic());
        }
        
        private void DryyaEvade()
        {
            IEnumerator EvadeAntic()
            {
                Log("Evade Antic");
                _anim.Play("Evade Antic");
                _rb.velocity = Vector2.zero;
                
                InstantlyFaceHero();
                yield return new WaitWhile(() => _anim.IsPlaying("Evade Antic"));

                StartCoroutine(Evading(0.25f));
            }

            IEnumerator Evading(float evadeTime)
            {
                Log("Evading");
                _anim.Play("Evading");
                _rb.velocity = new Vector2(transform.localScale.x * EvadeSpeed, 0);

                yield return new WaitForSeconds(evadeTime);

                StartCoroutine(IdleAndChooseNextAttack());
            }

            StartCoroutine(EvadeAntic());
        }
        
        private void DryyaTripleSlash()
        {
            IEnumerator Slash1Antic()
            {
                _rb.velocity = Vector2.zero;
                _anim.Play("Slash 1 Antic");
                yield return new WaitForSeconds(0.5f);

                StartCoroutine(Slash1());
            }

            IEnumerator Slash1()
            {
                Log("Slash 1");
                _anim.Play("Slash 1");
                _audio.Play("Slash", 0.85f, 1.15f);
                _rb.velocity = new Vector2(-transform.localScale.x * SlashSpeed, 0);
                _slash1Collider1.SetActive(true);

                yield return new WaitForSeconds(AnimFPS);
                
                _rb.velocity = Vector2.zero;
                _slash1Collider1.SetActive(false);
                _slash1Collider2.SetActive(true);

                yield return new WaitForSeconds(AnimFPS);
                
                _slash1Collider2.SetActive(false);
                
                yield return new WaitWhile(() => _anim.IsPlaying("Slash 1"));

                StartCoroutine(Slash2Antic());
            }

            IEnumerator Slash2Antic()
            {
                _anim.Play("Slash 2 Antic");

                yield return new WaitWhile(() => _anim.IsPlaying("Slash 2 Antic"));

                StartCoroutine(Slash2());
            }
            
            IEnumerator Slash2()
            {
                Log("Slash 2");
                _anim.Play("Slash 2");
                _audio.Play("Slash", 0.85f, 1.15f);
                _rb.velocity = new Vector2(-transform.localScale.x * SlashSpeed, 0);
                _slash2Collider1.SetActive(true);

                yield return new WaitForSeconds(AnimFPS);
                
                _rb.velocity = Vector2.zero;
                _slash2Collider1.SetActive(false);
                _slash2Collider2.SetActive(true);

                yield return new WaitForSeconds(AnimFPS);
                
                _slash2Collider2.SetActive(false);
                
                yield return new WaitWhile(() => _anim.IsPlaying("Slash 2"));

                int num = Random.Range(0, 10);
                if (num > 3)
                    StartCoroutine(Slash3Antic());
                else
                    StartCoroutine(IdleAndChooseNextAttack());

            }

            IEnumerator Slash3Antic()
            {
                _anim.Play("Slash 3 Antic");

                yield return new WaitWhile(() => _anim.IsPlaying("Slash 3 Antic"));

                StartCoroutine(Slash3());
            }
            
            IEnumerator Slash3()
            {
                Log("Slash 3");
                _anim.Play("Slash 3");
                _audio.Play("Slash", 0.85f, 1.15f);
                _rb.velocity = new Vector2(-transform.localScale.x * SlashSpeed, 0);
                _slash3Collider1.SetActive(true);

                yield return new WaitForSeconds(AnimFPS);
                _rb.velocity = Vector2.zero;

                _slash3Collider1.SetActive(false);
                _slash3Collider2.SetActive(true);

                yield return new WaitForSeconds(AnimFPS);
                _slash3Collider2.SetActive(false);
                yield return new WaitWhile(() => _anim.IsPlaying("Slash 3"));

                StartCoroutine(SlashEnd());
            }

            IEnumerator SlashEnd()
            {
                _anim.Play("Slash End");

                yield return new WaitWhile(() => _anim.IsPlaying("Slash End"));
                
                StartCoroutine(IdleAndChooseNextAttack());
            }

            StartCoroutine(Slash1Antic());
        }

        private void InstantlyFaceHero()
        {
            float heroX = HeroController.instance.transform.position.x;
            float posX = transform.position.x;
            float distX = heroX - posX;
            int multiplier = Math.Sign(-transform.localScale.x * distX);
            if (multiplier < 0) Flip();
        }
        
        private void SpawnShockwaves(float vertScale, float speed, int damage)
        {
            bool[] facingRightBools = {false, true};
            Vector2 pos = transform.position;
            foreach (bool facingRight in facingRightBools)
            {
                GameObject shockwave =
                    Instantiate(_mageLord.GetAction<SpawnObjectFromGlobalPool>("Quake Waves", 0).gameObject.Value); ;
                PlayMakerFSM shockFSM = shockwave.LocateMyFSM("shockwave");
                shockFSM.FsmVariables.FindFsmBool("Facing Right").Value = facingRight;
                shockFSM.FsmVariables.FindFsmFloat("Speed").Value = speed;
                shockwave.AddComponent<DamageHero>().damageDealt = damage;
                shockwave.SetActive(true);
                shockwave.transform.SetPosition2D(new Vector2(pos.x + (facingRight ? 0.5f : -0.5f), 6f));
                shockwave.transform.SetScaleX(vertScale);
            }
        }
        
        private Coroutine _flashRoutine;
        public void FlashWhite(float stayTime, float timeDown)
        {
            IEnumerator Flash()
            {
                Material material = _sprite.Collection.material;
                float flashAmount = 1.0f;
                material.SetFloat("_FlashAmount", flashAmount);
                yield return new WaitForSeconds(stayTime);

                while (flashAmount > 0)
                {
                    yield return new WaitForSeconds(0.01f * timeDown);
                    flashAmount -= 0.01f;
                    material.SetFloat("_FlashAmount", flashAmount);
                }
                material.SetFloat("_FlashAmount", 0.0f);
            }
            
            if (_flashRoutine != null) StopCoroutine(_flashRoutine);
            _flashRoutine = StartCoroutine(Flash());
        }

        private void SnapToGround()
        {
            Transform trans = transform;
            trans.position = new Vector2(trans.position.x, GroundY);
        }

        private void Flip()
        {
            Vector3 localScale = transform.localScale;
            localScale.x *= -1;
            transform.localScale = localScale;
        }
        
        private const float GroundExtension = 0.01f;
        private const int CollisionMask = 1 << 8;
        private bool IsGrounded()
        {
            float rayLength = _collider.bounds.extents.y + GroundExtension;
            return Physics2D.Raycast(transform.position, Vector2.down, rayLength, CollisionMask);
        }

        private const float WallExtension = 0.5f;

        private bool TestWallCollisions()
        {
            float rayLength = _collider.bounds.extents.x + WallExtension;
            RaycastHit2D hitLeftWall = Physics2D.Raycast(transform.position, Vector2.left, rayLength, CollisionMask);
            RaycastHit2D hitRightWall = Physics2D.Raycast(transform.position, Vector2.right, rayLength, CollisionMask);

            if (hitLeftWall && _rb.velocity.x < 0 || hitRightWall && _rb.velocity.x > 0)
            {
                _rb.velocity = new Vector2(0, _rb.velocity.y);
            }

            return hitLeftWall || hitRightWall;

        }

        private void OnDestroy()
        {
            _hm.OnDeath += DeathHandler;
            On.EnemyDreamnailReaction.RecieveDreamImpact -= OnReceiveDreamImpact;
            On.HealthManager.Hit -= OnBlockedHit;
            On.HealthManager.TakeDamage -= OnTakeDamage;
        }
        
        private void Log(object message) => Modding.Logger.Log("[Dryya] " + message);
    }
}