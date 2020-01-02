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
        private const float WalkSpeed = 20.0f;
        private const float AnimFPS = 1.0f / 12;

        private List<IEnumerator> _moves;
        private Dictionary<IEnumerator, int> _repeats;
        
        private int _hp = 1000;
        public Dictionary<string, List<Sprite>> animations = new Dictionary<string, List<Sprite>>();
        private Random _random;

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
            
            _dreamNailEffect = ogrim.GetComponent<EnemyDreamnailReaction>().GetAttr<GameObject>("dreamImpactPrefab");

            _moves = new List<IEnumerator>
            {
                //Action
            };

            _repeats = new Dictionary<IEnumerator, int>
            {
                //[Action] = 0;
            };
            
            AssignFields();

            StartCoroutine(IntroFalling());
        }

        private void FixedUpdate()
        {
            
        }
        
        private void OnTakeDamage(On.HealthManager.orig_TakeDamage orig, HealthManager self, HitInstance hitInstance)
        {
            if (self.name.Contains("Dryya"))
            {
                FlashWhite(1f, 1 / 1000f, 0.05f, 0.1f);
            }
            
            orig(self, hitInstance);
        }

        private GameObject _dreamNailEffect;
        private void OnReceiveDreamImpact(On.EnemyDreamnailReaction.orig_RecieveDreamImpact orig, EnemyDreamnailReaction self)
        {
            if (self.name.Contains("Dryya"))
            {
                FlashWhite(0.9f, 0.01f, 0.25f, 0.75f);
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
        
        private DamageHero _damageHero;
        private EnemyDreamnailReaction _dreamNailReaction;
        private EnemyHitEffectsUninfected _hitEffects;
        private HealthManager _hm;
        private void AddComponents()
        {
            gameObject.AddComponent<DebugColliders>();

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

        private IEnumerator IdleAndChooseNextAttack()
        {
            _anim.Play("Idle");
            FaceHero();
            
            float minWait = 0.25f;
            float maxWait = 0.5f;
            float waitTime = (float) (_random.NextDouble() * maxWait) + minWait;
            
            yield return new WaitForSeconds(waitTime);

            int index = _random.Next(_moves.Count);
            IEnumerator nextMove = _moves[index];
            
            // Make sure moves don't occur more than twice in a row
            while (_repeats[nextMove] >= 2)
            {
                index = _random.Next(_moves.Count);
                Log("Index: " + index);
                nextMove = _moves[index];
            }

            foreach (IEnumerator move in _moves)
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

            Log("Next Move: " + nextMove);
            StartCoroutine(nextMove);
        }
        
        private IEnumerator IntroFalling()
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
        private IEnumerator IntroLand()
        {
            Log("Intro Land");
            SnapToGround();
            _anim.Play("Intro Land");
            _rb.velocity = Vector2.zero;
            yield return new WaitForSeconds(4 * AnimFPS);
            
            Log("Starting Backstep 1 Coroutine");
            StartCoroutine(IntroBackstep1());
        }
        private IEnumerator IntroBackstep1()
        {
            Log("Intro Backstep 1");
            _anim.Play("Backstep 1");
            _rb.velocity = new Vector2(5, 0);
            yield return new WaitForSeconds(3 * AnimFPS);
            _rb.velocity = Vector2.zero;
            yield return new WaitForSeconds(AnimFPS);

            StartCoroutine(IntroBackstep2());
        }
        private IEnumerator IntroBackstep2()
        {
            Log("Intro Backstep 2");
            _anim.Play("Backstep 2");
            _rb.velocity = new Vector2(8, 0);
            yield return new WaitForSeconds(3 * AnimFPS);

            StartCoroutine(IdleToWalk());
        }
        private IEnumerator IdleToWalk()
        {
            Log("Idle to Walk");
            _anim.Play("Intro To Walk");
            _rb.velocity = Vector2.zero;
            yield return new WaitForSeconds(4 * AnimFPS);

            StartCoroutine(Walking());
        }
        private IEnumerator Walking()
        {
            Log("Walking");
            _anim.Play("Walking");
            yield return null;
        }

        private Coroutine _flashRoutine;
        public void FlashWhite(float amount, float timeUp, float stayTime, float timeDown)
        {
            IEnumerator Flash()
            {
                Material material = _sr.material;
                float flashAmount = 0.0f;
                while (flashAmount <= amount)
                {
                    material.SetFloat("_FlashAmount", flashAmount);
                    flashAmount += 0.1f;
                    yield return new WaitForSeconds(0.1f * timeUp);
                }

                material.SetFloat("_FlashAmount", amount);
                
                yield return new WaitForSeconds(stayTime);
                
                flashAmount = amount;
                
                while (flashAmount >= 0)
                {
                    material.SetFloat("_FlashAmount", flashAmount);
                    flashAmount -= 0.1f;
                    yield return new WaitForSeconds(0.1f * timeDown);
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

        private void OnDestroy()
        {
            On.HealthManager.TakeDamage -= OnTakeDamage;
            On.EnemyDreamnailReaction.RecieveDreamImpact -= OnReceiveDreamImpact;
        }
        
        private void Log(object message) => Modding.Logger.Log("[Dryya] " + message);
    }
}