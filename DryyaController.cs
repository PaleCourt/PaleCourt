using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Random = System.Random;
using System.Reflection;
using HutongGames.PlayMaker.Actions;
using ModCommon;
using ModCommon.Util;
using Modding;
using UnityEngine;

namespace FiveKnights
{
    public class DryyaController : MonoBehaviour
    {
        private const float GroundY = 8.5f;
        private const float LeftX = 61.0f;
        private const float RightX = 91.0f;
        private const float DashSpeed = 90.0f;
        private const float DiveJumpSpeed = 50.0f;
        private const float DiveSpeed = 80.0f;
        private const float EvadeSpeed = 40.0f;
        private const float SlashSpeed = 50.0f;
        private const float WalkSpeed = 15.0f;
        private const float AnimFPS = 1.0f / 12;

        private int _hp = 1650;
        private int _direction = -1;
        public Dictionary<string, List<Sprite>> animations = new Dictionary<string, List<Sprite>>();
        private Random _random;

        private PlayMakerFSM _pvControl;
        private PlayMakerFSM _kinControl;
        private PlayMakerFSM _mageLord;

        public GameObject ogrim;
        
        private string[] _dreamNailDialogue =
        {
            "DRYYA_DIALOG_1",
            "DRYYA_DIALOG_2",
            "DRYYA_DIALOG_3",
            "DRYYA_DIALOG_4",
            "DRYYA_DIALOG_5",
        };
        
        private void Awake()
        {
            GameObject go = gameObject;
            go.SetActive(true);
            go.layer = 11;
            _random = new Random();

            _pvControl = FiveKnights.preloadedGO["PV"].LocateMyFSM("Control");
            _kinControl = FiveKnights.preloadedGO["Kin"].LocateMyFSM("IK Control");
            _mageLord = FiveKnights.preloadedGO["Mage"].LocateMyFSM("Mage Lord");

            foreach(SpriteRenderer sr in gameObject.GetComponentsInChildren<SpriteRenderer>(true))
            {
                sr.material = new Material(Shader.Find("Sprites/Default"));
            }

            GetComponents();
            AddComponents();

            On.HealthManager.TakeDamage += OnTakeDamage;
            On.EnemyDreamnailReaction.RecieveDreamImpact += OnReceiveDreamImpact;
        }
        
        private IEnumerator Start()
        {
            while (HeroController.instance == null) yield return null;

            _dreamNailEffect = ogrim.GetComponent<EnemyDreamnailReaction>().GetAttr<EnemyDreamnailReaction, GameObject>("dreamImpactPrefab");

            _moves = new List<Action>
            {
                DryyaCounter,
                DryyaDash,
                DryyaDive,
                DryyaTripleSlash,
            };

            _repeats = new Dictionary<Action, int>
            {
                [DryyaCounter] = 0,
                [DryyaDash] = 0,
                [DryyaDive] = 0,
                [DryyaTripleSlash] = 0,
            };
            
            _maxRepeats = new Dictionary<Action, int>
            {
                [DryyaCounter] = 1,
                [DryyaDash] = 2,
                [DryyaDive] = 1,
                [DryyaTripleSlash] = 2,
            };
            
            AssignFields();

            DryyaIntro();
        }

        private void FixedUpdate()
        {
            TestWallCollisions();
        }
        
        private void OnTakeDamage(On.HealthManager.orig_TakeDamage orig, HealthManager self, HitInstance hitInstance)
        {
            if (self.name.Contains("Dryya"))
            {
                FlashWhite(0.05f, 0.1f);
            }
            
            orig(self, hitInstance);
        }

        private GameObject _dreamNailEffect;
        private void OnReceiveDreamImpact(On.EnemyDreamnailReaction.orig_RecieveDreamImpact orig, EnemyDreamnailReaction self)
        {
            if (self.name.Contains("Dryya"))
            {
                FlashWhite(0.25f, 0.75f);
                Instantiate(_dreamNailEffect, transform.position, Quaternion.identity);
                _dreamNailReaction.SetConvoTitle(_dreamNailDialogue[_random.Next(_dreamNailDialogue.Length)]);
            }

            orig(self);
        }

        // Put OnBlockedHit outside of DryyaCounter so that the event handler can be unhooked in OnDestroy if the scene changes mid-counter
        private void OnBlockedHit(On.HealthManager.orig_Hit orig, HealthManager self, HitInstance hitInstance)
        {
            if (self.name.Contains("Dryya"))
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
        
        private Animator _anim;
        private Rigidbody2D _rb;
        private SpriteRenderer _sr;
        private BoxCollider2D _collider;
        private void GetComponents()
        {
            _anim = GetComponent<Animator>();
            _collider = GetComponent<BoxCollider2D>();
            _rb = GetComponent<Rigidbody2D>();
            _sr = GetComponent<SpriteRenderer>();

            _sr.material = new Material(ArenaFinder.FlashShader);
        }

        private AudioSource _audio;
        private DamageHero _damageHero;
        private EnemyDeathEffectsUninfected _deathEffects;
        private EnemyDreamnailReaction _dreamNailReaction;
        private EnemyHitEffectsUninfected _hitEffects;
        private HealthManager _hm;
        private void AddComponents()
        {
            _audio = gameObject.AddComponent<AudioSource>();

            _deathEffects = gameObject.AddComponent<EnemyDeathEffectsUninfected>();
            _deathEffects.enabled = true;

            _dreamNailReaction = gameObject.AddComponent<EnemyDreamnailReaction>();
            _dreamNailReaction.enabled = true;
            _dreamNailReaction.SetConvoTitle(_dreamNailDialogue[_random.Next(_dreamNailDialogue.Length)]);
            
            _hitEffects = gameObject.AddComponent<EnemyHitEffectsUninfected>();
            _hitEffects.enabled = true;

            _hm = gameObject.AddComponent<HealthManager>();
            _hm.enabled = true;
            _hm.hp = _hp;

            _damageHero = gameObject.AddComponent<DamageHero>();
            _damageHero.enabled = true;
        }

        private void AssignFields()
        {
            EnemyDeathEffectsUninfected ogrimDeathEffects = ogrim.GetComponent<EnemyDeathEffectsUninfected>();
            foreach (FieldInfo fi in typeof(EnemyDeathEffectsUninfected).GetFields(BindingFlags.Instance | BindingFlags.Public))
            {
                fi.SetValue(_deathEffects, fi.GetValue(ogrimDeathEffects));
            }
            
            EnemyHitEffectsUninfected ogrimHitEffects = ogrim.GetComponent<EnemyHitEffectsUninfected>();
            foreach (FieldInfo fi in typeof(EnemyHitEffectsUninfected).GetFields(BindingFlags.Instance | BindingFlags.Public))
            {
                fi.SetValue(_hitEffects, fi.GetValue(ogrimHitEffects));
            }
            
            HealthManager ogrimHealth = ogrim.GetComponent<HealthManager>();
            foreach (FieldInfo fi in typeof(HealthManager).GetFields(BindingFlags.Instance | BindingFlags.NonPublic)
                .Where(x => x.Name.Contains("Prefab")))
            {
                fi.SetValue(_hm, fi.GetValue(ogrimHealth));
            }
        }

        public void PlayAudioClip(string clipName, float pitchMin = 1.0f, float pitchMax = 1.0f, float time = 0.0f)
        {
            AudioClip GetAudioClip()
            {
                switch (clipName)
                {
                    case "Counter":
                        return (AudioClip) _pvControl.GetAction<AudioPlayerOneShotSingle>("Counter Stance", 1).audioClip
                            .Value;
                    case "Dash":
                        return (AudioClip) _pvControl.GetAction<AudioPlayerOneShotSingle>("Dash", 1).audioClip.Value;
                    case "Dive":
                        return (AudioClip) _kinControl.GetAction<AudioPlaySimple>("Dstab Fall", 0).oneShotClip.Value;
                    case "Dive Land":
                        return (AudioClip) _kinControl.GetAction<AudioPlaySimple>("Dstab Land", 0).oneShotClip.Value;
                    case "Slash":
                        return (AudioClip) _pvControl.GetAction<AudioPlayerOneShotSingle>("Slash1", 1).audioClip.Value;
                    default:
                        return null;
                }
            }

            _audio.pitch = (float) (_random.NextDouble() * pitchMax) + pitchMin;
            _audio.time = time; 
            _audio.PlayOneShot(GetAudioClip());
            _audio.time = 0.0f;
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

            /*float minWait = 0.25f;
            float maxWait = 0.5f;
            float waitTime = (float) (_random.NextDouble() * maxWait) + minWait;
            
            yield return new WaitForSeconds(waitTime);*/
            yield return null;
            
            if (_nextMove != null) _previousMove = _nextMove;
            int index = _random.Next(_moves.Count);
            _nextMove = _moves[index];
            
            // Make sure moves don't occur more than its respective max number of repeats in a row
            while (_repeats[_nextMove] >= _maxRepeats[_nextMove])
            {
                index = _random.Next(_moves.Count);
                _nextMove = _moves[index];
            }

            Vector2 pos = transform.position;
            Vector2 heroPos = HeroController.instance.transform.position;
            float evadeRange = 4.0f;
            if (Mathf.Sqrt(Mathf.Pow(pos.x - heroPos.x, 2) + Mathf.Pow(pos.y - heroPos.y, 2)) < evadeRange)
            {
                int randNum = _random.Next(100);
                int threshold = 70;
                if (randNum < threshold)
                {
                    if (_direction == 1 && pos.x - LeftX > 4.0f || (_direction == -1 && RightX - pos.x > 4.0f))
                    {
                        if (_previousMove != DryyaWalk) _nextMove = DryyaEvade;
                    }
                }
            }
            else if (Mathf.Abs(pos.x - heroPos.x) <= 2.0f && heroPos.y - pos.y > 2.0f)
            {
                int randNum = _random.Next(100);
                int threshold = 50;
                if (randNum < threshold)
                {
                    // Pogo Punishment
                }
            }

            // Walk if Knight is out of walk range
            float walkRange = 10.0f;
            if (Mathf.Abs(heroPos.x - pos.x) > walkRange)
            {
                _nextMove = DryyaWalk;
            }
            
            // Turn if facing opposite of direction to Knight
            if (heroPos.x - pos.x < 0 && _direction == 1 || heroPos.x - pos.x > 0 && _direction == -1)
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
                Log("Intro Falling");
                _anim.Play("Intro Falling");
                int fallingSpeed = 50;
                _rb.velocity = new Vector2(-1, -1) * fallingSpeed;
                while (!IsGrounded())
                {
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
                _direction = -_direction;
                _sr.flipX = !_sr.flipX;
                
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
                    _rb.velocity = new Vector2(_direction * WalkSpeed * (i / 4), 0);
                    yield return new WaitForSeconds(AnimFPS);   
                }

                StartCoroutine(Walking());
            }

            IEnumerator Walking()
            {
                Log("Walking");
                _anim.Play("Walking");
                _rb.velocity = new Vector2(_direction * WalkSpeed, 0);
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
                _hm.IsInvincible = true;
                _anim.Play("Countering");
                
                _blockedHit = false;
                On.HealthManager.Hit += OnBlockedHit;
                PlayAudioClip("Counter");
                FlashWhite(0.01f, 0.35f);

                Vector2 fxPos = transform.position + Vector3.right * 1.9f * _direction + Vector3.up * 0.8f;
                Quaternion fxRot = Quaternion.Euler(0, 0, _direction * -60);
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
            On.HealthManager.Hit -= OnBlockedHit;
            
            yield return new WaitForSeconds(AnimFPS);

            StartCoroutine(CounterAttack());
        }

        private IEnumerator CounterAttack()
        {
            _hm.IsInvincible = false;
            Log("Counter Attack");
            _anim.Play("Slash Antic");
            yield return new WaitForSeconds(3 * AnimFPS);
            _anim.Play("Slash 1");
            PlayAudioClip("Slash", 0.85f, 1.15f);
            _rb.velocity = new Vector2(_direction * SlashSpeed, 0);
            GameObject slash1 = Instantiate(FiveKnights.preloadedGO["Slash"], transform);
            slash1.SetActive(true);
            slash1.layer = 22;
            slash1.AddComponent<DamageHero>();
            PolygonCollider2D slashCollider = slash1.GetComponent<PolygonCollider2D>();
            Vector2[] points1 =
            {
                new Vector2(-4.71f, -.31f),
                new Vector2(-3.09f, -1.51f),
                new Vector2(-1.05f, -1.12f),
                new Vector2(1.84f, 0),
                new Vector2(-1.44f, 1),
            };
            slashCollider.points = points1;
            slashCollider.SetPath(0, points1);

            yield return new WaitForSeconds(AnimFPS);
            _rb.velocity = Vector2.zero;
            Destroy(slash1);

            GameObject slash2 = Instantiate(FiveKnights.preloadedGO["Slash"], transform);
            slash2.SetActive(true);
            slash2.layer = 22;
            slash2.AddComponent<DamageHero>();
            slashCollider = slash2.GetComponent<PolygonCollider2D>();
            Vector2[] points2 =
            {
                new Vector2(3.73f, 1),
                new Vector2(5, -.65f),
                new Vector2(4.61f, -1.72f),
                new Vector2(1.87f, -.2f),
                new Vector2(3.77f, -.08f),
                new Vector2(1.84f, 1.72f),
            };
            slashCollider.points = points2;
            slashCollider.SetPath(0, points2);

            yield return new WaitForSeconds(AnimFPS);
            Destroy(slash2);
            yield return new WaitForSeconds(AnimFPS);
            
            StartCoroutine(IdleAndChooseNextAttack());
        }

        private GameObject _diveEffect;
        private void DryyaDash()
        {
            IEnumerator DashAntic()
            {
                Log("Dash Antic");
                _rb.velocity = Vector2.zero;
                _anim.Play("Dash Antic");
                
                yield return new WaitForSeconds(6 * AnimFPS);

                StartCoroutine(Dashing(0.15f));
            }

            IEnumerator Dashing(float dashTime)
            {
                Log("Dashing");
                _anim.Play("Dashing");
                Vector2 position = transform.position + Vector3.up * 0.1f;
                Quaternion rotation = Quaternion.Euler(0, 0, _direction * 180);
                GameObject stabEffect = Instantiate(FiveKnights.preloadedGO["Stab Effect"], position, rotation);
                stabEffect.SetActive(true);
                stabEffect.GetComponent<SpriteRenderer>().material = new Material(Shader.Find("Sprites/Default"));
                stabEffect.GetComponent<Animator>().Play("Stab Effect");
                Destroy(stabEffect, 3 * AnimFPS);

                PlayAudioClip("Dash");
                _rb.velocity = Vector2.right * _direction * DashSpeed;
                
                yield return new WaitForSeconds(dashTime);

                StartCoroutine(DashRecover());
            }

            IEnumerator DashRecover()
            {
                Log("Dash Recover");
                _anim.Play("Dash Recover");
                _rb.velocity = Vector2.zero;

                yield return new WaitForSeconds(3 * AnimFPS);

                StartCoroutine(IdleAndChooseNextAttack());
            }

            StartCoroutine(DashAntic());
        }

        private void DryyaDive()
        {
            IEnumerator DiveJumpAntic()
            {
                Log("Dive Jump Antic");
                _anim.Play("Dive Jump Antic");
                _rb.velocity = Vector2.zero;
                yield return new WaitForSeconds(3 * AnimFPS);

                StartCoroutine(DiveJump());
            }
            IEnumerator DiveJump()
            {
                Log("Dive Jump");
                _anim.Play("Dive Jump");
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

                yield return new WaitForSeconds(2 * AnimFPS);

                StartCoroutine(Diving());
            }
            
            IEnumerator Diving()
            {
                Log("Diving");
                _anim.Play("Diving");
                _rb.velocity = Vector2.down * DiveSpeed;
                Vector2 position = new Vector2(transform.position.x, GroundY - 2.0f);
                _diveEffect = Instantiate(FiveKnights.preloadedGO["Dive Effect"], position, Quaternion.identity);
                _diveEffect.SetActive(true);
                _diveEffect.GetComponent<SpriteRenderer>().material = new Material(Shader.Find("Sprites/Default"));
                _diveEffect.GetComponent<Animator>().Play("Dive Slam Diving");
                _diveEffect.layer = 22;

                PlayAudioClip("Dive");
                
                while (!IsGrounded())
                {
                    yield return null;
                }

                yield return null;

                StartCoroutine(DiveRecover());
            }

            IEnumerator DiveRecover()
            {
                Log("Dive Recover");
                _anim.Play("Dive Recover");
                _rb.velocity = Vector2.zero;

                PlayAudioClip("Dive Land", 1, 1, 0.25f);
                SnapToGround();
                
                SpawnShockwaves(2, 50, 1);
                _diveEffect.GetComponent<Animator>().Play("Dive Slam Effect");
                _diveEffect.AddComponent<DamageHero>();
                Destroy(_diveEffect, 3 * AnimFPS);
                
                yield return new WaitForSeconds(4 * AnimFPS);

                StartCoroutine(IdleAndChooseNextAttack());
            }

            StartCoroutine(DiveJumpAntic());
        }

        private void DryyaEvade()
        {
            IEnumerator EvadeAntic()
            {
                Log("Evade Antic");
                _anim.Play("Evade Antic");
                _rb.velocity = Vector2.zero;
                
                float heroX = HeroController.instance.transform.position.x;
                float posX = transform.position.x;
                float distX = heroX - posX;
                if (distX < 0 && _direction == 1)
                {
                    _sr.flipX = false;
                    _direction = -1;
                }
                else if (distX > 0 && _direction == -1)
                {
                    _sr.flipX = true;
                    _direction = 1;
                }

                yield return new WaitForSeconds(2 * AnimFPS);

                StartCoroutine(Evading(0.25f));
            }

            IEnumerator Evading(float evadeTime)
            {
                Log("Evading");
                _anim.Play("Evading");
                _rb.velocity = new Vector2(_direction * -EvadeSpeed, 0);

                yield return new WaitForSeconds(evadeTime);

                StartCoroutine(IdleAndChooseNextAttack());
            }

            StartCoroutine(EvadeAntic());
        }
        
        private void DryyaTripleSlash()
        {
            IEnumerator SlashAntic()
            {
                Log("Slash Antic");
                _rb.velocity = Vector2.zero;
                _anim.Play("Slash Antic");
                yield return new WaitForSeconds(6 * AnimFPS);

                StartCoroutine(Slash1());
            }

            IEnumerator Slash1()
            {
                Log("Slash 1");
                _anim.Play("Slash 1");
                PlayAudioClip("Slash", 0.85f, 1.15f);
                _rb.velocity = new Vector2(_direction * SlashSpeed, 0);
                GameObject slash1 = Instantiate(FiveKnights.preloadedGO["Slash"], transform);
                slash1.SetActive(true);
                slash1.layer = 22;
                slash1.AddComponent<DamageHero>();
                PolygonCollider2D slashCollider = slash1.GetComponent<PolygonCollider2D>();
                Vector2[] points1 =
                {
                    new Vector2(-4.71f, -.31f),
                    new Vector2(-3.09f, -1.51f),
                    new Vector2(-1.05f, -1.12f),
                    new Vector2(1.84f, 0),
                    new Vector2(-1.44f, 1),
                };
                slashCollider.points = points1;
                slashCollider.SetPath(0, points1);

                yield return new WaitForSeconds(AnimFPS);
                _rb.velocity = Vector2.zero;
                Destroy(slash1);

                GameObject slash2 = Instantiate(FiveKnights.preloadedGO["Slash"], transform);
                slash2.SetActive(true);
                slash2.layer = 22;
                slash2.AddComponent<DamageHero>();
                slashCollider = slash2.GetComponent<PolygonCollider2D>();
                Vector2[] points2 =
                {
                    new Vector2(3.73f, 1),
                    new Vector2(5, -.65f),
                    new Vector2(4.61f, -1.72f),
                    new Vector2(1.87f, -.2f),
                    new Vector2(3.77f, -.08f),
                    new Vector2(1.84f, 1.72f),
                };
                slashCollider.points = points2;
                slashCollider.SetPath(0, points2);

                yield return new WaitForSeconds(AnimFPS);
                Destroy(slash2);
                yield return new WaitForSeconds(AnimFPS);

                StartCoroutine(Slash2());
            }

            IEnumerator Slash2()
            {
                Log("Slash 2");
                _anim.Play("Slash 2");
                PlayAudioClip("Slash", 0.85f, 1.15f);
                _rb.velocity = new Vector2(_direction * SlashSpeed, 0);
                GameObject slash1 = Instantiate(FiveKnights.preloadedGO["Slash"], transform);
                slash1.SetActive(true);
                slash1.layer = 22;
                slash1.AddComponent<DamageHero>();
                PolygonCollider2D slashCollider = slash1.GetComponent<PolygonCollider2D>();
                Vector2[] points1 =
                {
                    new Vector2(-.89f, 4.99f),
                    new Vector2(2.55f, 3.68f),
                    new Vector2(-.74f, -3.27f),
                    new Vector2(-3.45f, .77f),
                    new Vector2(-3.19f, 3.71f),
                };
                slashCollider.points = points1;
                slashCollider.SetPath(0, points1);

                yield return new WaitForSeconds(AnimFPS);
                Destroy(slash1);
                _rb.velocity = Vector2.zero;

                GameObject slash2 = Instantiate(FiveKnights.preloadedGO["Slash"], transform);
                slash2.SetActive(true);
                slash2.layer = 22;
                slash2.AddComponent<DamageHero>();
                slashCollider = slash2.GetComponent<PolygonCollider2D>();
                Vector2[] points2 =
                {
                    new Vector2(1.31f, 1.1f),
                    new Vector2(4.03f, -2.39f),
                    new Vector2(4.08f, -.47f),
                    new Vector2(2.83f, -2.91f),
                    new Vector2(3.14f, .09f),
                };
                slashCollider.points = points2;
                slashCollider.SetPath(0, points2);

                yield return new WaitForSeconds(AnimFPS);
                Destroy(slash2);
                yield return new WaitForSeconds(AnimFPS);

                int num = _random.Next(0, 10);
                if (num > 3)
                {
                    StartCoroutine(Slash3());    
                }
                else
                {
                    StartCoroutine(IdleAndChooseNextAttack());
                }
                
            }

            IEnumerator Slash3()
            {
                Log("Slash 3");
                _anim.Play("Slash 3");
                PlayAudioClip("Slash", 0.85f, 1.15f);
                _rb.velocity = new Vector2(_direction * SlashSpeed, 0);
                GameObject slash1 = Instantiate(FiveKnights.preloadedGO["Slash"], transform);
                slash1.SetActive(true);
                slash1.layer = 22;
                slash1.AddComponent<DamageHero>();
                PolygonCollider2D slashCollider = slash1.GetComponent<PolygonCollider2D>();
                Vector2[] points1 =
                {
                    new Vector2(-3.72f, 2.23f),
                    new Vector2(-1.41f, 4.25f),
                    new Vector2(.98f, 5.31f),
                    new Vector2(-.54f, -.06f),
                    new Vector2(-4.2f, -1.36f),
                };
                slashCollider.points = points1;
                slashCollider.SetPath(0, points1);

                yield return new WaitForSeconds(AnimFPS);
                Destroy(slash1);
                _rb.velocity = Vector2.zero;

                GameObject slash2 = Instantiate(FiveKnights.preloadedGO["Slash"], transform);
                slash2.SetActive(true);
                slash2.layer = 22;
                slash2.AddComponent<DamageHero>();
                slashCollider = slash2.GetComponent<PolygonCollider2D>();
                Vector2[] points2 =
                {
                    new Vector2(1.05f, -.02f),
                    new Vector2(3.94f, -.29f),
                    new Vector2(1.63f, -1.97f),
                    new Vector2(-2.46f, -1.7f),
                    new Vector2(1.55f, -1.18f),
                };
                slashCollider.points = points2;
                slashCollider.SetPath(0, points2);

                yield return new WaitForSeconds(AnimFPS);
                Destroy(slash2);
                yield return new WaitForSeconds(4 * AnimFPS);

                StartCoroutine(IdleAndChooseNextAttack());
            }

            StartCoroutine(SlashAntic());
        }

        private void SpawnShockwaves(float vertScale, float speed, int damage)
        {
            bool[] facingRightBools = {false, true};
            Vector2 pos = transform.position;
            foreach (bool facingRight in facingRightBools)
            {
                Log("Instantiating Shockwave");
                GameObject shockwave =
                    Instantiate(_mageLord.GetAction<SpawnObjectFromGlobalPool>("Quake Waves", 0).gameObject.Value); ;
                PlayMakerFSM shockFSM = shockwave.LocateMyFSM("shockwave");
                shockFSM.FsmVariables.FindFsmBool("Facing Right").Value = facingRight;
                shockFSM.FsmVariables.FindFsmFloat("Speed").Value = speed;
                shockwave.AddComponent<DamageHero>().damageDealt = damage;
                shockwave.SetActive(true);
                shockwave.transform.SetPosition2D(new Vector2(pos.x + (facingRight ? 0.5f : -0.5f), 6.3f));
                shockwave.transform.SetScaleX(vertScale);
            }

        }
        
        private Coroutine _flashRoutine;
        public void FlashWhite(float stayTime, float timeDown)
        {
            IEnumerator Flash()
            {
                Material material = _sr.material;
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

        private const float Extension = 0.01f;
        private const int CollisionMask = 1 << 8;
        private bool IsGrounded()
        {
            float rayLength = _collider.bounds.extents.y + Extension;
            return Physics2D.Raycast(transform.position, Vector2.down, rayLength, CollisionMask);
        }

        private bool TestWallCollisions()
        {
            float rayLength = _collider.bounds.extents.x + Extension;
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
            On.HealthManager.TakeDamage -= OnTakeDamage;
            On.HealthManager.Hit -= OnBlockedHit;
            On.EnemyDreamnailReaction.RecieveDreamImpact -= OnReceiveDreamImpact;
        }
        
        private void Log(object message) => Modding.Logger.Log("[Dryya] " + message);
    }
}