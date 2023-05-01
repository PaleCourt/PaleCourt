using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using FiveKnights.BossManagement;
using FrogCore.Ext;
using HutongGames.PlayMaker.Actions;
using UnityEngine;
using GlobalEnums;
using Random = UnityEngine.Random;

namespace FiveKnights.Hegemol
{
    public class HegemolController : MonoBehaviour
    {
        private int Health => phase == 1 ? 650 : (phase == 2 ? 700 : 850);

        private readonly float LeftX = OWArenaFinder.IsInOverWorld ? 421.6f : 
            (CustomWP.boss == CustomWP.Boss.All || CustomWP.boss == CustomWP.Boss.Ogrim ? 60.3f : 11.2f);
        private readonly float RightX = OWArenaFinder.IsInOverWorld ? 456.0f :
            (CustomWP.boss == CustomWP.Boss.All || CustomWP.boss == CustomWP.Boss.Ogrim ? 91.7f : 45.7f);
        private readonly float GroundY = OWArenaFinder.IsInOverWorld ? 155.2f :
            (CustomWP.boss == CustomWP.Boss.All || CustomWP.boss == CustomWP.Boss.Ogrim ? 7.3f : 27.5f);
        private float CenterX => (LeftX + RightX) / 2;
        private float Width => RightX - LeftX;

        private const float IdleTime = 0f;
	    private int phase = 1;

        private GameObject _ogrim;

        private DamageHero _dh;
        private BoxCollider2D _col;
        private HealthManager _hm;
        private Rigidbody2D _rb;
        private Animator _anim;
        private SpriteRenderer _sr;
        private ExtraDamageable _xd;
        private Flash _flash;
        private EnemyHitEffectsArmoured _hitFx;

        private Mace _mace;
        private GameObject _hitter;
        private GameObject _traitorSlam;

        private bool _attacking;
        private bool _usingGroundPunch = false;

        private bool _grounded;

        private void Awake()
        {
            Log("Hegemol Awake");

            gameObject.name = "Hegemol";
            gameObject.layer = (int)PhysLayers.CORPSE;
            transform.position = new Vector3(RightX - 5f, GroundY + 3f, 0.01f);
            transform.localScale = 1.5f * new Vector3(-Mathf.Abs(transform.localScale.x), transform.localScale.y,
                    transform.localScale.z);

            _ogrim = FiveKnights.preloadedGO["WD"];

            _col = gameObject.GetComponent<BoxCollider2D>();
            _hm = gameObject.AddComponent<HealthManager>();
            _rb = gameObject.GetComponent<Rigidbody2D>();
            _anim = gameObject.GetComponent<Animator>();
            _sr = gameObject.GetComponent<SpriteRenderer>();
            _xd = gameObject.AddComponent<ExtraDamageable>();
            _flash = gameObject.AddComponent<Flash>();
            _flash.enabled = true;
            _hitFx = gameObject.AddComponent<EnemyHitEffectsArmoured>();
            _hitFx.enabled = true;
            _hm.hp = 850;

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
            _hm.hp = Health;

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

            _dh = AddDamageToGO(gameObject, 2, false);
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

            yield return new WaitWhile(() => _mace.transform.position.y > transform.position.y + 3f);

            _anim.enabled = true;
            _mace.gameObject.SetActive(false);

            yield return new WaitWhile(() => _anim.GetCurrentFrame() < 6);

            PlayAudioClip("HegAttackSwing", 2f);

            yield return new WaitWhile(() => _anim.GetCurrentFrame() < 9);

            PlayAudioClip("HegAttackHit", 1f);

            yield return new WaitWhile(() => _anim.IsPlaying());

            yield return Turn();
            currAtt = Charge;
            Log("[Attack] " + currAtt.Method.Name);
            StartCoroutine(currAtt.Invoke());
        }

        private void MusicControl()
        {
            Log("Start Music");
            GGBossManager.Instance.PlayMusic(FiveKnights.Clips["HegemolMusic"], 1f);
        }

        private Func<IEnumerator> prevAtt2;
        private Func<IEnumerator> prevAtt;
        private Func<IEnumerator> currAtt;

        private IEnumerator AttackChoice()
        {
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

                prevAtt2 = prevAtt;
                prevAtt = currAtt;

                int[] weights = new int[6];
                List<Func<IEnumerator>> attacks = new List<Func<IEnumerator>>() { Jump, Slam, Dig, Charge, MightySlam, MightyDig };

                float distToWall = Math.Abs(transform.position.x - (transform.localScale.x > 0f ? RightX : LeftX));

                weights[0] += 2;
                if(distToWall < (Width / 3f) + 2f)
				{
                    weights[3] += 4;
                    if(phase > 1) weights[4] += 2;
                    if(phase > 2) weights[5] += 2;
				}
                else if(distToWall < 2 * Width / 2f)
				{
                    weights[1]++;
                    weights[2]++;
                    weights[3]++;
                    if(phase > 1) weights[4]++;
                    if(phase > 2) weights[5]++;
				}
                else
				{
                    weights[1] += 2;
                    weights[2] += 2;
                    if(phase > 1) weights[4]++;
                    if(phase > 2) weights[5]++;
                }
                if(prevAtt != null)
                {
                    weights[attacks.IndexOf(prevAtt)] = 0;
                    if(prevAtt == Jump)
					{
                        weights[4] = 0;
                        weights[5] = 0;
                        if(prevAtt2 != null && prevAtt2 == Dig) weights[1]++;
					}
                    if(prevAtt == Dig)
					{
                        weights[1] = 0;
					}
                }
                if(prevAtt2 != null) weights[attacks.IndexOf(prevAtt2)]--;

                List<Func<IEnumerator>> attackTable = new List<Func<IEnumerator>>();

                for(int i = 0; i < weights.Length; i++)
                {
                    for(int j = 0; j < weights[i]; j++)
                    {
                        attackTable.Add(attacks[i]);
                    }
                }

                currAtt = attackTable[Random.Range(0, attackTable.Count)];

                Log("[Attack] " + currAtt.Method.Name);
                StartCoroutine(currAtt.Invoke());
            }
        }

        private IEnumerator GroundPunch()
        {
            if(_usingGroundPunch)
            {
                _anim.Play("JumpAntic");
                PlayVoiceClip("HCharge", false, 1f);
                yield return null;

                yield return new WaitWhile(() => _anim.IsPlaying("JumpAntic"));

                float diff = CenterX - gameObject.transform.position.x;
                _rb.gravityScale = 3;
                _rb.velocity = new Vector2(1.5f * diff, 60f);

                _anim.Play("Jump");

                yield return new WaitForSeconds(0.2f);
                yield return new WaitUntil(() => _grounded);

                _anim.Play("Land");

                _rb.velocity = Vector2.zero;
                yield return null;

                yield return new WaitWhile(() => _anim.IsPlaying("Land"));

                _usingGroundPunch = false;
            }
            yield return Turn();

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
            _anim.Play("Punch");
            for(int i = 0; i < 8; i++)
            {
                if(i == 7)
				{
                    _mace.gameObject.SetActive(false);
                    _mace.gameObject.transform.position = transform.position + 70f * Vector3.up;
                    _mace.LaunchSpeed = -69f;
                    _mace.SpinSpeed = 560f;
                    _mace.gameObject.SetActive(true);
                }

                yield return new WaitWhile(() => _anim.GetCurrentFrame() < 3);

                int debrisAmount = phase == 3 ? 1 + i % 2 : 1;
                PlayVoiceClip("HGrunt", true, 1f);
                PlayAudioClip("HegAttackHit", 1f);
                SpawnShockwaves(transform.localScale.x > 0f, 4f, 2.5f, 35f, 1);
                StartCoroutine(SpawnDebris(debrisAmount, false, 0f));

                yield return new WaitUntil(() => _anim.GetCurrentFrame() == 0);

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
            PlayAudioClip("HegJump", 2f);

            yield return new WaitForSeconds(0.2f);
            yield return new WaitUntil(() => _grounded);

            _anim.Play("Land");
            GameCameras.instance.cameraShakeFSM.SendEvent("AverageShake");
            _rb.velocity = Vector2.zero;
            PlayAudioClip("HegLand", 1f);
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
            
            // If past phase 1, has a chance to do a fakeout instead
            _anim.speed = 1f;
            if(phase > 1 && Random.Range(0, 2) == 1)
			{
                // Jump for the fakeout
                float diff = HeroController.instance.transform.position.x - transform.position.x;

                _anim.Play("JumpAntic");
                yield return null;

                yield return new WaitWhile(() => _anim.IsPlaying("JumpAntic"));

                _anim.Play("JumpAttackUp");
                _rb.gravityScale = 3f;
                _rb.velocity = new Vector2(diff, 60f);
                PlayAudioClip("HegJump", 2f);

                // Wait for him to start going down
                yield return new WaitForSeconds(0.2f);
                yield return new WaitWhile(() => _rb.velocity.y > 0f);
                yield return new WaitUntil(() => CheckTerrain(Vector3.down, 8f));

                // Start playing his jump attack animation
                _anim.Play("JumpAttackHit");
                PlayAudioClip("HegAttackSwing", 2f);
                transform.position -= 0.5f * Vector3.down;
                yield return null;
                _anim.enabled = false;

                // Step forward the animation frames to be on the right frame when he lands
                yield return new WaitUntil(() => CheckTerrain(Vector3.down, 5f));

                _anim.enabled = true;

                yield return new WaitWhile(() => _anim.GetCurrentFrame() < 1);

                _anim.enabled = false;

                // Landed, continue playing the rest of the animation, spawn shockwaves, etc.
                yield return new WaitUntil(() => _grounded);

                _rb.velocity = new Vector2(0f, _rb.velocity.y);
                _anim.enabled = true;

                yield return new WaitWhile(() => _anim.GetCurrentFrame() < 2);

                SpawnShockwaves(transform.localScale.x > 0f, 7f, 2.5f, 35f, 1);
                StartCoroutine(SpawnDebris(phase, true, transform.position.x));
                PlayAudioClip("HegAttackHit", 1f);
                GameCameras.instance.cameraShakeFSM.SendEvent("AverageShake");
                _rb.velocity = Vector2.zero;
                PlayAudioClip("HegLand", 1f);

                yield return new WaitWhile(() => _anim.IsPlaying("JumpAttackHit"));
            }
            else
			{
                // Non fakeout, just swing and spawn shockwaves
                _anim.Play("Attack");
                PlayAudioClip("HegAttackSwing", 2f);
                yield return null;

                yield return new WaitWhile(() => _anim.IsPlaying("Attack"));

                SpawnShockwaves(transform.localScale.x > 0f, 7f, 2.5f, 35f, 1);
                StartCoroutine(SpawnDebris(phase, true, transform.position.x));
                PlayAudioClip("HegAttackHit", 1f);
                GameCameras.instance.cameraShakeFSM.SendEvent("AverageShake");

                yield return _anim.PlayBlocking("AttackRecover");
            }
            yield return IdleTimer();
        }

        private IEnumerator Dig()
        {
            // Play antic for dig
            _anim.Play("DigAntic");
            yield return null;

            yield return new WaitWhile(() => _anim.GetCurrentFrame() < 3);

            _anim.enabled = false;

            yield return new WaitForSeconds(0.05f);

            _anim.enabled = true;
            PlayAudioClip("HegAttackSwing", 1f);
            PlayVoiceClip("HGrunt", true, 1f);

            yield return new WaitWhile(() => _anim.IsPlaying("DigAntic"));

            // Mace hit ground, start walking forward
            PlayAudioClip("HegAttackHit", 1f);
            GameCameras.instance.cameraShakeFSM.SendEvent("AverageShake");
            _anim.Play("Dig");
            _rb.velocity = 2f * Vector2.right * transform.localScale.x;

            yield return new WaitForSeconds(0.4f);

            // Mace leaves ground, spawn shockwaves and debris
            _anim.Play("DigEnd");
            _rb.velocity = Vector2.zero;
            yield return null;
			PlayAudioClip("HegAttackSwing", 2f);
            SpawnShockwaves(transform.localScale.x > 0f, 5f, 1.5f, 15f, 1);

            // Debris logic
            Vector2 pos = transform.position + Mathf.Sign(transform.localScale.x) * Vector3.right * 5.5f + 2.6f * Vector3.down;
            for(int i = -2; i < 3; i++)
			{
                float targetX = HeroController.instance.transform.position.x + Mathf.Sign(transform.localScale.x) * 10f * i;
                Vector2 diff = new Vector2(targetX, GroundY) - pos;

                Vector2 vel = Vector2.zero;
                float t = 1f;
                vel.x = diff.x / t;
                vel.y = diff.y / t + 45f * t;

                GameObject debris = Instantiate(FiveKnights.preloadedGO["Debris"], pos, Quaternion.identity);
                AddDamageToGO(debris, 1, true);
                debris.SetActive(false);

                Debris deb = debris.AddComponent<Debris>();
                deb.gravityScale = 1.5f;
                deb.vel = vel;
                deb.GroundY = GroundY;
                deb.type = CustomWP.boss == CustomWP.Boss.All ? Debris.DebrisType.DUNG : Debris.DebrisType.NORMAL;

                debris.SetActive(true);
            }

            yield return new WaitWhile(() => _anim.IsPlaying("DigEnd"));
            yield return IdleTimer();
        }

        private IEnumerator Charge()
		{
            PlayVoiceClip("HCharge", false, 1f);
            for(int i = 0; i < phase; i++)
            {
                // Turn towards the player first, then play antic
                if(i > 0) yield return Turn();
                yield return _anim.PlayBlocking("RunAntic");
                if(i == 0) yield return new WaitForSeconds(0.3f);

                // GAS GAS GAS
                _anim.Play("Run");
                _anim.speed = 0.75f;
                _rb.velocity = new Vector2(30f * Mathf.Sign(transform.localScale.x), 0f);

                // Wait until he hits the wall
                yield return new WaitUntil(() => CheckTerrain(transform.localScale.x * Vector2.right, 0.1f));

                // Make him bounce off the wall a bit
                _anim.speed = 1f;
                if(i == phase - 1) PlayVoiceClip("HHeavy", true, 1f);
                else PlayVoiceClip("HGrunt", true, 1f);
                PlayAudioClip("HegShockwave", 1f);
                _anim.Play("Jump");
                _rb.gravityScale = 1.5f;
                _rb.velocity = new Vector2(-7.5f * Mathf.Sign(transform.localScale.x), 25f);

                yield return new WaitForSeconds(0.1f);
                yield return new WaitUntil(() => _grounded);

                // Spawn shockwaves upon landing if it's past phase 1 and it's the final charge
                _rb.velocity = Vector2.zero;
                if(i == phase - 1)
                {
                    SpawnShockwaves(true, 0f, 1.5f, 20f, 1);
                    SpawnShockwaves(false, 0f, 1.5f, 20f, 1);
                }

                // Spawn debris that targets the other side of the arena from where he is
                StartCoroutine(SpawnDebris(i == phase - 1 ? 2 : 1, true, transform.position.x < CenterX ? RightX : LeftX));

                GameCameras.instance.cameraShakeFSM.SendEvent("AverageShake");
                PlayAudioClip("HegLand", 1f);
            }
            _anim.Play("Land");
            yield return null;

            yield return new WaitWhile(() => _anim.IsPlaying("Land"));

            yield return IdleTimer();
        }

        private IEnumerator MightySlam()
		{
            yield return JumpBack();

            _anim.Play("AttackAntic");
            PlayVoiceClip("HCharge", false, 1f);
            PlayAudioClip("HegAttackCharge", 1f);
            yield return null;

            yield return new WaitWhile(() => _anim.IsPlaying("AttackAntic"));

            _anim.Play("AttackAnticLoop");

            yield return new WaitForSeconds(0.7f);

            _anim.Play("Attack");
            PlayVoiceClip("HGrunt", true, 1f);
            PlayAudioClip("HegAttackSwing", 2f);
            yield return null;

            yield return new WaitWhile(() => _anim.IsPlaying("Attack"));

            SpawnPillar(-Mathf.Sign(transform.localScale.x), new Vector2(2f, 1f), 15f);
            StartCoroutine(SpawnDebris(3, true, transform.position.x));
            PlayAudioClip("HegAttackHit", 1f);
            GameCameras.instance.cameraShakeFSM.SendEvent("AverageShake");

            yield return _anim.PlayBlocking("AttackRecover");
            yield return IdleTimer();
        }

        private IEnumerator MightyDig()
		{
            yield return JumpBack();

            _anim.Play("DigAntic");
            yield return null;

            yield return new WaitWhile(() => _anim.GetCurrentFrame() < 3);

            _anim.enabled = false;

            yield return new WaitForSeconds(0.05f);

            _anim.enabled = true;
            PlayAudioClip("HegAttackSwing", 1f);
            PlayVoiceClip("HGrunt", true, 1f);

            yield return new WaitWhile(() => _anim.IsPlaying("DigAntic"));

            PlayAudioClip("HegAttackHit", 1f);
            GameCameras.instance.cameraShakeFSM.SendEvent("AverageShake");
            _anim.Play("Dig");
            _rb.velocity = 2f * Vector2.right * transform.localScale.x;

            yield return new WaitForSeconds(0.2f);

            _anim.Play("DigEnd");
            _rb.velocity = Vector2.zero;
            yield return null;
            PlayAudioClip("HegAttackSwing", 2f);

            // Send debris up
            Vector2 pos = transform.position + Mathf.Sign(transform.localScale.x) * Vector3.right * 5.5f + 2.6f * Vector3.down;

            Debris[] debrisArr = new Debris[5];
            for(int i = -2; i < 3; i++)
            {
                float targetX = pos.x + Mathf.Sign(transform.localScale.x) * 1f * i;
                Vector2 diff = new Vector2(targetX, transform.position.y + 10f) - pos;

                Vector2 vel = Vector2.zero;
                float t = 0.8f;
                vel.x = diff.x / t;
                vel.y = diff.y / t + 45f * t;

                GameObject debris = Instantiate(FiveKnights.preloadedGO["Debris"], pos, Quaternion.identity);
                AddDamageToGO(debris, 1, true);
                debris.SetActive(false);

                Debris deb = debris.AddComponent<Debris>();
                deb.gravityScale = 1.5f;
                deb.vel = vel;
                deb.GroundY = GroundY;
                deb.type = CustomWP.boss == CustomWP.Boss.All ? Debris.DebrisType.DUNG : Debris.DebrisType.NORMAL;
                debrisArr[i + 2] = deb;

                debris.SetActive(true);
            }

            yield return new WaitWhile(() => _anim.IsPlaying("DigEnd"));

            _anim.Play("JumpAntic");
            PlayVoiceClip("HCharge", false, 1f);

            yield return null;
            yield return new WaitWhile(() => _anim.IsPlaying("JumpAntic"));

            _anim.Play("JumpAttackUp");
            _rb.gravityScale = 3f;
            _rb.velocity = new Vector2(0f, 60f);
            PlayAudioClip("HegJump", 2f);

            yield return new WaitForSeconds(0.2f);
            yield return new WaitWhile(() => _rb.velocity.y > 0f);

            _anim.Play("JumpAttackHit");
            PlayAudioClip("HegAttackSwing", 2f);
            PlayVoiceClip("HHeavy", true, 1f);

            yield return new WaitWhile(() => _anim.GetCurrentFrame() < 2);

            // Redirect debris
            for(int i = -2; i < 3; i++)
			{
                Debris deb = debrisArr[i + 2];

                float targetX = transform.localScale.x > 0f ? (RightX + CenterX) / 2 : (LeftX + CenterX) / 2;
                Vector2 target = new Vector2(targetX + 4f * i, GroundY);
                Vector2 vel = target - (Vector2)deb.transform.position;

                deb.rb.velocity = 50f * vel.normalized;
                deb.rb.gravityScale = 0f;
			}
            _rb.velocity = Vector2.zero;
            _rb.gravityScale = 0f;

            yield return new WaitWhile(() => _anim.GetCurrentFrame() < 3);

            _rb.gravityScale = 3f;
            _anim.Play("Jump");
            yield return null;

            yield return new WaitUntil(() => _grounded);

            _anim.Play("Land");
            _rb.velocity = Vector2.zero;
            SpawnShockwaves(true, 0f, 1.5f, 40f, 1);
            SpawnShockwaves(false, 0f, 1.5f, 40f, 1);
            GameCameras.instance.cameraShakeFSM.SendEvent("AverageShake");
            PlayAudioClip("HegLand", 1f);
            yield return null;

            yield return new WaitWhile(() => _anim.IsPlaying("Land"));
            yield return IdleTimer();
        }

        private IEnumerator JumpBack()
		{
            float diff = ((transform.localScale.x > 0f ? LeftX : RightX) + transform.position.x) / 2 - transform.position.x;

            _anim.Play("JumpAntic");
            yield return null;

            yield return new WaitWhile(() => _anim.IsPlaying("JumpAntic"));

            _anim.Play("Jump");
            _rb.gravityScale = 3f;
            _rb.velocity = new Vector2(diff, 60f);
            PlayAudioClip("HegJump", 2f);

            yield return new WaitForSeconds(0.2f);
            yield return new WaitUntil(() => _grounded);

            _anim.Play("Land");
            GameCameras.instance.cameraShakeFSM.SendEvent("AverageShake");
            _rb.velocity = Vector2.zero;
            PlayAudioClip("HegLand", 1f);
            yield return null;

            yield return new WaitWhile(() => _anim.IsPlaying("Land"));
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
            PlayAudioClip("HegShockwave", 1f);

            GameObject slam = Instantiate(_traitorSlam);
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

        private IEnumerator SpawnDebris(int amount, bool useLogic, float targetX)
        {
            for(int i = 0; i < amount; i++)
            {
                float minX = LeftX;
                float maxX = RightX;
                if(!useLogic)
                {
                    minX += 2f;
                    maxX -= 2f;
                }
                else
				{
                    if(targetX > CenterX) minX = CenterX;
                    else maxX = CenterX;
				}

                Vector2 pos = new Vector2(Random.Range(minX, maxX), GroundY + 15f);
                GameObject debris = Instantiate(FiveKnights.preloadedGO["Debris"], pos, Quaternion.identity);
                AddDamageToGO(debris, 1, true);
                debris.SetActive(false);

                Debris deb = debris.AddComponent<Debris>();
                deb.gravityScale = 0f;
                deb.vel = 10f * Vector2.down;
                deb.GroundY = GroundY;
                deb.type = CustomWP.boss == CustomWP.Boss.All ? Debris.DebrisType.CC : Debris.DebrisType.NORMAL;

                debris.SetActive(true);
                yield return new WaitForSeconds(0.5f);
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

        private bool _staggered;

        private IEnumerator Stagger()
		{
            Log("Staggered");
            _hm.hp = Health;

            _anim.enabled = true;
            _anim.speed = 1f;
            _anim.Play("Stagger");
            _sr.material.SetFloat("_FlashAmount", 0f);
            Destroy(_dh);
            PlayAudioClip("HegDamageFinal", 1f);
            PlayVoiceClip("HHeavy", true, 1f);

            float dir = Mathf.Sign(transform.position.x - HeroController.instance.transform.position.x);
            _rb.gravityScale = 1.5f;
            _rb.velocity = new Vector2(7.5f * dir, 25f);

            yield return null;

            _anim.enabled = false;

            yield return new WaitForSeconds(0.1f);
            while(!_grounded)
			{
                yield return new WaitUntil(() => _grounded || CheckTerrain(transform.localScale.x * Vector3.right, 0.1f));
                if(CheckTerrain(transform.localScale.x * Vector3.right, 0.1f))
				{
                    _rb.velocity = new Vector2(-_rb.velocity.x, _rb.velocity.y);
				}
            }

            _anim.enabled = true;
            _rb.velocity = Vector2.zero;
            GameCameras.instance.cameraShakeFSM.SendEvent("AverageShake");
            PlayAudioClip("HegLand", 1f);

            yield return new WaitWhile(() => _anim.GetCurrentFrame() < 4);
            _anim.enabled = false;
            _staggered = true;
            _hm.IsInvincible = false;

            yield return new WaitForSeconds(1f);

            yield return StaggerEnd();
        }

        private IEnumerator StaggerEnd()
		{
            PlayVoiceClip("HCalm", true, 1f);
            _anim.enabled = true;

            yield return new WaitWhile(() => _anim.GetCurrentFrame() < 10);

            _anim.Play("Idle");

            _rb.gravityScale = 3f;
            _dh = AddDamageToGO(gameObject, 2, false);
            _usingGroundPunch = true;
            _attacking = false;
            _staggered = false;
            StartCoroutine(AttackChoice());
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
                StopAllCoroutines();
                StartCoroutine(Stagger());
                Log("Going to phase " + phase);
            }
        }

        private void OnTakeDamage(On.HealthManager.orig_TakeDamage orig, HealthManager self, HitInstance hitInstance)
        {
            orig(self, hitInstance);
            if(self.gameObject.name == "Hegemol")
			{
                if(_staggered)
				{
                    StopAllCoroutines();
                    StartCoroutine(StaggerEnd());
				}
                _hitFx.RecieveHitEffect(hitInstance.Direction);
                PlayAudioClip("HegDamage", 1f);
            }
        }

        private void HealthManagerDie(On.HealthManager.orig_Die orig, HealthManager self, float? attackDirection, AttackTypes attackType, bool ignoreEvasion)
        {
            if(self.gameObject.name.Contains("Hegemol") && phase < 3) return;
            orig(self, attackDirection, attackType, ignoreEvasion);
        }

        private IEnumerator Die()
        {
            Log("Hegemol Death");

            if(OWArenaFinder.IsInOverWorld) OWBossManager.PlayMusic(null);
            else GGBossManager.Instance.PlayMusic(null, 1f);
            CustomWP.wonLastFight = true;
            _anim.enabled = true;
            _anim.speed = 1f;
            _anim.Play("Stagger");
            _sr.material.SetFloat("_FlashAmount", 0f);
            Destroy(_dh);
            gameObject.AddComponent<NonBouncer>();
            PlayAudioClip("HegDamageFinal", 1f);
            PlayVoiceClip("HHeavy", true, 1f);

            float dir = Mathf.Sign(transform.position.x - HeroController.instance.transform.position.x);
            _rb.gravityScale = 1.5f;
            _rb.velocity = new Vector2(7.5f * dir, 25f);

            yield return null;

            _anim.enabled = false;

            yield return new WaitForSeconds(0.1f);
            while(!_grounded)
            {
                yield return new WaitUntil(() => _grounded || CheckTerrain(transform.localScale.x * Vector3.right, 0.1f));
                if(CheckTerrain(transform.localScale.x * Vector3.right, 0.1f))
                {
                    _rb.velocity = new Vector2(-_rb.velocity.x, _rb.velocity.y);
                }
            }

            _anim.enabled = true;
            _rb.velocity = Vector2.zero;
            GameCameras.instance.cameraShakeFSM.SendEvent("AverageShake");
            PlayAudioClip("HegLand", 1f);
            PlayVoiceClip("HTired", true, 1f);

            yield return new WaitWhile(() => _anim.GetCurrentFrame() < 4);
            _anim.enabled = false;

            yield return new WaitForSeconds(1f);

            PlayVoiceClip("HCalm", true, 1f);

            yield return new WaitForSeconds(1f);

            _anim.enabled = true;
            yield return null;

            yield return new WaitWhile(() => _anim.IsPlaying("Stagger"));

            _sr.enabled = false;

            yield return new WaitForSeconds(1f);

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

            _traitorSlam = Instantiate(FiveKnights.preloadedGO["TraitorSlam"]);
            _traitorSlam.transform.Find("slash_core").Find("hurtbox").GetComponent<DamageHero>().damageDealt = 2;
            var old = _traitorSlam.transform.Find("slash_core").Find("hurtbox");
            var cp = Instantiate(old);
            cp.name = "Test2";
            cp.parent = _traitorSlam.transform.Find("slash_core").transform;
            cp.transform.position = old.transform.position;
            cp.transform.localScale = old.transform.localScale;
            old.gameObject.SetActive(false);
            _traitorSlam.SetActive(false);
        }

        private DamageHero AddDamageToGO(GameObject go, int damage, bool isAttack)
		{
            go.layer = isAttack ? (int)PhysLayers.ENEMY_ATTACK : (int)PhysLayers.ENEMIES;
            DamageHero dh = go.AddComponent<DamageHero>();
            dh.damageDealt = damage;
            dh.hazardType = (int)HazardType.NON_HAZARD + 1;
            dh.shadowDashHazard = false;
            return dh;
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
                        num = Random.Range(1, 4);
                        break;
                    case "HGrunt":
                        num = Random.Range(1, 5);
                        break;
                    case "HTired":
                        num = Random.Range(1, 4);
                        break;
                    case "HHeavy":
                        num = Random.Range(1, 3);
                        break;
                    default:
                        num = 0;
                        break;
				}
			}
            if(num != 0) clip += num;
            PlayAudioClip(clip, volume);
		}

        private void PlayAudioClip(string clip, float volume)
        {
            this.PlayAudio(FiveKnights.Clips[clip], volume);
        }

        private bool CheckTerrain(Vector3 dir, float distance)
		{
            return Physics2D.BoxCast(_col.bounds.center, _col.bounds.size, 0f,
                dir, distance, 256);
        }

        private void FixedUpdate()
		{
            _grounded = CheckTerrain(Vector3.down, 0.1f);
		}

        private void OnDestroy()
        {
            On.HealthManager.TakeDamage -= OnTakeDamage;
            On.HealthManager.Die -= HealthManagerDie;
        }

        private void Log(object message)
        {
            Modding.Logger.Log("[Hegemol Controller] " + message);
        }
    }
}
