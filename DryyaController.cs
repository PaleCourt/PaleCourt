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
        private const float GroundY = 7.4f;
        private const float LeftY = 61.0f;
        private const float RightY = 91.0f;

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
        
        private Sprite[] _dryyaIntroSprites;
        private Sprite[] _dryyaWalkSprites;
        private void Awake()
        {
            gameObject.layer = 11;
            _dryyaIntroSprites = ArenaFinder.DryyaAssetBundle.LoadAssetWithSubAssets<Sprite>("Dryya_Intro");
            _dryyaWalkSprites = ArenaFinder.DryyaAssetBundle.LoadAssetWithSubAssets<Sprite>("Dryya_Walk");
            _flashShader = ArenaFinder.DryyaAssetBundle.LoadAsset<Shader>("Flash Shader");
            _random = new Random();

            On.HealthManager.TakeDamage += OnTakeDamage;
            On.EnemyDreamnailReaction.RecieveDreamImpact += OnReceiveDreamImpact;
        }

        private IEnumerator Start()
        {
            while (HeroController.instance == null) yield return null;
            
            AddComponents();
            AssignFields();
            AddAnimations();

            DryyaIntro();
        }

        private void FixedUpdate()
        {
            Log("Is Grounded: " + IsGrounded());
        }
        
        private void OnTakeDamage(On.HealthManager.orig_TakeDamage orig, HealthManager self, HitInstance hitInstance)
        {
            if (self.name.Contains("Dryya"))
            {
                FlashWhite(0.25f);
            }
            
            orig(self, hitInstance);
        }

        private void OnReceiveDreamImpact(On.EnemyDreamnailReaction.orig_RecieveDreamImpact orig, EnemyDreamnailReaction dreamNailReaction)
        {
            FlashWhite(1.0f);
            _dreamNailReaction.SetConvoTitle(_dreamNailDialogue[_random.Next(_dreamNailDialogue.Length)]);

            orig(dreamNailReaction);
        }
        
        private BoxCollider2D _collider;
        private DamageHero _damageHero;
        private EnemyDreamnailReaction _dreamNailReaction;
        private EnemyHitEffectsUninfected _hitEffects;
        private HealthManager _hm;
        private Rigidbody2D _rb;
        private SpriteRenderer _sr;
        private void AddComponents()
        {
            _collider = gameObject.AddComponent<BoxCollider2D>();
            _collider.enabled = true;
            _collider.size = new Vector2(2, 4);
            _collider.isTrigger = true;
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

            _rb = gameObject.AddComponent<Rigidbody2D>();
            _rb.isKinematic = true;

            _sr = gameObject.AddComponent<SpriteRenderer>();
            _sr.enabled = true;
            _sr.material = new Material(_flashShader);
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

        private void AddAnimations()
        {
            List<Sprite> introDashInSprites = new List<Sprite>
            {
                FindSprite(_dryyaIntroSprites, "Dryya_Intro_0"),
            };
            List<Sprite> introLandSprites = new List<Sprite>
            {
                FindSprite(_dryyaIntroSprites, "Dryya_Intro_1"),
                FindSprite(_dryyaIntroSprites, "Dryya_Intro_2"),
                FindSprite(_dryyaIntroSprites, "Dryya_Intro_3"),
                FindSprite(_dryyaIntroSprites, "Dryya_Intro_4"),
                FindSprite(_dryyaIntroSprites, "Dryya_Intro_5"),
            };

            List<Sprite> introFlourishSprites = new List<Sprite>
            {
                FindSprite(_dryyaIntroSprites, "Dryya_Intro_6"),
                FindSprite(_dryyaIntroSprites, "Dryya_Intro_7"),
                FindSprite(_dryyaIntroSprites, "Dryya_Intro_8"),
                FindSprite(_dryyaIntroSprites, "Dryya_Intro_9"),
                FindSprite(_dryyaIntroSprites, "Dryya_Intro_10"),
                FindSprite(_dryyaIntroSprites, "Dryya_Intro_11"),
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
            };

            animations.Add("Intro Dash In", introDashInSprites);
            animations.Add("Intro Land", introLandSprites);
            animations.Add("Intro Flourish", introFlourishSprites);
            animations.Add("Intro to Walk", introToWalkSprites);
            animations.Add("Walking", walkingSprites);
        }

        private void DryyaIntro()
        {
            IEnumerator IntroDashIn()
            {
                PlayAnimation("Intro Dash In");
                float dashInSpeed = 3.0f;
                _rb.velocity = new Vector2(-1, -1) * dashInSpeed;
                while (!IsGrounded())
                {
                    yield return null;
                }

                StartCoroutine(IntroLand());
            }
            
            IEnumerator IntroLand()
            {
                SnapToGround();
                PlayAnimation("Intro Land");
                _rb.velocity = Vector2.zero;
                yield return new WaitForSeconds(GetAnimDuration("Intro Land"));

                StartCoroutine(IntroFlourish());
            }

            IEnumerator IntroFlourish()
            {
                PlayAnimation("Intro Flourish");
                _rb.velocity = new Vector2(1.5f, 0);
                yield return new WaitForSeconds(GetAnimDuration("Intro Flourish"));
                _rb.velocity = Vector2.zero;

                StartCoroutine(IntroToWalk());
            }

            IEnumerator IntroToWalk()
            {
                PlayAnimation("Intro to Walk");
                yield return new WaitForSeconds(GetAnimDuration("Intro to Walk"));

                StartCoroutine(Walking());
            }

            IEnumerator Walking()
            {
                PlayAnimation("Walking", true);
                yield return null;
            }

            StartCoroutine(IntroDashIn());
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
        private Coroutine _animRoutine;
        public void PlayAnimation(string animName, bool looping = false, float delay = AnimFPS)
        {
            IEnumerator Play()
            {
                List<Sprite> animation = animations[animName];
                do
                {
                    foreach (var frame in animation)
                    {
                        _sr.sprite = frame;
                        yield return new WaitForSeconds(delay);
                    }
                } 
                while (looping);
            }
            if (_animRoutine != null) StopCoroutine(_animRoutine);
            _animRoutine = StartCoroutine(Play());
        }
        
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
            transform.position.SetY(GroundY);
        }
        
        private const float Extension = 0.01f;
        private const int CollisionMask = 1 << 8;
        private bool IsGrounded()
        {
            float rayLength = _sr.bounds.extents.y + Extension;
            return Physics2D.Raycast(transform.position, Vector2.down, rayLength, CollisionMask);
        }

        private void OnDestroy()
        {
            On.HealthManager.TakeDamage -= OnTakeDamage;
        }
        
        private void Log(object message) => Modding.Logger.Log("[Dryya] " + message);
    }
}