using HutongGames.PlayMaker.Actions;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using Random = UnityEngine.Random;
using SFCore.Utils;
using GlobalEnums;
using Vasi;
using Modding;

namespace FiveKnights
{
	public class AbyssalBloom : MonoBehaviour
	{
        private HeroController _hc => HeroController.instance;
        private PlayerData _pd => PlayerData.instance;
        private tk2dSpriteAnimator _hcAnim;
        private GameObject _knightBall;
        private tk2dSpriteAnimator _knightBallAnim;
        private List<NailSlash> _nailSlashes;
        private ModifyBloomProps _modifyProps;

        private Coroutine _sideSlashCoro;
        private GameObject _sideSlash;
        private Coroutine _vertSlashCoro;
        private GameObject _shadeSlashContainer;
        private Coroutine _wallSlashCoro;
        private GameObject _wallSlash;

        private int _level;
        private int _shadeSlashNum = 1;
        private bool playingAudio;
        private float audioCooldown = 0.2f;
        private float damageBuff => 0.075f * (_pd.maxHealth - _pd.health) // 7.5% extra per missing mask
            * (_level == 2 ? 1.5f : 1f) // Multiplies current buff by 1.5 when at 1 hp
            * (_pd.equippedCharm_6 && _pd.health == 1 ? 1.75f : 1f); // Manually add the fury multiplier because otherwise it gets ignored

		private void OnEnable()
		{
            _hcAnim = _hc.GetComponent<tk2dSpriteAnimator>();

            _knightBall = Instantiate(FiveKnights.preloadedGO["Knight Ball"], _hc.transform);
            Vector3 localScale = _knightBall.transform.localScale;
            localScale.x *= -1;
            _knightBall.transform.localScale = localScale;
			_knightBall.transform.localPosition += new Vector3(-4.75f, -0.25f);
			_knightBallAnim = _knightBall.GetComponent<tk2dSpriteAnimator>();

            _nailSlashes = new List<NailSlash>
            {
                HeroController.instance.normalSlash,
                HeroController.instance.alternateSlash,
                HeroController.instance.downSlash,
                HeroController.instance.upSlash,
            };

            _modifyProps = GetComponent<ModifyBloomProps>();

            PlayMakerFSM _radControl = Instantiate(FiveKnights.preloadedGO["Radiance"].LocateMyFSM("Control"), _hc.transform);
            FiveKnights.Clips["Shade Slash"] = (AudioClip)_radControl.GetAction<AudioPlayerOneShotSingle>("Antic", 1).audioClip.Value;

			On.HealthManager.Hit += HealthManagerHit;
			On.HeroController.CancelAttack += HeroControllerCancelAttack;
			On.HeroController.CancelDownAttack += HeroControllerCancelDownAttack;
            On.HeroController.Attack += DoVoidAttack;
			On.tk2dSpriteAnimator.Play_string += Tk2dSpriteAnimatorPlay;
        }

		private void Tk2dSpriteAnimatorPlay(On.tk2dSpriteAnimator.orig_Play_string orig, tk2dSpriteAnimator self, string name)
		{
            if(self.gameObject == _hc.gameObject && name == "Idle Hurt")
			{
                self.Play("Idle");
                return;
			}
            orig(self, name);
		}

		private void OnDisable()
		{
            SetLevel(0);
            On.HealthManager.Hit -= HealthManagerHit;
            On.HeroController.CancelAttack -= HeroControllerCancelAttack;
            On.HeroController.CancelDownAttack -= HeroControllerCancelDownAttack;
            On.HeroController.Attack -= DoVoidAttack;
            On.tk2dSpriteAnimator.Play_string -= Tk2dSpriteAnimatorPlay;
        }

		public void SetLevel(int level)
		{
            _level = level;
            switch(_level)
			{
                case 0:
                    ModifySlashColors(false);
                    _modifyProps.ResetProps();
                    break;
                case 1:
                    ModifySlashColors(true);
                    _modifyProps.ModifyPropsL1();
                    break;
                case 2:
                    ModifySlashColors(true);
                    _modifyProps.ModifyPropsL2();
                    break;
            }
		}

        private void ModifySlashColors(bool modify)
        {
            foreach(NailSlash nailSlash in _nailSlashes)
            {
                nailSlash.SetFury(modify);
            }

            Color color = modify ? Color.black : Color.white;

            foreach(GameObject slash in new GameObject[]
            {
                _hc.slashPrefab,
                _hc.slashAltPrefab,
                _hc.downSlashPrefab,
                _hc.upSlashPrefab,
                _hc.wallSlashPrefab
            })
            {
                slash.GetComponent<tk2dSprite>().color = color;
            }

            GameObject attacks = HeroController.instance.gameObject.FindGameObjectInChildren("Attacks");

            foreach(string child in new[] { "Cyclone Slash", "Dash Slash", "Great Slash" })
            {
                attacks.FindGameObjectInChildren(child).GetComponent<tk2dSprite>().color = color;
                foreach(var item in attacks.FindGameObjectInChildren(child).GetComponentsInChildren<tk2dSprite>())
                    item.color = color;
            }
        }

        private void HealthManagerHit(On.HealthManager.orig_Hit orig, HealthManager self, HitInstance hitInstance)
        {
            if(hitInstance.AttackType == AttackTypes.Nail)
            {
                hitInstance.Multiplier += damageBuff;
            }
            //Log("Multiplier is currently " + damageBuff + " to deal total damage of " + hitInstance.DamageDealt * hitInstance.Multiplier);
            orig(self, hitInstance);
        }

        private void HeroControllerCancelDownAttack(On.HeroController.orig_CancelDownAttack orig, HeroController self)
        {
            if(_vertSlashCoro != null)
            {
                CancelVerticalTendrilAttack();
            }
            orig(self);
        }

        private void HeroControllerCancelAttack(On.HeroController.orig_CancelAttack orig, HeroController self)
        {
            if(_sideSlashCoro != null)
            {
                CancelTendrilAttack();
            }
            orig(self);
        }

        private void DoVoidAttack(On.HeroController.orig_Attack origAttack, HeroController hc, AttackDirection dir)
        {
            if(_level != 2)
            {
                origAttack(hc, dir);
                return;
            }

            InputHandler ih = InputHandler.Instance;
            if(_pd.GetBool(nameof(PlayerData.equippedCharm_32)))
            {
                Mirror.SetField(_hc, "attackDuration", _hc.ATTACK_DURATION_CH);
            }
            else
            {
                Mirror.SetField(_hc, "attackDuration", _hc.ATTACK_DURATION);
            }

            if(hc.cState.wallSliding)
            {
                if(_hc.cState.attacking) CancelWallTendrilAttack();
                _wallSlashCoro = StartCoroutine(WallTendrilAttack());
            }
            else if(ih.ActionButtonToPlayerAction(HeroActionButton.DOWN) && !hc.CheckTouchingGround())
            {
                if(_hc.cState.attacking) CancelVerticalTendrilAttack();
                _vertSlashCoro = StartCoroutine(VerticalTendrilAttack(false));
            }
            else if(ih.ActionButtonToPlayerAction(HeroActionButton.UP))
            {
                if(_hc.cState.attacking) CancelVerticalTendrilAttack();
                _vertSlashCoro = StartCoroutine(VerticalTendrilAttack(true));
            }
            else
            {
                if(_hc.cState.attacking)
                {
                    CancelTendrilAttack();
					_shadeSlashNum = _shadeSlashNum == 1 ? 2 : 1;
				}
                _sideSlashCoro = StartCoroutine(TendrilAttack());
            }
        }

        private void CancelTendrilAttack()
		{
            Log("Canceling attack");
            StopCoroutine(_sideSlashCoro);
            Destroy(_sideSlash);
			_knightBall.SetActive(false);
            _hc.GetComponent<MeshRenderer>().enabled = true;
            _hc.cState.attacking = false;
        }

        private void CancelVerticalTendrilAttack()
		{
            Log("Canceling vertical attack");
            StopCoroutine(_vertSlashCoro);
            Destroy(_shadeSlashContainer);
            _hc.StartAnimationControl();
            _hc.cState.attacking = false;
        }

        private void CancelWallTendrilAttack()
		{
            Log("Canceling wall attack");
            StopCoroutine(_wallSlashCoro);
            Destroy(_wallSlash);
            _hc.StartAnimationControl();
            _hc.cState.attacking = false;
        }

        private IEnumerator TendrilAttack()
        {
            _hc.cState.attacking = true;

            MeshRenderer mr = _hc.GetComponent<MeshRenderer>();
            if(!playingAudio) StartCoroutine(PlayAudio());

            mr.enabled = false;
            _knightBall.SetActive(true);

            Destroy(_sideSlash);
            _sideSlash = new GameObject("Shade Slash");
            _sideSlash.transform.parent = _knightBall.transform;
            _sideSlash.layer = (int)PhysLayers.HERO_ATTACK;
            _sideSlash.tag = "Nail Attack";
            _sideSlash.transform.localPosition = Vector3.zero;
            _sideSlash.transform.localScale = Vector3.one;
            _sideSlash.SetActive(false);

            AddDamageEnemiesFsm(_sideSlash, AttackDirection.normal);

            PolygonCollider2D slashPoly = _sideSlash.AddComponent<PolygonCollider2D>();
            slashPoly.points = new[]
            {
                new Vector2(0.0f, -2.0f),
                new Vector2(3.5f, -2.0f),
                new Vector2(3.5f, 0.0f),
                new Vector2(3.0f, 1.0f),
                new Vector2(0.0f, 2.0f),
                new Vector2(-3f, 0.0f), // to have parts of the player covered with a hitbox
            };

            slashPoly.offset = Vector2.zero;
            slashPoly.isTrigger = true;

            GameObject parrySlash = Instantiate(_sideSlash, _sideSlash.transform);
            parrySlash.layer = (int)PhysLayers.ITEM;

            ShadeSlash ss = _sideSlash.AddComponent<ShadeSlash>();
            ss.attackDirection = AttackDirection.normal;

            _sideSlash.SetActive(true);
            parrySlash.SetActive(true);

			_knightBallAnim.PlayFromFrame("Slash" + _shadeSlashNum + " Antic", 2);
			yield return new WaitWhile(() => _knightBallAnim.IsPlaying("Slash" + _shadeSlashNum + " Antic"));
			yield return new WaitForSeconds(_knightBallAnim.PlayAnimGetTime("Slash" + _shadeSlashNum) - (1f / 24f));

			Destroy(_sideSlash);

            mr.enabled = true;

            _knightBall.SetActive(false);

            // Used to keep track of reg slash/alt slash
            _shadeSlashNum = _shadeSlashNum == 1 ? 2 : 1;
            _hc.cState.attacking = false;
        }

        private IEnumerator VerticalTendrilAttack(bool up)
        {
            _hc.cState.attacking = true;

            Rigidbody2D rb = _hc.GetComponent<Rigidbody2D>();
            string animName = up ? "Up" : "Down";

            _hc.StopAnimationControl();
            if(!playingAudio) StartCoroutine(PlayAudio());

            _hcAnim.Play(animName + "Slash Void");
            tk2dSpriteAnimationClip hcSlashAnim = _hcAnim.GetClipByName(animName + "Slash Void");
            _hcAnim.Play(hcSlashAnim);

            // Create slash objects
            Destroy(_shadeSlashContainer);
            _shadeSlashContainer = Instantiate(new GameObject("Shade Slash Container"), _hc.transform);
            _shadeSlashContainer.layer = (int)PhysLayers.HERO_ATTACK;
            _shadeSlashContainer.SetActive(false);

            GameObject shadeSlash = new GameObject("Shade Slash");
            shadeSlash.transform.parent = _shadeSlashContainer.transform;
            shadeSlash.layer = (int)PhysLayers.HERO_ATTACK;
            shadeSlash.tag = "Nail Attack";
            shadeSlash.transform.localPosition = new Vector3(0f, up ? 1.0f : -2.0f, 0f);
            shadeSlash.transform.localScale = new Vector3(2f, 2f, 2f);

            AddDamageEnemiesFsm(shadeSlash, up ? AttackDirection.upward : AttackDirection.downward);

            // Create hitboxes
            PolygonCollider2D slashPoly = shadeSlash.AddComponent<PolygonCollider2D>();
            if(up) slashPoly.points = new[]
            {
                new Vector2(-1f, 0f),
                new Vector2(-0.75f, 1.5f),
                new Vector2(-0.5f, 2.0f),
                new Vector2(0f, 2.25f),
                new Vector2(0.5f, 2.0f),
                new Vector2(0.75f, 1.5f),
                new Vector2(1f, 0f),
                new Vector2(0f, 0.5f)
            };
            else slashPoly.points = new[]
            {
                new Vector2(-1f, -0f),
                new Vector2(-1.25f, -0.5f),
                new Vector2(-0.875f, -1.5f),
                new Vector2(-0.5f, -1.9f),
                new Vector2(0f, -2.2f),
                new Vector2(0.5f, -2.0f),
                new Vector2(0.875f, -1.5f),
                new Vector2(1.25f, -0.5f),
                new Vector2(1f, -0f),
                new Vector2(0f, 0.5f)
            };
            slashPoly.offset = new Vector2(0.0f, up ? -1f : 0.75f);
            slashPoly.isTrigger = true;

            GameObject parrySlash = Instantiate(shadeSlash, shadeSlash.transform);
            parrySlash.layer = (int)PhysLayers.ITEM;
            parrySlash.transform.localPosition = Vector3.zero;
            parrySlash.transform.localScale = Vector3.one;

            shadeSlash.AddComponent<MeshRenderer>();
            shadeSlash.AddComponent<MeshFilter>();
            tk2dSprite slashSprite = shadeSlash.AddComponent<tk2dSprite>();
            tk2dSpriteAnimator slashAnim = shadeSlash.AddComponent<tk2dSpriteAnimator>();
            slashSprite.Collection = _hc.GetComponent<tk2dSprite>().Collection;
            slashAnim.Library = _hcAnim.Library;

            ShadeSlash ss = shadeSlash.AddComponent<ShadeSlash>();
            ss.attackDirection = up ? AttackDirection.upward : AttackDirection.downward;

            _shadeSlashContainer.SetActive(true);

            yield return new WaitForSeconds(slashAnim.PlayAnimGetTime(animName + "Slash Effect"));

            Destroy(_shadeSlashContainer);

            yield return new WaitWhile(() => _hcAnim.Playing && _hcAnim.IsPlaying(animName + "Slash Void"));
            _hc.StartAnimationControl();
            _hc.cState.attacking = false;
        }

        private IEnumerator WallTendrilAttack()
        {
            _hc.cState.attacking = true;

            _hc.StopAnimationControl();

            if(!playingAudio) StartCoroutine(PlayAudio());

            _hcAnim.Play(_hcAnim.GetClipByName("WallSlash Void"));

            Destroy(_wallSlash);
            _wallSlash = new GameObject("Shade Slash");
            _wallSlash.transform.parent = _hc.transform;
            _wallSlash.layer = (int)PhysLayers.HERO_ATTACK;
            _wallSlash.tag = "Nail Attack";
            _wallSlash.transform.localPosition = new Vector3(0f, 1f, 0f);
            _wallSlash.transform.localScale = Vector3.one;
            _wallSlash.SetActive(false);

            AddDamageEnemiesFsm(_wallSlash, AttackDirection.normal);

            PolygonCollider2D slashPoly = _wallSlash.AddComponent<PolygonCollider2D>();
            slashPoly.points = new[]
            {
                new Vector2(-1.5f, 2.0f),
                new Vector2(1.0f, 1.5f),
                new Vector2(3.0f, -1.0f),
                new Vector2(1.0f, -2.0f),
                new Vector2(-1.5f, -1.0f),
            };
            slashPoly.offset = new Vector2(1f, 0f);
            slashPoly.isTrigger = true;

			GameObject parrySlash = Instantiate(_wallSlash, _wallSlash.transform);
			parrySlash.layer = (int)PhysLayers.ITEM;
			parrySlash.transform.localPosition = Vector3.zero;
			parrySlash.transform.localScale = Vector3.one;
            parrySlash.SetActive(false);

			_wallSlash.AddComponent<MeshRenderer>();
            _wallSlash.AddComponent<MeshFilter>();
            tk2dSprite slashSprite = _wallSlash.AddComponent<tk2dSprite>();
            tk2dSpriteAnimator slashAnim = _wallSlash.AddComponent<tk2dSpriteAnimator>();
            slashSprite.Collection = _hc.GetComponent<tk2dSprite>().Collection;
            slashAnim.Library = _hcAnim.Library;

            ShadeSlash ss = _wallSlash.AddComponent<ShadeSlash>();
            ss.attackDirection = AttackDirection.normal;

            _wallSlash.SetActive(true);
            parrySlash.SetActive(true);

            yield return new WaitForSeconds(slashAnim.PlayAnimGetTime("Slash Effect"));

            Destroy(_wallSlash);

			_hc.StartAnimationControl();
            _hc.cState.attacking = false;
        }

        private IEnumerator PlayAudio()
		{
            playingAudio = true;
            this.PlayAudio(FiveKnights.Clips["Shade Slash"], 0.7f);
            yield return new WaitForSeconds(audioCooldown);
            playingAudio = false;
        }

        private void AddDamageEnemiesFsm(GameObject o, AttackDirection dir)
        {
            PlayMakerFSM tempFsm = o.AddComponent<PlayMakerFSM>();
            PlayMakerFSM fsm = _hc.gameObject.Find("AltSlash").LocateMyFSM("damages_enemy");
            foreach(var fi in typeof(PlayMakerFSM).GetFields(BindingFlags.Instance | BindingFlags.NonPublic |
                                                              BindingFlags.Public))
            {
                fi.SetValue(tempFsm, fi.GetValue(fsm));
            }
            switch(dir)
			{
                case AttackDirection.normal:
                    tempFsm.GetFsmFloatVariable("direction").Value = _hc.cState.facingRight ? 0f : 180f;
                    break;
                case AttackDirection.upward:
                    tempFsm.GetFsmFloatVariable("direction").Value = 90f;
                    break;
                case AttackDirection.downward:
                    tempFsm.GetFsmFloatVariable("direction").Value = 270f;
                    break;
			}
        }

        private void Log(object o) => Modding.Logger.Log("[Abyssal Bloom] " + o);
    }
}
