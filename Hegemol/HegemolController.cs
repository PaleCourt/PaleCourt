using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using FiveKnights.BossManagement;
using FiveKnights.Ogrim;
using FrogCore.Ext;
using HutongGames.PlayMaker;
using HutongGames.PlayMaker.Actions;
using JetBrains.Annotations;
using SFCore.Utils;
using UnityEngine;
using GlobalEnums;
using Vasi;
using Random = UnityEngine.Random;

namespace FiveKnights.Hegemol
{
    public class HegemolController : MonoBehaviour
    {
        private const int Health = 800; //1600; //2400; // 800 is 2400/3, did this because of the new phases

        private readonly float LeftX = OWArenaFinder.IsInOverWorld ? 420.7f : 11.2f;
        private readonly float RightX = OWArenaFinder.IsInOverWorld ? 456.0f : 45.7f;
        private readonly float GroundY = 27.4f;
        private float CenterX => (LeftX + RightX) / 2;

        private const float BRLeftX = 60.3f;
        private const float BRRightX = 91.6f;
        private const float BRBottomY = 7.4f;
        private const float BRCenterX = (BRLeftX + BRRightX) / 2;

        
        private const float DigInWalkSpeed = 8.0f;
        private const float IdleTime = 0f;
	    private int phase = 1;

        private GameObject _ogrim;
        private PlayMakerFSM _dd;
        private GameObject _pv;

        private MusicPlayer _ap;
        private MusicPlayer _voice;
        private MusicPlayer _damage;

        private BoxCollider2D _col;
        private HealthManager _hm;
        private Rigidbody2D _rb;
        private Animator _anim;
        private SpriteRenderer _sr;
        private EnemyHitEffectsArmoured _hitFx;

        private Mace _mace;
        private GameObject _hitter;
        private GameObject traitorSlam;

        private bool _attacking;
        private bool _usingGroundPunch = false;

        private bool _grounded;

        private void Awake()
        {
            Log("Hegemol Awake");

            gameObject.name = "Hegemol";
            transform.position = new Vector2(RightX - 7f, GroundY + 3f);
            transform.localScale = 1.5f * new Vector3(-Mathf.Abs(transform.localScale.x), transform.localScale.y,
                    transform.localScale.z);
            AddDamageToGO(gameObject, 2, false);

            _pv = Instantiate(FiveKnights.preloadedGO["PV"], Vector2.down * 10, Quaternion.identity);
            _pv.SetActive(true);
            PlayMakerFSM control = _pv.LocateMyFSM("Control");
            control.RemoveTransition("Pause", "Set Phase HP");

            _ogrim = FiveKnights.preloadedGO["WD"];
            _dd = _ogrim.LocateMyFSM("Dung Defender");

            _col = gameObject.GetComponent<BoxCollider2D>();
            _hm = gameObject.AddComponent<HealthManager>();
            _rb = gameObject.GetComponent<Rigidbody2D>();
            _anim = gameObject.GetComponent<Animator>();
            _sr = gameObject.GetComponent<SpriteRenderer>();
            _hitFx = gameObject.AddComponent<EnemyHitEffectsArmoured>();
            _hitFx.enabled = true;
            _hm.hp = Health;

            On.EnemyHitEffectsArmoured.RecieveHitEffect += OnReceiveHitEffect;
            On.HealthManager.TakeDamage += OnTakeDamage;
			On.HealthManager.Die += HealthManagerDie;
        }

		private IEnumerator Start()
        {
            _sr.enabled = false;
            while (HeroController.instance == null) yield return null;
            yield return new WaitForSeconds(1f);

            GetComponent<EnemyDeathEffects>().SetJournalEntry(FiveKnights.journalentries["Hegemol"]);
            
            GameObject _maceGO = Instantiate(FiveKnights.preloadedGO["Mace"], transform);
            _maceGO.SetActive(false);
            _mace = _maceGO.AddComponent<Mace>();
            AddDamageToGO(_maceGO.transform.Find("Head").gameObject, 2, true);
            AddDamageToGO(_maceGO.transform.Find("Handle").gameObject, 2, true);

            _hitter = gameObject.transform.Find("Hitter").gameObject;
            _hitter.AddComponent<NonBouncer>();
            AddDamageToGO(_hitter, 2, true);

            AssignFields();

            StartCoroutine(IntroGreet());
        }

        private IEnumerator IntroGreet()
        {
            _sr.enabled = true;
            _anim.Play("Arrive");

            _mace.transform.position = new Vector3(transform.position.x - 1f, transform.position.y + 50f, _mace.transform.position.z);
            _mace.transform.localScale = new Vector3(-1f, 1f, 1f);
            _mace.gameObject.SetActive(true);

            yield return new WaitWhile(() => _anim.GetCurrentFrame() < 1);

            GameCameras.instance.cameraShakeFSM.SendEvent("AverageShake");
            if (!OWArenaFinder.IsInOverWorld) MusicControl();
            PlayVoiceClip("HCalm", true, 1f);

            yield return new WaitForSeconds(1f);

            yield return new WaitWhile(() => _anim.IsPlaying());

            _attacking = true;
            yield return IntroAttack();

            StartCoroutine(AttackChoice());
        }

        private IEnumerator IntroAttack()
        {
            Log("Intro Grab");
            _anim.Play("IntroAttack");

            _mace.gameObject.transform.position = transform.position + 50f * Vector3.up;
            _mace.LaunchSpeed = -200f;
            _mace.SpinSpeed = 560f;
            _mace.gameObject.SetActive(true);

            yield return new WaitWhile(() => _anim.GetCurrentFrame() < 3);

            _anim.enabled = false;

            yield return new WaitWhile(() => _mace.transform.position.y > transform.position.y);

            _anim.enabled = true;
            _mace.gameObject.SetActive(false);

            yield return new WaitWhile(() => _anim.GetCurrentFrame() < 6);

            PlayAudioClip(_ap, "HegAttackSwing", 2f);

            yield return new WaitWhile(() => _anim.GetCurrentFrame() < 9);

            PlayAudioClip(_ap, "HegAttackHit", 1f);

            yield return new WaitWhile(() => _anim.IsPlaying());
            yield return IdleTimer();
        }

        private void MusicControl()
        {
            Log("Start Music");
            GGBossManager.Instance.PlayMusic(FiveKnights.Clips["HegemolMusic"], 1f);
        }

        private Func<IEnumerator> prevAtt;
        private Func<IEnumerator> currAtt;
        private Func<IEnumerator> nextAtt;

        private IEnumerator AttackChoice()
        {
            Dictionary<string, List<Func<IEnumerator>>> attacks = new Dictionary<string, List<Func<IEnumerator>>>();
            attacks.Add("Jump", new List<Func<IEnumerator>>()
            {
                Slam, Charge
            });
            attacks.Add("Slam", new List<Func<IEnumerator>>()
            {
                Dig, Jump
            });
            attacks.Add("Dig", new List<Func<IEnumerator>>()
            {
                Slam, Jump
            });
            attacks.Add("Charge", new List<Func<IEnumerator>>()
            {
                Slam, Jump
            });

            // Always start with Charge so he immediately starts doing something
            nextAtt = Charge;
            // Prevent Heg from doing Slam after Charge
            currAtt = Slam;

            while(true)
            {
                yield return new WaitWhile(() => _attacking);
                _attacking = true;

                if(_usingGroundPunch)
                {
                    Log("[Attack] GroundPunch");
                    yield return GroundPunch();
                    continue;
                }

                yield return Turn();

                prevAtt = currAtt;
                currAtt = nextAtt;
                do
                {
                    nextAtt = attacks[currAtt.Method.Name][Random.Range(0, attacks[currAtt.Method.Name].Count)];
                } while(prevAtt.Method.Name == nextAtt.Method.Name);
                
                Log("[Attack] " + currAtt.Method.Name);
                StartCoroutine(currAtt.Invoke());
            }
        }

        private IEnumerator GroundPunch()
        {
            if(_usingGroundPunch)
            {
                _anim.Play("JumpAntic");
                PlayVoiceClip("HCharge1", false, 1f);
                yield return null;

                yield return new WaitWhile(() => _anim.IsPlaying("JumpAntic"));

                float diff = CenterX - gameObject.transform.position.x;
                _rb.gravityScale = 3;
                _rb.velocity = new Vector2(1.5f * diff, 60f);

                _anim.Play("Jump");

                yield return new WaitForSeconds(0.69f);
                yield return new WaitUntil(() => _grounded);

                _anim.Play("Land");

                _rb.velocity = Vector2.zero;
                yield return null;

                yield return new WaitWhile(() => _anim.IsPlaying("Land"));

                _usingGroundPunch = false;
            }
            _anim.Play("PunchAntic");
            _rb.velocity = Vector2.zero;
            yield return null;

            yield return new WaitWhile(() => _anim.IsPlaying("PunchAntic"));

            _mace.LaunchSpeed = 45f;
            _mace.SpinSpeed = 560f * transform.localScale.x;
            _mace.transform.localScale = new Vector3(1f, 1f, 1f);
            _mace.transform.position = new Vector3(transform.position.x - (0.75f * transform.localScale.x), transform.position.y + 6f, _mace.transform.position.z);
            _mace.gameObject.SetActive(true);

            bool right = transform.localScale.x > 0f;
            _anim.speed = 0.9f;
            _anim.Play("Punch");
            for(int i = 0; i < 8; i++)
            {
                if(i == 7)
				{
                    _mace.gameObject.SetActive(false);
                    _mace.gameObject.transform.position = transform.position + 50f * Vector3.up;
                    _mace.LaunchSpeed = -69f;
                    _mace.SpinSpeed = 560f;
                    _mace.gameObject.SetActive(true);
                }

                yield return new WaitWhile(() => _anim.GetCurrentFrame() < 3);
                
                if(_usingGroundPunch)
                {
                    //code to spawn barrels from ceiling
                }

                PlayVoiceClip("HGrunt", true, 1f);
                SpawnPillar(-Mathf.Sign(transform.localScale.x), new Vector2(1.5f, 1f), 7.5f);
                _anim.speed = 0.7f;

                yield return new WaitUntil(() => _anim.GetCurrentFrame() == 0);

                _anim.speed = 0.9f;
                transform.localScale = new Vector3(-transform.localScale.x, transform.localScale.y, transform.localScale.z);
                right = !right;
                if(i == 0) _mace.gameObject.SetActive(false); 
            }

            yield return new WaitWhile(() => _mace.transform.position.y > transform.position.y);

            _anim.speed = 1f;
            _anim.enabled = true;
            _mace.gameObject.SetActive(false);

            yield return new WaitWhile(() => _anim.IsPlaying());

            _anim.Play("Idle");

            yield return new WaitForSeconds(0.75f);

            yield return IdleTimer();
        }

        private IEnumerator Jump()
        {
            bool towards = true;
            float diff;
            if(towards) diff = HeroController.instance.transform.position.x - transform.position.x;
            else diff = (transform.localScale.x > 0f ? LeftX : RightX + transform.position.x) / 2 - transform.position.x;

            _anim.Play("JumpAntic");

            yield return null;
            yield return new WaitWhile(() => _anim.IsPlaying("JumpAntic"));

            _anim.Play("Jump");
            _rb.gravityScale = 3f;
            _rb.velocity = new Vector2(diff, 60f);
            PlayAudioClip(_ap, "HegJump", 2f);

            yield return new WaitForSeconds(0.2f);
            yield return new WaitUntil(() => _grounded);

            _anim.Play("Land");
            GameCameras.instance.cameraShakeFSM.SendEvent("AverageShake");
            _rb.velocity = Vector2.zero;
            SpawnDebris(phase);
            PlayAudioClip(_ap, "HegLand", 1f);
            yield return null;

            yield return new WaitWhile(() => _anim.IsPlaying("Land"));
            yield return IdleTimer();
        }

        private IEnumerator Slam()
        {
            _anim.speed = 1.25f;
            _anim.Play("AttackAntic");
            PlayVoiceClip("HCalm", true, 1f);
            yield return null;

            yield return new WaitWhile(() => _anim.IsPlaying("AttackAntic"));

            _anim.speed = 1f;
            _anim.Play("Attack");
            PlayAudioClip(_ap, "HegAttackSwing", 2f);
            yield return null;

            yield return new WaitWhile(() => _anim.IsPlaying("Attack"));

            SpawnShockwaves(transform.localScale.x > 0f, 7f, 2.5f, 50f, 2);
            SpawnDebris(phase);
            PlayAudioClip(_ap, "HegAttackHit", 1f);

            yield return _anim.PlayBlocking("AttackRecover");
            yield return IdleTimer();
        }

        private IEnumerator Dig()
        {
            _anim.Play("DigAntic");
            yield return null;

            yield return new WaitWhile(() => _anim.GetCurrentFrame() < 3);
            _anim.enabled = false;
            yield return new WaitForSeconds(0.1f);
            _anim.enabled = true;
            PlayAudioClip(_ap, "HegAttackSwing", 1f);
            PlayVoiceClip("HGrunt", true, 1f);

            yield return new WaitWhile(() => _anim.IsPlaying("DigAntic"));

            PlayAudioClip(_ap, "HegAttackHit", 1f);
            _anim.Play("Dig");
            _rb.velocity = 2f * Vector2.right * transform.localScale.x;

            yield return new WaitForSeconds(1f);

            _anim.Play("DigEnd");
            _rb.velocity = Vector2.zero;
            yield return null;
			PlayAudioClip(_ap, "HegAttackSwing", 2f);

            // Debris logic
            Vector2 pos = transform.position + Mathf.Sign(transform.localScale.x) * Vector3.right * 5.5f + 2.6f * Vector3.down;

            for (int i = -2; i < 3; i++)
            {
                float yDiff = 6.5f + i * 2.5f;
                float xDiff = (transform.localScale.x > 0f ? RightX : LeftX) - transform.position.x;
                float t = 0.4f;
                float velx = xDiff / t;
                float vely = yDiff / t + 30f * t;

                GameObject debris = Instantiate(FiveKnights.preloadedGO["Debris"], pos, Quaternion.identity);
                AddDamageToGO(debris, 1, true);
                debris.transform.localScale *= 2f;
                debris.SetActive(false);

                DigDebris dd = debris.AddComponent<DigDebris>();
                dd.vel = new Vector2(velx, vely);
                dd.WallX = transform.localScale.x > 0f ? RightX : LeftX;
                dd.GroundY = GroundY;

                debris.transform.Find("Debris0").gameObject.SetActive(false);
                debris.transform.Find("Debris" + Random.Range(0, 3)).gameObject.SetActive(true);

                debris.SetActive(true);
            }

            yield return new WaitWhile(() => _anim.IsPlaying("DigEnd"));
            yield return IdleTimer();
        }

        private IEnumerator Charge()
		{
            PlayVoiceClip("HCharge1", false, 1f);
            for(int i = 0; i < (phase > 1  && !_usingGroundPunch? 2 : 1); i++)
            {
                if(i == 1) yield return Turn();
                yield return _anim.PlayBlocking("RunAntic");
                if(i == 0) yield return new WaitForSeconds(0.3f);

                _anim.Play("Run");
                _anim.speed = 0.75f;
                _rb.velocity = new Vector2(35f * Mathf.Sign(transform.localScale.x), 0f);

                yield return new WaitUntil(() => CheckTerrain(transform.localScale.x * Vector2.right));

                _anim.speed = 1f;
                if(i == 0 || phase == 1) PlayVoiceClip("HGrunt", true, 1f);
                else PlayVoiceClip("HCharge2", false, 1f);
                PlayAudioClip(_ap, "HegShockwave", 1f);
                _anim.Play("Jump");
                _rb.gravityScale = 1.5f;
                _rb.velocity = new Vector2(-7.5f * Mathf.Sign(transform.localScale.x), 25f);
                SpawnDebris(i);

                yield return new WaitForSeconds(0.1f);
                yield return new WaitUntil(() => _grounded);

                _rb.velocity = Vector2.zero;
                SpawnShockwaves(true, 0f, 1.5f, 40f, 2);
                SpawnShockwaves(false, 0f, 1.5f, 40f, 2);
                GameCameras.instance.cameraShakeFSM.SendEvent("AverageShake");
                PlayAudioClip(_ap, "HegLand", 1f);
            }
            _anim.Play("Land");
            yield return null;

            yield return new WaitWhile(() => _anim.IsPlaying("Land"));

            yield return IdleTimer();
        }

        private IEnumerator DungWave()
        {
            Transform trans = transform;
            Vector2 pos = trans.position;
            float scaleX = trans.localScale.x;
            float xLeft = pos.x + 5 * scaleX - 2;
            float xRight = pos.x + 5 * scaleX + 2;
            float pillarSpacing = 2;
            while(xLeft >= LeftX || xRight <= RightX)
            {
                //_audio.Play("Dung Pillar", 0.9f, 1.1f);

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
     
        private IEnumerator DungSide(bool right)
        {
            Transform trans = transform;
            Vector2 pos = trans.position;
            float scaleX = trans.localScale.x;
            float x = pos.x + 5 * scaleX + (right ? 2 : -2);
            float pillarSpacing = 2;
            float xMaxMin = right ? RightX : LeftX;
            while(right ? x <= xMaxMin : x >= xMaxMin)
            {
                //_audio.Play("Dung Pillar", 0.9f, 1.1f);

                GameObject dungPillar = Instantiate(FiveKnights.preloadedGO["pillar"], new Vector2(x, 12.0f), Quaternion.identity);
                dungPillar.SetActive(true);
                dungPillar.AddComponent<DungPillar>();
                if(!right)
                {
                    Vector3 pillarScale = dungPillar.transform.localScale;
                    dungPillar.transform.localScale = new Vector3(-pillarScale.x, pillarScale.y, pillarScale.z);
                }

                x -= pillarSpacing;

                yield return new WaitForSeconds(0.1f);
            }
        }

        private void SpawnShockwaves(bool facingRight, float offset, float scale, float speed, int damage)
        {
            Vector2 pos = transform.position;

            PlayMakerFSM fsm = FiveKnights.preloadedGO["Mage"].LocateMyFSM("Mage Lord");

            GameObject shockwave = Instantiate(fsm.GetAction<SpawnObjectFromGlobalPool>("Quake Waves", 0).gameObject.Value);

            PlayMakerFSM shockFSM = shockwave.LocateMyFSM("shockwave");

            shockFSM.FsmVariables.FindFsmBool("Facing Right").Value = facingRight;
            shockFSM.FsmVariables.FindFsmFloat("Speed").Value = speed;
            shockwave.AddComponent<DamageHero>().damageDealt = damage;
            shockwave.SetActive(true);

            shockwave.transform.SetPosition2D(new Vector2(pos.x + (facingRight ? 1 : -1) * offset, 
                GroundY - 1.4f));
            shockwave.transform.SetScaleX(scale);
        }

        private void SpawnPillar(float dir, Vector2 size, float xSpd)
        {
            PlayAudioClip(_ap, "HegShockwave", 1f);

            GameObject slam = Instantiate(traitorSlam);
            Animator anim = slam.transform.Find("slash_core").GetComponent<Animator>();
            slam.SetActive(true);
            anim.enabled = true;
            anim.Play("mega_mantis_slash_big", -1, 0f);
            Rigidbody2D rb = slam.GetComponent<Rigidbody2D>();

            rb.velocity = new Vector2(-dir * xSpd, 0f);
            Vector3 pos = transform.position;
            slam.transform.position = new Vector3(-dir * 2.15f + pos.x, GroundY - 3.2f, 6.4f);
            slam.transform.localScale = new Vector3(-dir * size.x, size.y, 1f);
            if(slam.transform.Find("slash_core").Find("hurtbox") != null)
            {
                slam.transform.Find("slash_core").Find("hurtbox").gameObject.SetActive(false);
            }

            var pc = slam.transform.Find("slash_core").Find("Test2").GetComponent<PolygonCollider2D>();
            var off = pc.offset;
            off.y = slam.transform.Find("slash_core").Find("hurtbox").GetComponent<PolygonCollider2D>().offset.y - 10f;
        }

        private void SpawnDebris(int amount)
        {
            for(int i = 0; i < amount; i++)
            {
                Vector2 pos = new Vector2(Random.Range(LeftX + 2f, RightX - 2f), GroundY + 15f);
                GameObject debris = Instantiate(FiveKnights.preloadedGO["Debris"], pos, Quaternion.identity);
                AddDamageToGO(debris, 1, true);
                debris.transform.localScale *= 2f;
                debris.SetActive(false);

                Debris deb = debris.AddComponent<Debris>();
                deb.delay = Random.Range(0f, 1f);
                deb.vel = 15f * Vector2.down;
                deb.GroundY = GroundY;

                debris.transform.Find("Debris0").gameObject.SetActive(false);
                debris.transform.Find("Debris" + Random.Range(0, 3)).gameObject.SetActive(true);

                debris.SetActive(true);
            }
        }

        private IEnumerator Turn()
        {
            float diff = HeroController.instance.transform.position.x - transform.position.x;
            if(Mathf.Sign(diff) * Mathf.Sign(transform.localScale.x) < 0)
            {
                transform.localScale = new Vector3(-transform.localScale.x, transform.localScale.y,
                    transform.localScale.z);
                yield return _anim.PlayBlocking("Turn");
            }
        }

        private void Update()
		{
            if(_hm.hp <= 0 && phase <= 3)
            {
                phase++;
                if(phase > 3)
				{
                    StopAllCoroutines();
                    StartCoroutine(Die());
                    return;
                }
                _hm.hp = Health;
                _usingGroundPunch = true;
                Log("Going to phase " + phase);
            }
        }

        private IEnumerator FlashWhite()
		{
            _sr.material.SetFloat("_FlashAmount", 1f);
            yield return null;
            for(float i = 1f; i >= 0f; i -= 0.05f)
            {
                _sr.material.SetFloat("_FlashAmount", i);
                yield return null;
            }
            yield return null;
        }

        private void OnReceiveHitEffect(On.EnemyHitEffectsArmoured.orig_RecieveHitEffect orig, EnemyHitEffectsArmoured self, float attackDirection)
        {
            if(self.gameObject.name == "Hegemol")
			{
                StartCoroutine(FlashWhite());
                PlayAudioClip(_damage, "HegDamage", 1f);
                return;
            }
            orig(self, attackDirection);
        }

        private void OnTakeDamage(On.HealthManager.orig_TakeDamage orig, HealthManager self, HitInstance hitInstance)
        {
            if(self.gameObject.name == "Hegemol") _hitFx.RecieveHitEffect(hitInstance.Direction);
            orig(self, hitInstance);
        }

        private void HealthManagerDie(On.HealthManager.orig_Die orig, HealthManager self, float? attackDirection, AttackTypes attackType, bool ignoreEvasion)
        {
            if(self.gameObject.name == "Hegemol" && phase < 3) return;
            orig(self, attackDirection, attackType, ignoreEvasion);
        }

        private IEnumerator Die()
        {
            Log("Hegemol Death");

            GGBossManager.Instance.PlayMusic(null, 1f);
            CustomWP.wonLastFight = true;
            _sr.material.SetFloat("_FlashAmount", 0f);
            _rb.velocity = Vector2.zero;
            _anim.speed = 1f;
            _anim.Play("Stagger");
            PlayAudioClip(_ap, "HegDamageFinal", 1f);
            PlayVoiceClip("HDeath", false, 1f);

            yield return new WaitWhile(() => _anim.GetCurrentFrame() < 4);
            _anim.enabled = false;

            yield return new WaitForSeconds(1f);

            PlayVoiceClip("HCalm", true, 1f);

            yield return new WaitForSeconds(2f);

            GetComponent<EnemyDeathEffects>().RecordJournalEntry();
	    
            Destroy(this);
        }
        
        private void AssignFields()
        {
            HealthManager ogrimHealth = _ogrim.GetComponent<HealthManager>();
            foreach (FieldInfo fi in typeof(HealthManager).GetFields(BindingFlags.Instance | BindingFlags.NonPublic)
                .Where(x => x.Name.Contains("Prefab")))
            {
                fi.SetValue(_hm, fi.GetValue(ogrimHealth));
            }

            PlayMakerFSM spellControl = HeroController.instance.gameObject.LocateMyFSM("Spell Control");
            GameObject fireballParent = spellControl.GetAction<SpawnObjectFromGlobalPool>("Fireball 2", 3).gameObject.Value;
            PlayMakerFSM fireballCast = fireballParent.LocateMyFSM("Fireball Cast");
            GameObject actor = fireballCast.GetAction<AudioPlayerOneShotSingle>("Cast Right", 3).audioPlayer.Value;
            _ap = new MusicPlayer
            {
                Volume = 1f,
                Player = actor,
                MaxPitch = 1f,
                MinPitch = 1f,
                Spawn = gameObject
            };
            _voice = new MusicPlayer
            {
                Volume = 1f,
                Player = actor,
                MaxPitch = 1f,
                MinPitch = 1f,
                Spawn = gameObject
            };
            _damage = new MusicPlayer
            {
                Volume = 1f,
                Player = actor,
                MaxPitch = 1f,
                MinPitch = 1f,
                Spawn = gameObject
            };

            traitorSlam = Instantiate(FiveKnights.preloadedGO["TraitorSlam"]);
            traitorSlam.transform.Find("slash_core").Find("hurtbox").GetComponent<DamageHero>().damageDealt = 2;
            var old = traitorSlam.transform.Find("slash_core").Find("hurtbox");
            var cp = Instantiate(old);
            cp.name = "Test2";
            cp.parent = traitorSlam.transform.Find("slash_core").transform;
            cp.transform.position = old.transform.position;
            cp.transform.localScale = old.transform.localScale;
            old.gameObject.SetActive(false);
            traitorSlam.SetActive(false);
        }

        private void AddDamageToGO(GameObject go, int damage, bool isAttack)
		{
            go.layer = isAttack ? (int)PhysLayers.ENEMY_ATTACK : (int)PhysLayers.ENEMIES;
            DamageHero dh = go.AddComponent<DamageHero>();
            dh.damageDealt = damage;
            dh.hazardType = (int)HazardType.SPIKES;
            dh.shadowDashHazard = false;
		}

        private IEnumerator IdleTimer()
        {
            Log("[Idle]");
            _anim.Play("Idle");
            yield return new WaitForSeconds(IdleTime);
            _attacking = false;
        }

        private void PlayVoiceClip(string clip, bool random, float volume)
		{
            int num = 0;
            if(random)
			{
                switch(clip)
				{
                    case "HCalm":
                        num = Random.Range(1, 3);
                        break;
                    case "HGrunt":
                        num = Random.Range(1, 5);
                        break;
                    case "HTired":
                        num = Random.Range(1, 3);
                        break;
                    default:
                        num = 0;
                        break;
				}
			}
            if(num != 0) clip += num;
            PlayAudioClip(_voice, clip, volume);
		}

        private void PlayAudioClip(MusicPlayer ap, string clip, float volume)
        {
            ap.Clip = FiveKnights.Clips[clip];
            ap.Volume = volume;
            ap.DoPlayRandomClip();
        }

        private bool CheckTerrain(Vector3 dir)
		{
            return Physics2D.BoxCast(_col.bounds.center, _col.bounds.size, 0f,
                dir, 0.1f, 256);
        }

        private void FixedUpdate()
		{
            _grounded = CheckTerrain(Vector3.down);
		}

        private void OnDestroy()
        {
            On.EnemyHitEffectsArmoured.RecieveHitEffect -= OnReceiveHitEffect;
            On.HealthManager.TakeDamage -= OnTakeDamage;
            On.HealthManager.Die -= HealthManagerDie;
        }

        private void Log(object message)
        {
            Modding.Logger.Log("[Hegemol Controller] " + message);
        }
    }
}
