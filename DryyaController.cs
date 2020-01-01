using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Random = System.Random;
using System.Reflection;
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

        private List<IEnumerator> _moves;
        private Dictionary<IEnumerator, int> _repeats;
        
        private int _hp = 1000;
        public Dictionary<string, List<Sprite>> animations = new Dictionary<string, List<Sprite>>();
        private Shader _flashShader;
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
            gameObject.SetActive(true);
            gameObject.layer = 11;
            _random = new Random();

            foreach(SpriteRenderer sr in gameObject.GetComponentsInChildren<SpriteRenderer>(true))
            {
                sr.material = new Material(Shader.Find("Sprites/Default"));
            }

            GetComponents();
            AddComponents();
            
            Log("Animator null? " + (_anim == null));
            Log("BoxCollider2D null? " + (_collider == null));
            Log("Rigidbody2D null? " + (_rb == null));
            Log("SpriteRenderer null? " + (_sr == null));
            
            Log("Animator enabled? " + _anim.enabled);
            Log("BoxCollider2D enabled? " + _collider.enabled);
            Log("BoxCollider2D trigger? " + _collider.isTrigger);
            Log("Rigidbody2D kinematic? " + _rb.isKinematic);
            Log("SpriteRenderer enabled? " + _sr.enabled);

            On.HealthManager.TakeDamage += OnTakeDamage;
            On.EnemyDreamnailReaction.RecieveDreamImpact += OnReceiveDreamImpact;
        }
        
        private IEnumerator Start()
        {
            while (HeroController.instance == null) yield return null;

            _moves = new List<IEnumerator>
            {
                //Action
            };

            _repeats = new Dictionary<IEnumerator, int>
            {
                //[Action] = 0;
            };
            
            AssignFields();
            //AddAnimations();

            StartCoroutine(IntroFalling());
        }

        private void FixedUpdate()
        {
            
        }
        
        private void OnTakeDamage(On.HealthManager.orig_TakeDamage orig, HealthManager self, HitInstance hitInstance)
        {
            if (self.name.Contains("Dryya"))
            {
                FlashWhite(0.25f);
            }
            
            orig(self, hitInstance);
        }

        private void OnReceiveDreamImpact(On.EnemyDreamnailReaction.orig_RecieveDreamImpact orig, EnemyDreamnailReaction self)
        {
            if (self.name.Contains("Dryya"))
            {
                FlashWhite(1.0f);   
            }
            
            _dreamNailReaction.SetConvoTitle(_dreamNailDialogue[_random.Next(_dreamNailDialogue.Length)]);
            
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
           /* _collider = gameObject.AddComponent<BoxCollider2D>();
            _collider.enabled = true;
            _collider.size = new Vector2(2, 5);
            _collider.isTrigger = true;*/
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

            //_rb = gameObject.AddComponent<Rigidbody2D>();
            //_rb.isKinematic = true;

            /*_sr = gameObject.AddComponent<SpriteRenderer>();
            _sr.enabled = true;
            _sr.material = new Material(ArenaFinder.flashShader);*/
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

        private Sprite[] _dryyaIntroSprites;
        private Sprite[] _dryyaWalkSprites;
        private void AddAnimations()
        {
            List<Sprite> idleSprites = new List<Sprite>
            {
                FindSprite(_dryyaIntroSprites, "Dryya_Intro_12"),
            };
            
            List<Sprite> introDashInSprites = new List<Sprite>
            {
                FindSprite(_dryyaIntroSprites, "Dryya_Intro_0"),
                FindSprite(_dryyaIntroSprites, "Dryya_Intro_1"),
            };
            
            List<Sprite> introLandSprites = new List<Sprite>
            {
                FindSprite(_dryyaIntroSprites, "Dryya_Intro_2"),
                FindSprite(_dryyaIntroSprites, "Dryya_Intro_3"),
                FindSprite(_dryyaIntroSprites, "Dryya_Intro_4"),
                FindSprite(_dryyaIntroSprites, "Dryya_Intro_5"),
            };

            List<Sprite> introBackstep1Sprites = new List<Sprite>
            {
                FindSprite(_dryyaIntroSprites, "Dryya_Intro_6"),
                FindSprite(_dryyaIntroSprites, "Dryya_Intro_7"),
                FindSprite(_dryyaIntroSprites, "Dryya_Intro_8"),
                FindSprite(_dryyaIntroSprites, "Dryya_Intro_9"),
            };
            
            List<Sprite> introBackstep2Sprites = new List<Sprite>
            {
                FindSprite(_dryyaIntroSprites, "Dryya_Intro_10"),
                FindSprite(_dryyaIntroSprites, "Dryya_Intro_11"),
                FindSprite(_dryyaIntroSprites, "Dryya_Intro_12"),
            };
            
            List<Sprite> introToWalkSprites = new List<Sprite>
            {
                FindSprite(_dryyaWalkSprites, "Dryya_Walk_0"),
                FindSprite(_dryyaWalkSprites, "Dryya_Walk_1"),
                FindSprite(_dryyaWalkSprites, "Dryya_Walk_2"),
                FindSprite(_dryyaWalkSprites, "Dryya_Walk_3"),
            };

            List<Sprite> walkingSprites = new List<Sprite>
            {
                FindSprite(_dryyaWalkSprites, "Dryya_Walk_4"),
                FindSprite(_dryyaWalkSprites, "Dryya_Walk_5"),
                FindSprite(_dryyaWalkSprites, "Dryya_Walk_6"),
                FindSprite(_dryyaWalkSprites, "Dryya_Walk_7"),
                FindSprite(_dryyaWalkSprites, "Dryya_Walk_8"),
                FindSprite(_dryyaWalkSprites, "Dryya_Walk_9"),
                FindSprite(_dryyaWalkSprites, "Dryya_Walk_10"),
            };
            
            List<Sprite> walkToIdleSprites = new List<Sprite>
            {
                FindSprite(_dryyaWalkSprites, "Dryya_Walk_3"),
                FindSprite(_dryyaWalkSprites, "Dryya_Walk_2"),
                FindSprite(_dryyaWalkSprites, "Dryya_Walk_1"),
                FindSprite(_dryyaWalkSprites, "Dryya_Walk_0"),
            };

            animations.Add("Idle", idleSprites);
            animations.Add("Intro Dash In", introDashInSprites);
            animations.Add("Intro Land", introLandSprites);
            animations.Add("Intro Backstep 1", introBackstep1Sprites);
            animations.Add("Intro Backstep 2", introBackstep2Sprites);
            animations.Add("Intro to Walk", introToWalkSprites);
            animations.Add("Walking", walkingSprites);
            animations.Add("Walk to Idle", walkToIdleSprites);
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
            Log("Getting Falling Speed");
            float fallingSpeed = 75.0f;
            Log("Setting RB velocity");
            _rb.velocity = new Vector2(-1, -1) * fallingSpeed;
            /*Log("IsGrounded?");
            while (!IsGrounded())
            {
                yield return null;
                Log("Rb Velocity: " + _rb.velocity);
            }*/

            yield return null;
            
            Log("Starting IntroLand Coroutine");
            StartCoroutine(IntroLand());
        }
        private IEnumerator IntroLand()
        {
            Log("Intro Land");
            SnapToGround();
            Log("Playing Intro Land");
            _anim.Play("Intro Land");
            Log("Setting Rb to 0");
            _rb.velocity = Vector2.zero;
            Log("Waiting...");
            yield return new WaitWhile(() =>
            {
                Log("Current Frame: " + _anim.GetCurrentFrame());
                return _anim.GetCurrentFrame() <= 2;
            });
            
            Log("Starting Backstep 1 Coroutine");
            StartCoroutine(IntroBackstep1());
        }
        private IEnumerator IntroBackstep1()
        {
            Log("Intro Backstep 1");
            _anim.Play("Backstep 1");
            _rb.velocity = new Vector2(5, 0);
            yield return new WaitWhile(() => _anim.GetCurrentFrame() <= 1);
            _rb.velocity = Vector2.zero;
            yield return new WaitWhile(() => _anim.GetCurrentFrame() <= 2);

            StartCoroutine(IntroBackstep2());
        }
        private IEnumerator IntroBackstep2()
        {
            Log("Intro Backstep 2");
            _anim.Play("Backstep 2");
            _rb.velocity = new Vector2(8, 0);
            yield return new WaitWhile(() => _anim.GetCurrentFrame() <= 0);
            _rb.velocity = Vector2.zero;
            yield return new WaitWhile(() => _anim.GetCurrentFrame() <= 1);

            StartCoroutine(IdleToWalk());
        }
        private IEnumerator IdleToWalk()
        {
            Log("Idle to Walk");
            _anim.Play("Intro To Walk");
            _rb.velocity = Vector2.zero;
            yield return new WaitWhile(() => _anim.GetCurrentFrame() <= 2);

            StartCoroutine(Walking());
        }
        private IEnumerator Walking()
        {
            Log("Walking");
            _anim.Play("Walking");
            yield return null;
        }

        public static Sprite FindSprite(Sprite[] spriteList, string spriteName)
        {
            foreach (Sprite sprite in spriteList)
            {
                if (sprite.name == spriteName)
                {
                    return sprite;
                }
            }
            return null;
        }

        public float GetAnimDuration(string animName, float delay = AnimFPS)
        {
            int numFrames = animations[animName].Count;
            return numFrames * delay;
        }
        
        private const float AnimFPS = 1.0f / 12;

        private Coroutine _flashRoutine;
        public void FlashWhite(float time)
        {
            IEnumerator Flash()
            {
                float flashAmount = 1.0f;
                Material material = _sr.material;
                while (flashAmount > 0)
                {
                    material.SetFloat("_FlashAmount", flashAmount);
                    flashAmount -= 0.01f;
                    yield return new WaitForSeconds(0.01f * time);
                }
                yield return null;
            }
            
            if (_flashRoutine != null) StopCoroutine(_flashRoutine);
            _flashRoutine = StartCoroutine(Flash());
        }

        private void SnapToGround()
        {
            transform.position = new Vector2(transform.position.x, GroundY);
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
        }
        
        private void Log(object message) => Modding.Logger.Log("[Dryya] " + message);
    }
}