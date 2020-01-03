using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Random = System.Random;
using System.Reflection;
using HutongGames.PlayMaker.Actions;
using ModCommon;
using ModCommon.Util;
using UnityEngine;

namespace FiveKnights
{
    public class DryyaController : MonoBehaviour
    {
        private const float GroundY = 8.5f;
        private const float LeftY = 61.0f;
        private const float RightY = 91.0f;
        private const float SlashSpeed = 40.0f;
        private const float WalkSpeed = 20.0f;
        private const float AnimFPS = 1.0f / 12;

        private List<Action> _moves;
        private Dictionary<Action, int> _repeats;
        
        private int _hp = 1000;
        private int _direction = -1;
        public Dictionary<string, List<Sprite>> animations = new Dictionary<string, List<Sprite>>();
        private Random _random;

        private PlayMakerFSM _pvControl;

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

            foreach(SpriteRenderer sr in gameObject.GetComponentsInChildren<SpriteRenderer>(true))
            {
                sr.material = new Material(Shader.Find("Sprites/Default"));
            }

            GetComponents();
            AddComponents();
            
            FiveKnights.preloadedGO["PV"].PrintSceneHierarchyTree();

            On.HealthManager.TakeDamage += OnTakeDamage;
            On.EnemyDreamnailReaction.RecieveDreamImpact += OnReceiveDreamImpact;
        }
        
        private IEnumerator Start()
        {
            while (HeroController.instance == null) yield return null;
            
            _dreamNailEffect = ogrim.GetComponent<EnemyDreamnailReaction>().GetAttr<GameObject>("dreamImpactPrefab");

            _moves = new List<Action>
            {
                DryyaTripleSlash,
            };

            _repeats = new Dictionary<Action, int>
            {
                [DryyaTripleSlash] = 0,
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

            _sr.material = new Material(ArenaFinder.flashShader);
        }

        private AudioSource _audio;
        private DamageHero _damageHero;
        private EnemyDreamnailReaction _dreamNailReaction;
        private EnemyHitEffectsUninfected _hitEffects;
        private HealthManager _hm;
        private void AddComponents()
        {
            _audio = gameObject.AddComponent<AudioSource>();
            
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
            HealthManager ogrimHealth = ogrim.GetComponent<HealthManager>();
            foreach (FieldInfo fi in typeof(HealthManager).GetFields(BindingFlags.Instance | BindingFlags.NonPublic)
                .Where(x => x.Name.Contains("Prefab")))
            {
                fi.SetValue(_hm, fi.GetValue(ogrimHealth));
            }

            EnemyHitEffectsUninfected ogrimHitEffects = ogrim.GetComponent<EnemyHitEffectsUninfected>();
            foreach (FieldInfo fi in typeof(EnemyHitEffectsUninfected).GetFields(BindingFlags.Instance | BindingFlags.Public))
            {
                fi.SetValue(_hitEffects, fi.GetValue(ogrimHitEffects));
            }
        }

        public void PlayAudioClip(string clipName, float pitchMin = 1.0f, float pitchMax = 1.0f, float time = 0.0f)
        {
            AudioClip GetAudioClip()
            {
                switch (clipName)
                {
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

        
        private IEnumerator IdleAndChooseNextAttack()
        {
            _anim.Play("Idle");
            _rb.velocity = Vector2.zero;

            /*float minWait = 0.25f;
            float maxWait = 0.5f;
            float waitTime = (float) (_random.NextDouble() * maxWait) + minWait;
            
            yield return new WaitForSeconds(waitTime);*/
            yield return null;

            int index = _random.Next(_moves.Count);
            Action nextMove = _moves[index];
            
            // Make sure moves don't occur more than twice in a row
            /*while (_repeats[nextMove] >= 2)
            {
                index = _random.Next(_moves.Count);
                Log("Index: " + index);
                nextMove = _moves[index];
            }*/

            foreach (Action move in _moves)
            {
                if (move == nextMove)
                {
                    _repeats[move]++;
                }
                else
                {
                    _repeats[move] = 0;
                }
            }

            Vector2 pos = transform.position;
            Vector2 heroPos = HeroController.instance.transform.position;
            if (Mathf.Sqrt(Mathf.Pow(pos.x - heroPos.x, 2) + Mathf.Pow(pos.y - heroPos.y, 2)) < 4.0f)
            {
                int randNum = _random.Next(100);
                int threshold = 70;
                if (randNum < threshold)
                {
                    // Evade
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

            if (heroPos.x - pos.x < 0 && _direction == 1 ||
                heroPos.x - pos.x > 0 && _direction == -1)
            {
                nextMove = DryyaTurn;
            }

            Log("Next Move: " + nextMove.Method.Name);
            nextMove.Invoke();
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
                _anim.Play("Intro To Walk");
                _rb.velocity = Vector2.zero;
                yield return new WaitForSeconds(4 * AnimFPS);

                StartCoroutine(Walking());
            }

            IEnumerator Walking()
            {
                Log("Walking");
                _anim.Play("Walking");
                _rb.velocity = new Vector2(_direction * WalkSpeed, 0);
                Vector2 heroPos = HeroController.instance.transform.position;
                Vector2 pos = transform.position;
                float distance = (float) Math.Sqrt(Math.Pow(heroPos.x - pos.x, 2) + Math.Pow(heroPos.y - pos.y, 2));
                while (distance >= 5.0f)
                {
                    heroPos = HeroController.instance.transform.position;
                    pos = transform.position;                    
                    distance = (float)Math.Sqrt(Math.Pow(heroPos.x - pos.x, 2) + Math.Pow(heroPos.y - pos.y, 2));
                    yield return null;
                }

                StartCoroutine(IdleAndChooseNextAttack());
            }

            StartCoroutine(IdleToWalk());
        }

        private void DryyaTripleSlash()
        {
            IEnumerator SlashAntic()
            {
                Log("Slash Antic");
                _anim.Play("Slash Antic");
                _rb.velocity = Vector2.zero;
                yield return new WaitForSeconds(0.5f);

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

                StartCoroutine(Slash3());
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

                StartCoroutine(IdleAndChooseNextAttack());
            }

            StartCoroutine(SlashAntic());
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
            transform.position.SetY(GroundY);
        }
        
        private float FaceHero(bool opposite = false)
        {
            float heroSignX = Mathf.Sign(HeroController.instance.transform.position.x - gameObject.transform.position.x);
            heroSignX = opposite ? -heroSignX : heroSignX;
            Vector3 pScale = gameObject.transform.localScale;
            gameObject.transform.localScale = new Vector3(Mathf.Abs(pScale.x) * heroSignX, pScale.y, 1f);
            return heroSignX;
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
            On.EnemyDreamnailReaction.RecieveDreamImpact -= OnReceiveDreamImpact;
        }
        
        private void Log(object message) => Modding.Logger.Log("[Dryya] " + message);
    }
}