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
using Random = UnityEngine.Random;

namespace FiveKnights.Hegemol
{
    public class HegemolController : MonoBehaviour
    {
        private const int Health = 800; //1600; //2400; // 800 is 2400/3, did this because of the new phases
        private const float LeftX = 61.0f;
        private const float RightX = 91.0f;
        
        private const float OWLeftX = 420.7f;
        private const float OWRightX = 456.0f;
        private const float OWBottomY = 27.4f;
        private const float OWCenterX = (OWLeftX + OWRightX) / 2;


        private const float GGLeftX = 11.2f;
        private const float GGRightX = 45.7f;
        private const float GGBottomY = 27.4f;
        private const float GGCenterX = (GGLeftX + GGRightX) / 2;


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

        private bool _attacking;
        private bool _isNextPhase = false;
	private bool _isDead = false;

        private bool _grounded;

        private void Awake()
        {
            Log("Hegemol Awake");

            gameObject.name = "Hegemol";
            gameObject.layer = (int)PhysLayers.ENEMIES;
            transform.localScale = 1.5f * new Vector3(-Mathf.Abs(transform.localScale.x), transform.localScale.y,
                    transform.localScale.z);
            DamageHero dh = gameObject.AddComponent<DamageHero>();
            dh.damageDealt = 2;
            dh.hazardType = (int)HazardType.SPIKES;

            _pv = Instantiate(FiveKnights.preloadedGO["PV"], Vector2.down * 10, Quaternion.identity);
            _pv.SetActive(true);
            PlayMakerFSM control = _pv.LocateMyFSM("Control");
            control.RemoveTransition("Pause", "Set Phase HP");

            gameObject.transform.position = OWArenaFinder.IsInOverWorld ?
                new Vector2(OWRightX, (CustomWP.boss == CustomWP.Boss.All) ? 11.4f : 29.4f) :
                new Vector2((CustomWP.boss == CustomWP.Boss.All) ? RightX - 10f : 40f, (CustomWP.boss == CustomWP.Boss.All) ? 11.4f : 29.4f);

            _ogrim = FiveKnights.preloadedGO["WD"];
            _dd = _ogrim.LocateMyFSM("Dung Defender");

            _col = gameObject.GetComponent<BoxCollider2D>();
            _hm = gameObject.AddComponent<HealthManager>();
            _rb = gameObject.GetComponent<Rigidbody2D>();
            _anim = gameObject.GetComponent<Animator>();
            _sr = gameObject.GetComponent<SpriteRenderer>();
            _hitFx = gameObject.AddComponent<EnemyHitEffectsArmoured>();
            _hitFx.enabled = true;

            On.EnemyHitEffectsArmoured.RecieveHitEffect += OnReceiveHitEffect;
            On.HealthManager.TakeDamage += OnTakeDamage;
			On.HealthManager.Die += HealthManagerDie;
        }

		private IEnumerator Start()
        {
            _sr.enabled = false;
            while (HeroController.instance == null) yield return null;
            yield return new WaitForSeconds(1f);

            _hm.hp = Health;
            #region TODO old stuff might have to put back in
            /*GetComponent<EnemyDeathEffects>().SetJournalEntry(FiveKnights.journalentries["Hegemol"]);
            float sizemod = 1.754386f;
            _mace = new GameObject("Mace");
            GameObject _head = new GameObject("Head");
            GameObject _handle = new GameObject("Handle");
            GameObject _Msprite = new GameObject("Mace Sprite");
            _head.transform.parent = _mace.transform;
            _handle.transform.parent = _mace.transform;
            _Msprite.transform.parent = _mace.transform;
            Rigidbody2D _macerb2d = _mace.AddComponent<Rigidbody2D>();
            _macerb2d.gravityScale = 1f;
            _macerb2d.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            _macerb2d.interpolation = RigidbodyInterpolation2D.Interpolate;
            _handle.transform.localPosition = new Vector3(-3.2f, -0.29f, 0f);
            _handle.transform.SetRotationZ(-25f);
            BoxCollider2D _headcol = _head.AddComponent<BoxCollider2D>();
            _headcol.isTrigger = true;
            _headcol.offset = new Vector2(-0.1039203f, -0.1409256f);
            _headcol.size = new Vector2(2.75424f, 3.117193f);
            DamageHero _headdamage = _head.AddComponent<DamageHero>();
            _headdamage.damageDealt = 2;
            _headdamage.hazardType = 1;
            _handle.transform.localPosition = new Vector3(-0.07f, -1.64f, 0f);
            _handle.transform.SetRotationZ(-24.44f);
            BoxCollider2D _handlecol = _handle.AddComponent<BoxCollider2D>();
            _handlecol.isTrigger = true;
            _handlecol.offset = new Vector2(1.787836f, -0.2449788f);
            _handlecol.size = new Vector2(7.905645f, 0.3324739f);
            DamageHero _handledamage = _handle.AddComponent<DamageHero>();
            _handledamage.damageDealt = 2;
            _handledamage.hazardType = 1;
            _handle.transform.localPosition = new Vector3(0f, 0.75f * sizemod, 0f);
            _handle.transform.SetRotationZ(0f);
            _Msprite.transform.localScale = new Vector3(sizemod, sizemod, 1f);
            //_Msprite.AddComponent<SpriteRenderer>().sprite = FiveKnights.SPRITES["mace"];
            _mace.AddComponent<Mace>();
            //_mace.AddComponent<DebugColliders>();
            //_mace.transform.Log();
            _mace.SetActive(false);*/
            #endregion

            GetComponent<EnemyDeathEffects>().SetJournalEntry(FiveKnights.journalentries["Hegemol"]);
            
            GameObject _maceGO = Instantiate(FiveKnights.preloadedGO["Mace"], transform);
            _maceGO.SetActive(false);
            _mace = _maceGO.AddComponent<Mace>();

            _hitter = gameObject.transform.Find("Hitter").gameObject;
            _hitter.layer = (int)PhysLayers.ENEMY_ATTACK;
            _hitter.AddComponent<NonBouncer>();
            DamageHero dh = _hitter.AddComponent<DamageHero>();
            dh.damageDealt = 2;
            dh.hazardType = (int)HazardType.SPIKES;

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

            yield return new WaitWhile(() => _anim.GetCurrentFrame() < 9);

            PlayAudioClip(_ap, FiveKnights.Clips["AudLand"], 1f);

            yield return new WaitWhile(() => _anim.IsPlaying());
            yield return IdleTimer();
        }

        private void MusicControl()
        {
            Log("Start Music");
            GGBossManager.Instance.PlayMusic(FiveKnights.Clips["HegemolMusic"], 1f);
        }

        //some logic taken from dryya
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
                Slam, Charge
            });
            attacks.Add("Charge", new List<Func<IEnumerator>>()
            {
                Slam, Jump
            });

            // Always start with Charge so he immediately starts doing something
            nextAtt = Charge;

            while(true)
            {
                yield return new WaitWhile(() => _attacking);
                _attacking = true;
		if(_isDead)
			_attcking = true;
                if (_hm.hp <= 0 && phase < 3)
                {
                    phase++;
                    _hm.hp = 800;
                    _isNextPhase = true;
                    Log("[Attack] GroundPunch");
                    yield return GroundPunch();
                    continue;
                }

                yield return Turn();

                prevAtt = currAtt;
                currAtt = nextAtt;
                nextAtt = attacks[currAtt.Method.Name][Random.Range(0, attacks[currAtt.Method.Name].Count)];
                
                Log("[Attack] " + currAtt.Method.Name);
                StartCoroutine(currAtt.Invoke());
            }
        }

        private IEnumerator GroundPunch()
        {
            if(_isNextPhase)
            {
                float arenaCenter;
                if(!OWArenaFinder.IsInOverWorld)
                    arenaCenter = GGCenterX;
                else
                    arenaCenter = OWCenterX;
                float diff = arenaCenter - gameObject.transform.position.x;
                _rb.gravityScale = 3;
                _rb.velocity = new Vector2(diff, 60f);

                _anim.Play("Jump");

                yield return new WaitForSeconds(0.69f);
                yield return new WaitUntil(() => _grounded);

                _anim.Play("Land");

                _rb.velocity = Vector2.zero;
                yield return null;

                yield return new WaitWhile(() => _anim.IsPlaying("Land"));

                _isNextPhase = false;
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
                
                if(_isNextPhase)
                {
                    //code to spawn barrels from ceiling
                }

                if(OWArenaFinder.IsInOverWorld) StartCoroutine(DungSide(right));
                else
                {
                    SpawnShockwaves(right, 4f, 2.5f, 50f, 2);
                }

                yield return new WaitUntil(() => _anim.GetCurrentFrame() == 0);
                transform.localScale = new Vector3(-transform.localScale.x, transform.localScale.y, transform.localScale.z);
                right = !right;
                if(i == 0) _mace.gameObject.SetActive(false); 
            }

            yield return new WaitWhile(() => _mace.transform.position.y > transform.position.y);

            _anim.enabled = true;
            _mace.gameObject.SetActive(false);

            yield return new WaitWhile(() => _anim.IsPlaying());
            yield return IdleTimer();
        }

        private IEnumerator Jump()
        {
            bool towards = true;
            float diff;
            if(towards) diff = HeroController.instance.transform.position.x - transform.position.x;
            else diff = (transform.localScale.x > 0 ? LeftX : RightX + transform.position.x) / 2 - transform.position.x;

            _anim.Play("JumpAntic");

            yield return null;
            yield return new WaitWhile(() => _anim.IsPlaying("JumpAntic"));

            _anim.Play("Jump");
            _rb.gravityScale = 3f;
            _rb.velocity = new Vector2(diff, 60f);
            
            yield return new WaitForSeconds(0.69f);
            yield return new WaitUntil(() => _grounded);

            _anim.Play("Land");
            GameCameras.instance.cameraShakeFSM.SendEvent("AverageShake");
            _rb.velocity = Vector2.zero;
            PlayAudioClip(_ap, FiveKnights.Clips["AudLand"], 1f);
            yield return null;

            yield return new WaitWhile(() => _anim.IsPlaying("Land"));
            yield return IdleTimer();
        }

        private IEnumerator Slam()
        {
            _anim.Play("AttackAntic");
            yield return null;

            yield return new WaitWhile(() => _anim.IsPlaying("AttackAntic"));

            _anim.Play("AttackAnticLoop");

            yield return new WaitForSeconds(0.1f);

            _anim.StopPlayback();
            _anim.Play("Attack");
            yield return null;

            yield return new WaitWhile(() => _anim.IsPlaying("Attack"));

            SpawnShockwaves(transform.localScale.x > 0f, 7f, 2.5f, 50f, 2);

            yield return _anim.PlayBlocking("AttackRecover");
            yield return IdleTimer();
        }

        private IEnumerator Dig()
        {
            _anim.Play("DigAntic");
            yield return null;

            yield return new WaitWhile(() => _anim.GetCurrentFrame() < 3);
            yield return new WaitForSeconds(0.2f);

            yield return new WaitWhile(() => _anim.IsPlaying("DigAntic"));

            PlayAudioClip(_ap, FiveKnights.Clips["AudLand"], 1f);
            _anim.Play("Dig");
            _rb.velocity = 2f * Vector2.right * transform.localScale.x;

            yield return new WaitForSeconds(1f);

            _anim.Play("DigEnd");
            _rb.velocity = Vector2.zero;
            yield return null;
            //PlayAudioClip(_ap, FiveKnights.Clips["Mace Swing"], 1f);

            if(!OWArenaFinder.IsInOverWorld)
            {
                Vector2 pos = transform.position + Mathf.Sign(transform.localScale.x) * Vector3.right * 5.5f + Vector3.down * 2.6f;
                float valMin = 20f;
                float valMax = 30f;
                int times = 3;
                if (phase == 3)
                    times = 5;

                for (int i = 0; i < times; i++)
                {
                    GameObject dungBall1 = Instantiate(FiveKnights.preloadedGO["ball"], pos, Quaternion.identity);
                    dungBall1.SetActive(true);
                    //dungBall1.AddComponent<ExplosionControl>();
                    dungBall1.GetComponent<Rigidbody2D>().velocity = new Vector2(transform.localScale.x * Random.Range(valMin, valMax), 2 * Random.Range(valMin, valMax));
                }
            }

            yield return new WaitWhile(() => _anim.IsPlaying("DigEnd"));
            yield return IdleTimer();
        }

        private IEnumerator Charge()
		{
            yield return _anim.PlayBlocking("RunAntic");
            yield return new WaitForSeconds(0.1f);

            _anim.Play("Run");
            _rb.velocity = new Vector2(18f * Mathf.Sign(transform.localScale.x), 0f);

            yield return new WaitUntil(() => CheckTerrain(transform.localScale.x * Vector2.right));

            _anim.Play("Jump");
            _rb.gravityScale = 1.5f;
            _rb.velocity = new Vector2(-7.5f * Mathf.Sign(transform.localScale.x), 25f);

            yield return new WaitForSeconds(0.1f);
            yield return new WaitUntil(() => _grounded);

            _anim.Play("Land");
            GameCameras.instance.cameraShakeFSM.SendEvent("AverageShake");
            _rb.velocity = Vector2.zero;
            PlayAudioClip(_ap, FiveKnights.Clips["AudLand"], 1f);
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
            while(xLeft >= (OWArenaFinder.IsInOverWorld ? OWLeftX : (CustomWP.boss == CustomWP.Boss.All) ? LeftX : 11.2f) || xRight <= (OWArenaFinder.IsInOverWorld ? OWRightX : (CustomWP.boss == CustomWP.Boss.All) ? RightX : 45.7f))
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
            float xMaxMin = right ? (OWArenaFinder.IsInOverWorld ? OWRightX : RightX) : (OWArenaFinder.IsInOverWorld ? OWLeftX : LeftX);
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
            PlayAudioClip(_ap, FiveKnights.Clips["AudLand"], 1f);

            PlayMakerFSM fsm = FiveKnights.preloadedGO["Mage"].LocateMyFSM("Mage Lord");

            GameObject shockwave = Instantiate(fsm.GetAction<SpawnObjectFromGlobalPool>("Quake Waves", 0).gameObject.Value);

            PlayMakerFSM shockFSM = shockwave.LocateMyFSM("shockwave");

            shockFSM.FsmVariables.FindFsmBool("Facing Right").Value = facingRight;
            shockFSM.FsmVariables.FindFsmFloat("Speed").Value = speed;
            shockwave.AddComponent<DamageHero>().damageDealt = damage;
            shockwave.SetActive(true);

            shockwave.transform.SetPosition2D(new Vector2(pos.x + (facingRight ? 1 : -1) * offset, 
                (OWArenaFinder.IsInOverWorld ? OWBottomY : GGBottomY) - 1.4f));
            shockwave.transform.SetScaleX(scale);
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
            StartCoroutine(FlashWhite());
            PlayAudioClip(_damage, FiveKnights.Clips["HegDamage"], 1f);
            orig(self, attackDirection);
        }

        private void OnTakeDamage(On.HealthManager.orig_TakeDamage orig, HealthManager self, HitInstance hitInstance)
        {
            //if (self.name.Contains("False Knight Dream"))
            if(self.gameObject.name == "Hegemol")
            {
                if (_hm.hp <= 0 && phase >= 3)
                {
                    StopAllCoroutines();
                    StartCoroutine(Die());
                }
                if (hitInstance.AttackType == AttackTypes.Nail)
                {
                    // Manually gain soul when striking Hegemol
                    int soulGain;
                    if (PlayerData.instance.MPCharge >= 99)
                    {
                        soulGain = 4;
                        if (PlayerData.instance.equippedCharm_20) soulGain += 1;
                        if (PlayerData.instance.equippedCharm_21) soulGain += 2;
                    }
                    else
                    {
                        soulGain = 9;
                        if (PlayerData.instance.equippedCharm_20) soulGain += 2;
                        if (PlayerData.instance.equippedCharm_21) soulGain += 4;
                    }
                    HeroController.instance.AddMPCharge(soulGain);
                }
                _hitFx.RecieveHitEffect(hitInstance.Direction);
            }

            orig(self, hitInstance);
        }

        private void HealthManagerDie(On.HealthManager.orig_Die orig, HealthManager self, float? attackDirection, AttackTypes attackType, bool ignoreEvasion)
        {
            if(self.gameObject.name == "Hegemol") return;
            orig(self, attackDirection, attackType, ignoreEvasion);
        }

        private IEnumerator Die()
        {
            Log("Hegemol Death");
	    _isDead = true;
            GGBossManager.Instance.PlayMusic(null, 1f);
            CustomWP.wonLastFight = true;
            _anim.Play("Stagger");

            yield return new WaitWhile(() => _anim.GetCurrentFrame() < 4);
            _anim.enabled = false;

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
        }

        private IEnumerator IdleTimer()
        {
            Log("[Idle]");
            _anim.Play("Idle");
            yield return new WaitForSeconds(IdleTime);
            _attacking = false;
        }

        private void PlayAudioClip(MusicPlayer ap, AudioClip clip, float volume)
        {
            ap.Clip = clip;
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
