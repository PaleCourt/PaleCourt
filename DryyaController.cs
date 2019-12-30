using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Random = System.Random;
using System.Reflection;
using ModCommon;
using UnityEngine;

namespace FiveKnights
{
    public class DryyaController : MonoBehaviour
    {
        private int _hp = 1000;
        public Dictionary<string, List<Sprite>> animations = new Dictionary<string, List<Sprite>>();
        private Sprite[] _dryyaIntroSprites;
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
            gameObject.layer = 11;
            _dryyaIntroSprites = ArenaFinder.DryyaAssetBundle.LoadAssetWithSubAssets<Sprite>("Dryya_Intro");
            _flashShader = ArenaFinder.DryyaAssetBundle.LoadAsset<Shader>("Flash Shader");
            _random = new Random();
        }

        private IEnumerator Start()
        {
            while (HeroController.instance == null) yield return null;
            
            AddComponents();
            AssignFields();
            AddAnimations();

            DryyaIntro();
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
            foreach (FieldInfo fi in typeof(EnemyHitEffectsUninfected).GetFields(BindingFlags.Default | BindingFlags.Instance | BindingFlags.Public | BindingFlags.Static | BindingFlags.CreateInstance | BindingFlags.DeclaredOnly | BindingFlags.ExactBinding | BindingFlags.FlattenHierarchy | BindingFlags.GetField | BindingFlags.GetProperty | BindingFlags.IgnoreCase | BindingFlags.IgnoreReturn | BindingFlags.InvokeMethod | BindingFlags.NonPublic | BindingFlags.SetField | BindingFlags.SetProperty | BindingFlags.OptionalParamBinding | BindingFlags.PutDispProperty | BindingFlags.SuppressChangeType | BindingFlags.PutRefDispProperty))
            {
                fi.SetValue(_hitEffects, fi.GetValue(ogrimHitEffects));
            }
        }

        private void AddAnimations()
        {
            List<Sprite> introLandSprites = new List<Sprite>
            {
                FindSprite(_dryyaIntroSprites, "Dryya_Intro_0"),
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
            
            animations.Add("Intro Land", introLandSprites);
            animations.Add("Intro Flourish", introFlourishSprites);
        }

        private void DryyaIntro()
        {
            IEnumerator IntroLand()
            {
                PlayAnimation("Intro Land");
                yield return new WaitForSeconds(GetAnimDuration("Intro Land"));

                StartCoroutine(IntroFlourish());
            }

            IEnumerator IntroFlourish()
            {
                PlayAnimation("Intro Flourish");
                _rb.velocity = new Vector2(0.5f, 0);
                yield return new WaitForSeconds(GetAnimDuration("Intro Flourish"));
                _rb.velocity = Vector2.zero;
            }

            StartCoroutine(IntroLand());
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

        private List<string> validColliders = new List<string>
        {
            "Slash", "AltSlash", "DownSlash", "UpSlash", "Hit L", "Hit R", "Hit U", "Hit D", "Great Slash",
            "Dash Slash", "Q Fall Damage", "Fireball2 Spiral(Clone)", "Enemy Damager", "Shield", 
            "Grubberfly BeamL(Clone)", "Grubberfly BeamR(Clone)", "Grubberfly BeamU(Clone)", "Grubberfly BeamD(Clone)", 
            "Damager", "Sharp Shadow", "Knight Dung Trail(Clone)", "Dung Explosion(Clone)", "Knight Spore Cloud(Clone)",
        };
        private void OnTriggerEnter2D(Collider2D collider)
        {
            if (validColliders.Any(@string => collider.name.Contains(@string)))
            {
                FlashWhite(0.25f);
            }
            else if (collider.name == "Hitbox")
            {
                _dreamNailReaction.SetConvoTitle(_dreamNailDialogue[_random.Next(_dreamNailDialogue.Length)]);
                FlashWhite(1.0f);
            }
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
        
        private void Log(object message) => Modding.Logger.Log("[Dryya] " + message);
    }
}