using System.Collections;
using System.Collections.Generic;
using GlobalEnums;
using HutongGames.PlayMaker.Actions;
using UnityEngine;
using static FiveKnights.Tiso.TisoAudio;
using Logger = Modding.Logger;

namespace FiveKnights.Tiso
{
    public class TisoAttacks
    {
        private Transform transform;
        private Rigidbody2D _rb;
        private BoxCollider2D _bc;
        private Animator _anim;
        private GameObject _target;
        private GameObject _shield;
        private TisoController _tc;
        private EnemyDeathEffectsUninfected _deathEff;
        private List<GameObject> _destroyOnDeath;

        private const float RunSpeed = 12f;
        private const float DodgeSpeed = 20f;
        private const float BombHeight = 19f;
        private const float SpikeVel = 60f;
        private const int NumShots = 8;
        
        public TisoAttacks(Transform transform, Rigidbody2D rb, BoxCollider2D bc, Animator anim, EnemyDeathEffectsUninfected deathEff)
        {
            this.transform = transform;
            _rb = rb;
            _bc = bc;
            _anim = anim;
            _shield = transform.Find("Shields").Find("ShieldHalf").gameObject;
            _target = HeroController.instance.gameObject;
            _deathEff = deathEff;
            _tc = transform.GetComponent<TisoController>();
            _destroyOnDeath = new List<GameObject>();

            foreach (Transform shield in this.transform.Find("ShieldHB"))
            {
                var runParry = shield.gameObject;
                var shieldParryCtrl = runParry.AddComponent<TisoShieldParry>();
                if (shield.name.Contains("UpShield"))
                {
                    shieldParryCtrl.Predicate = () =>
                    {
                        Vector2 hPos = _target.transform.position;
                        Vector2 tPos = transform.position;
                        var relativePt = transform.InverseTransformPoint(hPos);
                        return hPos.x.Within(tPos.x, 2.5f) && relativePt.y > 0.5f;
                    };
                }
                else
                {
                    shieldParryCtrl.Predicate = () =>
                    {
                        float dir = Mathf.Sign(transform.GetScaleX());
                        return dir > 0
                            ? HeroController.instance.transform.position.x < transform.position.x
                            : HeroController.instance.transform.position.x > transform.position.x;
                    };
                }
                runParry.layer = (int) PhysLayers.ENEMIES;
                runParry.AddComponent<DamageHero>();
            }

            TisoSpike.AllSpikes = new List<GameObject>();
        }

        public IEnumerator SpawnBombs()
        {
            Transform arm = transform.Find("SwapWeapon").Find("s3").Find("s8");
            GameObject bombPar = arm.Find("Bomb").gameObject;
            TisoBomb.AllBombs = new List<GameObject>();
            _anim.Play("TisoRoar");
            PlayAudio(_tc, Clip.Yell);

            yield return new WaitForSeconds(0.6f);
            float leftPos = transform.position.x - 3;
            float rightPos = transform.position.x + 3;

            var a = _tc.StartCoroutine(BlockUpSpecial());
            
            while (leftPos > TisoController.LeftX + 0.5f || rightPos < TisoController.RightX - 0.5f)
            {
                if (leftPos > TisoController.LeftX + 0.5f)
                {
                    GameObject bombL = Object.Instantiate(bombPar);
                    bombL.transform.position = new Vector3(leftPos, BombHeight);
                    bombL.SetActive(true);
                    bombL.AddComponent<TisoBomb>(); 
                }

                if (rightPos < TisoController.RightX - 0.5f)
                {
                    GameObject bombR = Object.Instantiate(bombPar);
                    bombR.transform.position = new Vector3(rightPos, BombHeight);
                    bombR.SetActive(true);
                    bombR.AddComponent<TisoBomb>();
                }
                
                leftPos -= 2.5f;
                rightPos += 2.5f;
                yield return new WaitForSeconds(Random.Range(0.2f, 0.25f));
            }

            yield return a;
            
            IEnumerator BlockUpSpecial()
            {
                var shieldParry = transform.Find("ShieldHB").Find("UpShield").GetComponent<TisoShieldParry>();
                shieldParry.hitFlag = false;
                yield return _anim.PlayToFrame("TisoBlockUp", 0);
                _anim.enabled = false;
                yield return new WaitWhile(() => TisoBomb.AllBombs.Count > 0);
                yield return new WaitForSeconds(0.1f);
                _anim.enabled = true;
                yield return Dodge();
            }
        }

        public IEnumerator BlockUp()
        {
            var shieldParry = transform.Find("ShieldHB").Find("UpShield").GetComponent<TisoShieldParry>();
            shieldParry.hitFlag = false;
            yield return _anim.PlayToFrame("TisoBlockUp", 0);
            _anim.enabled = false;
            yield return new WaitSecWhile(() => !shieldParry.hitFlag && CheckPlayerAbove(), 1.5f);
            _anim.enabled = true;
            
            if (shieldParry.hitFlag)
            {
                yield return _anim.PlayToEnd();
            }
            
            yield return Dodge();
        }
        
        public IEnumerator JumpGlideSlam()
        {
            bool shouldStopWave = false;
            
            IEnumerator Jump()
            {
                FaceHero();
                PlayAudio(_tc, Clip.Jump);
                yield return _anim.PlayToEnd("TisoJump");
                float dir = FaceHero();
                _anim.Play("TisoSpin");
                float spd = 40;
                _rb.velocity = new Vector2(-dir * 10f, spd);
                _rb.isKinematic = false;
                _rb.gravityScale = 1.4f;
                yield return new WaitWhile(() => _rb.velocity.y > 5f);
            }
            
            IEnumerator WaveMove()
            {
                while (!shouldStopWave)
                {
                    _rb.velocity += new Vector2(0f, 3f);
                    yield return new WaitForSeconds(0.2f);
                }
            }

            IEnumerator Glide()
            {
                float dir = Mathf.Sign(transform.GetScaleX());
                _anim.Play("TisoFlyAntic", -1, 0f);
                _rb.gravityScale = 0.5f;
                yield return _anim.PlayToEnd();
                _anim.Play("TisoFly");

                _rb.velocity = new Vector2(-dir * 20f, _rb.velocity.y / 4f);
                _tc.StartCoroutine(WaveMove());
                
                yield return new WaitWhile(() =>
                    dir > 0
                        ? _target.transform.position.x + 2f < transform.position.x
                        : _target.transform.position.x - 2f > transform.position.x);
                _rb.isKinematic = true;
                _rb.gravityScale = 0f;
                shouldStopWave = true;
            }

            IEnumerator Slam()
            {
                _anim.speed = 1.5f;
                _tc.StartCoroutine(DampToVel(_rb.velocity, new Vector2(0f, -50f), 2 / 12f));
                yield return _anim.PlayToFrame("TisoFlySlam", 4);
                _anim.speed = 1f;
                _anim.enabled = false;
                _rb.velocity = new Vector2(0f, -50f);
                yield return new WaitWhile(() => transform.position.y > TisoController.GroundY);
                PlayAudio(_tc, Clip.LandHard);
                _rb.velocity = Vector2.zero;
                transform.position = new Vector2(transform.position.x, TisoController.GroundY);
                GameCameras.instance.cameraShakeFSM.SendEvent("AverageShake");
                _anim.enabled = true;
                SpawnShockwaves(1.2f, 35f, 1, transform.position);
                yield return _anim.PlayToEnd();
            }
            
            _tc.PlayAudio(TisoAud[TisoRandAudio.PickRandomTisoAud(2, 6)]);
            yield return Jump();
            yield return Glide();
            yield return Slam();
            yield return Dodge();
        }

        public IEnumerator Shoot()
        {
            float dir = FaceHero() * -1f;
            // Try and stop the gun from being in front of its hinge
            Transform arm = transform.Find("SwapWeapon").Find("s3").Find("s8");
            arm.position = new Vector3(arm.position.x, arm.position.y, 0.5f);
            GameObject spikePar = arm.Find("Spikes").gameObject; 
            _tc.PlayAudio(TisoAud[TisoRandAudio.PickRandomTisoAud(2, 6)]);
            
            // Have to do this once here so the canon doesn't appear at the wrong angle at the start
            Vector2 tarPos = _target.transform.position;
            float angleOrig = GetAngleToPlayer(spikePar.transform.position, tarPos, dir);
            // 40
            angleOrig = dir > 0 ? Mathf.Max(Mathf.Min(40f, angleOrig), -17f) : Mathf.Max(Mathf.Min(15f, angleOrig), -42f);
            float angle2 = dir > 0 ? Mathf.Max(Mathf.Min(17f, angleOrig), -16f) : Mathf.Max(Mathf.Min(8f, angleOrig), -25f);
            transform.Find("SwapWeapon").Find("s3").Find("s8").SetRotation2D(angle2);
            transform.Find("SwapWeapon").Find("s3").SetRotation2D(angleOrig - angle2);
            
            yield return _anim.PlayToEnd("TisoShootAntic");
            _anim.speed = 1f;
            int numInterrupts = 0;
            
            for (int i = 0; i < NumShots; i++)
            {
                // Interrupt Tiso shooting if player is on the wrong side or too close
                if (FaceHero(true) * dir > 0 || _target.transform.position.x.Within(transform.position.x, 3f)) 
                {
                    _anim.speed = 1.5f;
                    yield return _anim.PlayToEnd("TisoShootEnd");
                    _anim.speed = 1f;
                    yield return Dodge();
                    // Just stop shooting if interrupted too many times
                    if (numInterrupts >= 2) yield break;
                    _anim.speed = 1.5f;
                    // Case where player has moved to opposite side
                    dir = FaceHero() * -1f;
                    yield return _anim.PlayToEnd("TisoShootAntic");
                    _anim.speed = 1f;
                    numInterrupts++;
                }
                
                // Player lower than tiso on left side because spikes shoot too high otherwise
                tarPos = _target.transform.position - new Vector3(0f, (dir > 0 ? 0f : 1f));
                angleOrig = GetAngleToPlayer(spikePar.transform.position, tarPos, dir);

                angleOrig = dir > 0
                    ? Mathf.Max(Mathf.Min(40f, angleOrig), -17f)
                    : Mathf.Max(Mathf.Min(15f, angleOrig), -42f);
                angle2 = dir > 0
                    ? Mathf.Max(Mathf.Min(17f, angleOrig), -16f)
                    : Mathf.Max(Mathf.Min(8f, angleOrig), -25f);
                
                transform.Find("SwapWeapon").Find("s3").Find("s8").SetRotation2D(angle2);
                transform.Find("SwapWeapon").Find("s3").SetRotation2D(angleOrig - angle2);
                _anim.Play("TisoShoot", -1, 0f);
                PlayAudio(_tc, Clip.Shoot);
                GameObject spike = Object.Instantiate(spikePar);
                spike.transform.SetRotation2D(angle2);
                spike.transform.position = spikePar.transform.position + new Vector3(0f, 0f, 0.5f);
                spike.transform.localScale *= 2.5f;
                spike.SetActive(true);
                spike.AddComponent<TisoSpike>();
                var rb = spike.GetComponent<Rigidbody2D>(); 
                angle2 += dir > 0 ? 0f : 180f;
                rb.velocity = new Vector2(Mathf.Cos(angle2 * Mathf.Deg2Rad), Mathf.Sin(angle2 * Mathf.Deg2Rad)) * SpikeVel;
                _anim.speed = 1.2f;
                yield return _anim.PlayToEnd();
                _anim.speed = 1;
            }
            _anim.speed = 1f;
            yield return _anim.PlayToEnd("TisoShootEnd");
        }

        private float GetAngleToPlayer(Vector2 from, Vector2 to, float dir)
        {
            Vector2 relativeTo = dir < 0 ? (from - to).normalized : (to - from).normalized;
            return Vector2.SignedAngle(from, relativeTo);
        }

        public IEnumerator ThrowShield()
        {
            float dir = FaceHero();
            _tc.PlayAudio(TisoAud[TisoRandAudio.PickRandomTisoAud(2, 6)]);
            PlayAudio(_tc, Clip.ThrowShield);
            yield return _anim.PlayToEnd("TisoThrow");

            GameObject[] shields = {Object.Instantiate(_shield), Object.Instantiate(_shield)};
            _destroyOnDeath.AddRange(shields);
            int ind = 0;
            foreach (float i in new [] {0.4f, -0.4f})
            {
                GameObject shield = shields[ind];
                shield.transform.position = _shield.transform.position;
                shield.transform.localScale *= 2f;
                shield.SetActive(true);
                Shield shieldCtrl = shield.AddComponent<Shield>();
                shieldCtrl.horizDir = dir;
                shieldCtrl.vertDir = i;
                shieldCtrl.yOffset = i == 0 ? 0.25f : 0.5f;
                ind++;
            }
            yield return _anim.PlayToFrame("TisoThrowCatch", 1);
            _anim.enabled = false;
            Shield shCtrl = shields[0].GetComponent<Shield>();
            yield return new WaitWhile(() => !shCtrl.isDoneFlag ||
                (dir > 0
                ? shields[0].transform.position.x < transform.position.x - 3.5
                : shields[0].transform.position.x >= transform.position.x + 3.5f));
            _anim.enabled = true;
            _anim.speed = 3.5f;
            yield return _anim.PlayToFrame("TisoThrowCatch", 2);
            foreach (GameObject shield in shields) _destroyOnDeath.Remove(shield);
            foreach (GameObject shield in shields) Object.Destroy(shield);
            _anim.speed = 1f;
            yield return _anim.PlayToEnd();
        }

        public IEnumerator Walk(float towardsX)
        {
            towardsX = Mathf.Max(towardsX, TisoController.LeftX);
            towardsX = Mathf.Min(towardsX, TisoController.RightX);
            float dir = FacePos(new Vector2(towardsX, 0f));
            _anim.speed = 2f;
            _anim.Play("TisoRunStart", -1, 0f);
            yield return new WaitForEndOfFrame();
            _anim.enabled = false;
            yield return new WaitForSeconds(0.15f);
            _anim.enabled = true;
            yield return _anim.PlayToEnd();
            _anim.Play("TisoRun");
            
            bool isDoneWalking = false;
            
            PlayAudio(_tc, Clip.Walk, 1f, 0f, () => !isDoneWalking);
            
            _rb.velocity = new Vector2(RunSpeed * -dir, 0f);
            yield return new WaitWhile(() =>
                !CheckPlayerAbove() && (dir < 0
                    ? transform.position.x < towardsX && transform.position.x < TisoController.RightX
                    : transform.position.x > towardsX && transform.position.x > TisoController.LeftX));
            if (CheckPlayerAbove())
            {
                _anim.speed = 1f;
                _rb.velocity = Vector2.zero;
                yield return BlockUp();
                yield break;
            }

            isDoneWalking = true;
            yield return _anim.PlayToEnd("TisoRunEnd");
            _anim.speed = 1f;
            _rb.velocity = Vector2.zero;
        }

        public IEnumerator Dodge()
        {
            float dir = FaceHero();
            // If near the edges and backing into the edge, then jump off edge instead
            // Don't do this if already did JumpGlideSlam once
            if ((transform.position.x < TisoController.LeftX + 4f && dir < 0) ||
                 (transform.position.x > TisoController.RightX - 4f && dir > 0))
            {
                Modding.Logger.Log($"Choosing to do jump log spam {_tc.Rep[JumpGlideSlam]}");
                if (_tc.Rep[JumpGlideSlam] == 0)
                {
                    Modding.Logger.Log($"Can repeat jumpglideslam {_tc.Rep[JumpGlideSlam]}");
                    _tc.Rep[JumpGlideSlam]++;
                    yield return JumpGlideSlam();
                }
                else
                {
                    _tc.Rep[JumpGlideSlam] = 0;
                    yield return BlockUp();
                }
                yield break;
            }
            
            _rb.velocity = new Vector2(DodgeSpeed * dir, 0f);
            yield return _anim.PlayToEnd("TisoDodge");
            _rb.velocity = Vector2.zero;
        }
        
        private void SpawnShockwaves(float vertScale, float speed, int damage, Vector2 pos)
        {
            bool[] facingRightBools = {false, true};

            PlayMakerFSM fsm = FiveKnights.preloadedGO["Mage"].LocateMyFSM("Mage Lord");

            foreach (bool facingRight in facingRightBools)
            {
                GameObject shockwave = Object.Instantiate
                (
                    fsm.GetAction<SpawnObjectFromGlobalPool>("Quake Waves", 0).gameObject.Value
                );

                PlayMakerFSM shockFSM = shockwave.LocateMyFSM("shockwave");

                shockFSM.FsmVariables.FindFsmBool("Facing Right").Value = facingRight;
                shockFSM.FsmVariables.FindFsmFloat("Speed").Value = speed;

                shockwave.AddComponent<DamageHero>().damageDealt = damage;

                shockwave.SetActive(true);
        
                shockwave.transform.SetPosition2D(new Vector2(pos.x + (facingRight ? 0.5f : -0.5f), TisoController.GroundY - 0.5f));
                shockwave.transform.SetScaleX(vertScale);
            }
        }
        
        public IEnumerator Death()
        {
            PlayerData.instance.isInvincible = true;
            // Destroy no longer used objects like any shields that were spawned
            foreach (var go in _destroyOnDeath) Object.Destroy(go);
            GameObject mawlek = GameObject.Find("Mawlek Body");
            if (mawlek == null)
            {
                Logger.Log("Mawlek not Found");
                var battle = GameObject.Find("Battle Scene");
                mawlek = battle.transform.Find("Mawlek Body").gameObject;
            }
            
            float knockDir = FaceHero();
            _rb.isKinematic = false;
            _bc.enabled = false;
            _rb.gravityScale = 1.5f;
            _rb.velocity = new Vector2(knockDir * 10f, 30f);
            PlayDeathFor(transform.gameObject);
            _anim.enabled = true;
            //StartCoroutine(PlayDeathSound());
            _anim.speed = 1f;
            _anim.Play("TisoSpin");
            yield return new WaitForSeconds(0.1f);
            yield return new WaitWhile(() => transform.position.y > TisoController.GroundY);
            _bc.enabled = true;
            _rb.gravityScale = 0f;
            _rb.isKinematic = true;
            _rb.velocity = Vector2.zero;
            transform.position = new Vector3(transform.position.x, TisoController.GroundY);
            FaceHero();
            yield return _anim.PlayToEnd("TisoLand");
            _anim.Play("TisoRoar");
            PlayAudio(_tc, Clip.Roar);

            yield return new WaitForSeconds(TisoAud["AudTisoRoar"].length);
            
            mawlek.SetActive(true);
            mawlek.LocateMyFSM("Mawlek Control").FsmVariables.FindFsmBool("Skip Title").Value = true;
            mawlek.LocateMyFSM("Mawlek Control").FsmVariables.FindFsmBool("Skip Title").Value = true;
            mawlek.transform.position = new Vector2(transform.position.x, mawlek.transform.position.y);

            yield return new WaitWhile(() => mawlek.transform.position.y > 20f);
            var pos = new Vector2(
                TisoController.MiddleX > transform.position.x ? TisoController.LeftX - 5f : TisoController.RightX + 5f,
                0f);
            float dir = FacePos(pos);
            yield return SpecialDodge(dir);
            yield return SpecialDodge(dir);
            yield return ShootBomb(mawlek);

            IEnumerator SpecialDodge(float pos)
            {
                _rb.velocity = new Vector2((DodgeSpeed + 3f) * dir, 0f);
                yield return _anim.PlayToEnd("TisoDodge");
                _rb.velocity = Vector2.zero;
            }

            IEnumerator ShootBomb(GameObject mawlek)
            {
                float dir = FacePos(mawlek.transform.position) * -1f;
                // Try and stop the gun from being in front of its hinge
                Transform arm = transform.Find("SwapWeapon").Find("s3").Find("s8");
                arm.position = new Vector3(arm.position.x, arm.position.y, 0.5f);
                GameObject bombPar = arm.Find("Bomb").gameObject;
                yield return _anim.PlayToEnd("TisoShootAntic");
                for (int i = 0; i < NumShots; i++)
                {
                    // Player lower than tiso so I have to lower their height
                    Vector2 tarPos = mawlek.transform.position;
                    float angleOrig = GetAngleToPlayer(transform.Find("SwapWeapon").Find("s3").Find("s8").position,
                        tarPos, dir);
                    angleOrig = dir > 0
                        ? Mathf.Max(Mathf.Min(40f, angleOrig), -17f)
                        : Mathf.Max(Mathf.Min(15f, angleOrig), -42f);
                    float angle2 = dir > 0
                        ? Mathf.Max(Mathf.Min(17f, angleOrig), -16f)
                        : Mathf.Max(Mathf.Min(8f, angleOrig), -25f);
                    transform.Find("SwapWeapon").Find("s3").Find("s8").SetRotation2D(angle2);
                    transform.Find("SwapWeapon").Find("s3").SetRotation2D(angleOrig - angle2);
                    _anim.Play("TisoShoot", -1, 0f);
                    _anim.enabled = true;
                    GameObject bomb = Object.Instantiate(bombPar);
                    bomb.transform.localScale *= 1.9f;
                    bomb.transform.position = bombPar.transform.position + new Vector3(0f, 0f, 0.5f);
                    bomb.SetActive(true);
                    Animator bombAnim = bomb.GetComponent<Animator>();
                    if (i == NumShots - 1)
                    {
                        bomb.transform.localScale *= 1.1f;
                        bomb.transform.localScale = new Vector3(bomb.transform.localScale.x, Mathf.Abs(bomb.transform.localScale.y) * -dir, bomb.transform.localScale.z);
                        arm.Find("ShootFx").gameObject.SetActive(false);
                        yield return ExplodeBomb(bombAnim, 0.6f, 2f, null);
                        _anim.enabled = true;
                        _anim.Play("TisoDeath");
                        PlayAudio(_tc, Clip.Death);
                        _tc.PlayMusic(null);
                        transform.position -= new Vector3(0f, 0.5f, 0f);
                        transform.localScale.Scale(new Vector3(-1f, 1f, 1f));
                        yield break;
                    }
                    
                    var rb = bomb.GetComponent<Rigidbody2D>();
                    angle2 += dir > 0 ? 0f : 180f;
                    rb.velocity = new Vector2(Mathf.Cos(angle2 * Mathf.Deg2Rad), Mathf.Sin(angle2 * Mathf.Deg2Rad)) *
                                  35f;
                    _tc.StartCoroutine(ExplodeBomb(bombAnim, 1.5f, 1f, mawlek));
                    yield return _anim.PlayToEnd();
                }
            }

            IEnumerator ExplodeBomb(Animator bombAnim, float animSpd, float scale, GameObject mawlek)
            {
                bombAnim.speed = animSpd;
                if (mawlek == null)
                {
                    bombAnim.enabled = true;
                    yield return bombAnim.PlayToEnd("BombAir");
                }
                else
                {
                    var rb = bombAnim.GetComponent<Rigidbody2D>();
                    yield return new WaitWhile(() => rb.velocity.x > 0
                        ? bombAnim.transform.position.x < mawlek.transform.position.x
                        : bombAnim.transform.position.x > mawlek.transform.position.x);
                }
                GameObject explosion = Object.Instantiate(FiveKnights.preloadedGO["Explosion"]);
                explosion.transform.position = bombAnim.transform.position;
                explosion.transform.localScale *= scale;
                explosion.SetActive(true);
                Object.Destroy(bombAnim.gameObject);
            }
        }
        
        private void PlayDeathFor(GameObject go)
        {
            GameObject eff1 = Object.Instantiate(_deathEff.uninfectedDeathPt);
            GameObject eff2 = Object.Instantiate(_deathEff.whiteWave);

            eff1.SetActive(true);
            eff2.SetActive(true);

            eff1.transform.position = eff2.transform.position = go.transform.position;

            _deathEff.EmitSound();

            GameCameras.instance.cameraShakeFSM.SendEvent("EnemyKillShake");
        }
        
        private bool CheckPlayerAbove()
        {
            Vector2 hPos = _target.transform.position;
            Vector2 tPos = transform.position;
            var relativePt = transform.InverseTransformPoint(hPos);
            return hPos.x.Within(tPos.x, 2.5f) && relativePt.y > 2f;
        }
        
        IEnumerator DampToVel(Vector2 start, Vector2 end, float duration)
        {
            float time = 0f;
            Vector2 vel = Vector2.zero;
            while (time < duration)
            {
                _rb.velocity = Vector2.SmoothDamp(_rb.velocity, end, ref vel, duration);
                time += Time.deltaTime;
                yield return null;
            }
        }

        public float FaceHero(bool onlyCalc = false)
        {
            return FacePos(_target.transform.position, onlyCalc);
        }

        // If pos is on left, returns positive
        private float FacePos(Vector2 pos, bool onlyCalc = false)
        {
            float sign = Mathf.Sign(transform.position.x - pos.x);
            if (!onlyCalc)
            {
                Vector2 oldScale = transform.localScale;
                transform.localScale = new Vector3(Mathf.Abs(oldScale.x) * sign, oldScale.y);
            }
            return sign;
        }
    }
}