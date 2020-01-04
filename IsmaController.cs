using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Modding;
using System.Collections;
using HutongGames.PlayMaker;
using HutongGames.PlayMaker.Actions;
using ModCommon.Util;
using ModCommon;
using Object = UnityEngine.Object;
using System.Reflection;

namespace FiveKnights
{
    public class IsmaController : MonoBehaviour
    {
        //Note: Dreamnail code was taken from Jngo's code :)

        private bool flashing;
        private bool _attacking;
        private bool _plantpillar;
        private HealthManager _hm;
        private BoxCollider2D _bc;
        private HealthManager _hmDD;
        private AudioSource _aud;
        private EnemyHitEffectsUninfected _hitEff;
        private EnemyDreamnailReaction _dnailReac;
        private GameObject _dnailEff;
        private SpriteRenderer _sr;
        private GameObject _target;
        public GameObject dd;
        private PlayMakerFSM _ddFsm;
        private Animator _anim;
        private System.Random _rand;
        private int _healthPool;
        private const float LEFT_X = 60.3f;
        private const float RIGHT_X = 90.6f;
        private const float GROUND_Y = 5.9f;
        private string[] _dnailDial =
        {
            "ISMA_DREAM_1",
            "ISMA_DREAM_2",
            "ISMA_DREAM_3"
        };

        private void Awake()
        {
            _hm = gameObject.GetComponent<HealthManager>();
            _sr = gameObject.GetComponent<SpriteRenderer>();
            _anim = gameObject.GetComponent<Animator>();
            _bc = gameObject.GetComponent<BoxCollider2D>();
            _dnailReac = gameObject.AddComponent<EnemyDreamnailReaction>();
            _hitEff = gameObject.AddComponent<EnemyHitEffectsUninfected>();
            _aud = gameObject.AddComponent<AudioSource>();
            _dnailReac.enabled = true;
            _hitEff.enabled = true;
            _rand = new System.Random();
            _dnailReac.SetConvoTitle(_dnailDial[_rand.Next(_dnailDial.Length)]);
            _healthPool = 600;
        }

        private IEnumerator Start()
        {
            Log("Begin Isma");
            yield return new WaitWhile(() => !dd);
            _hmDD = dd.GetComponent<HealthManager>();
            _ddFsm = dd.LocateMyFSM("Dung Defender");
            _dnailEff = dd.GetComponent<EnemyDreamnailReaction>().GetAttr<EnemyDreamnailReaction, GameObject>("dreamImpactPrefab");
            SpriteRenderer sil = GameObject.Find("Silhouette Isma").GetComponent<SpriteRenderer>();
            sil.sprite = ArenaFinder.sprites["isma_sil_0"];
            sil.transform.localScale *= 1.15f;
            yield return new WaitForSeconds(0.15f);
            sil.gameObject.SetActive(false);
            yield return new WaitForSeconds(1f);
            On.HealthManager.TakeDamage += HealthManager_TakeDamage;
            On.EnemyDreamnailReaction.RecieveDreamImpact += OnReceiveDreamImpact;
            _hm.hp = 600;
            gameObject.layer = 11;
            _target = HeroController.instance.gameObject;
            AssignFields();
            PositionIsma();
            FaceHero();
            _anim.Play("Apear"); //Yes I know it's "appear," I don't feel like changing the assetbundle buddo
            yield return new WaitForSeconds(0.05f);
            yield return new WaitWhile(() => _anim.IsPlaying());
            yield return new WaitForSeconds(0.5f);
            _anim.Play("Bow");
            yield return new WaitForSeconds(0.1f);
            yield return new WaitWhile(() => _anim.IsPlaying());
            yield return new WaitForSeconds(1f);
            _anim.Play("EndBow");
            yield return new WaitForSeconds(0.1f);
            yield return new WaitWhile(() => _anim.IsPlaying());
            GameObject whip = transform.Find("Whip").gameObject;
            whip.AddComponent<DamageHero>().damageDealt = 1;
            whip.layer = 11;
            PlantPillar();
            StartCoroutine(SmashBall());
            StartCoroutine(AttackChoice());
            ToggleIsma(false);
            Log("Isma Attack");
        }

        private void Update()
        {
            if (_healthPool <= 0)
            {
                Log("Victory");
                _hm.Die(new float?(0f), AttackTypes.Nail, true);
                _hmDD.Die(new float?(0f), AttackTypes.Nail, true);
                Destroy(this);
            }
        }

        private IEnumerator AttackChoice()
        {
            while (true)
            {
                yield return new WaitForSeconds(UnityEngine.Random.Range(3f, 5f));
                if (!_attacking)
                {
                    StartCoroutine(AirFist());
                }
            }
        }

        private void PlantPillar()
        {
            List<float> PlantX = new List<float>();
            IEnumerator PlantChecker()
            {
                while (true)
                {
                    if (_ddFsm.ActiveStateName == "RJ Antic")
                    {
                        Coroutine c = StartCoroutine(PlantPillar());
                        yield return new WaitWhile(() => _ddFsm.ActiveStateName != "Idle");
                        StopCoroutine(c);
                    }
                    yield return new WaitForEndOfFrame();
                }
            }

            IEnumerator PlantPillar()
            {
                while (true)
                {
                    float posX = _target.transform.GetPositionX();
                    bool skip = false;
                    foreach (float i in PlantX.Where(x => FastApproximately(x, posX, 4f))) skip = true;
                    if (skip) continue;
                    PlantX.Add(posX);
                    GameObject plant = Instantiate(FiveKnights.preloadedGO["Plant"]);
                    plant.transform.position = new Vector2(posX, GROUND_Y);
                    plant.AddComponent<PlantCtrl>().PlantX = PlantX;
                    yield return new WaitForSeconds(UnityEngine.Random.Range(2, 3));
                }
            }
            StartCoroutine(PlantChecker());
        }

        private IEnumerator AirFist()
        {
            float heroX = _target.transform.GetPositionX();
            float distance = UnityEngine.Random.Range(7, 15);
            float ismaX = heroX - 75f > 0f ? heroX - distance : heroX + distance;
            transform.position = new Vector2(ismaX, UnityEngine.Random.Range(10,16));
            ToggleIsma(true);
            float dir = FaceHero();
            GameObject arm = transform.Find("Arm2").gameObject;
            Vector2 diff = arm.transform.position - _target.transform.position;
            float rot = Mathf.Atan(diff.y / diff.x);
            SpriteRenderer armSpr = arm.GetComponent<SpriteRenderer>();
            GameObject spike = transform.Find("SpikeArm").gameObject;
            arm.transform.SetRotation2D(rot * Mathf.Rad2Deg);
            GameObject stripStr = arm.transform.Find("StripStart").gameObject;
            stripStr.SetActive(false);
            GameObject rstrip = arm.transform.Find("StripEnd").gameObject;
            GameObject rfist = arm.transform.Find("SpikeFistB").gameObject;
            GameObject strip = Instantiate(rstrip, rstrip.transform.position, rstrip.transform.rotation);
            GameObject fist = Instantiate(rfist, rfist.transform.position, rfist.transform.rotation);
            Rigidbody2D fstRB = fist.GetComponent<Rigidbody2D>();
            SpriteRenderer stpSR = strip.GetComponent<SpriteRenderer>();
            strip.SetActive(false);
            fist.SetActive(false);
            AirFistCollision afc = fist.AddComponent<AirFistCollision>();
            Vector3 strSC = strip.transform.localScale;
            Vector3 fstSc = fist.transform.localScale;
            strip.transform.localScale = new Vector3(dir * strSC.x, strSC.y, strSC.z) * 1.6f;
            fist.transform.localScale = new Vector3(dir * fstSc.x, fstSc.y, fstSc.z) * 1.4f;
            _anim.Play("AFistAntic");
            yield return new WaitForSeconds(0.05f);
            yield return new WaitWhile(() => _anim.IsPlaying());
            spike.SetActive(true);
            yield return new WaitForSeconds(0.01f);
            spike.SetActive(false);
            arm.SetActive(true);
            armSpr.enabled = true;
            stripStr.SetActive(true);
            strip.SetActive(true);
            fist.SetActive(true);
            _anim.Play("AFist");
            fstRB.velocity = new Vector2(dir * 60f * Mathf.Cos(rot), dir * 60f * Mathf.Sin(rot));
            int i = 0;
            while (!afc.isHit)
            {
                stpSR.size = new Vector2(stpSR.size.x + 0.68f, 0.42f);
                i++;
                yield return new WaitForSeconds(0.01f);
            }
            fstRB.velocity = new Vector2(dir * -60f * Mathf.Cos(rot), dir * -60f * Mathf.Sin(rot));
            while (i > 0)
            {
                stpSR.size = new Vector2(stpSR.size.x - 0.68f, 0.42f);
                i--;
                yield return new WaitForSeconds(0.01f);
            }
            fstRB.velocity = Vector2.zero;
            armSpr.enabled = false;
            stripStr.SetActive(false);
            Destroy(strip);
            Destroy(fist);
            spike.SetActive(true);
            _anim.Play("AFist2");
            yield return new WaitForSeconds(0.05f);
            spike.SetActive(false);
            _anim.Play("AFistEnd");
            yield return new WaitForSeconds(0.05f);
            yield return new WaitWhile(() => _anim.IsPlaying());
            ToggleIsma(false);
        }

        private IEnumerator WhipAttack()
        {
            float heroX = _target.transform.GetPositionX();
            float ismaX = heroX - 75f > 0f ? UnityEngine.Random.Range(62,66) : UnityEngine.Random.Range(85,88);
            transform.position = new Vector2(ismaX, GROUND_Y);
            ToggleIsma(true);
            FaceHero();
            
            GameObject fist = transform.Find("Arm").gameObject;
            GameObject whip = transform.Find("Whip").gameObject;
            Animator anim = whip.GetComponent<Animator>();

            _sr.enabled = true;
            _anim.Play("GFistAntic");
            yield return new WaitForSeconds(0.05f);
            yield return new WaitWhile(() => _anim.IsPlaying());
            whip.SetActive(true);
            anim.Play("Whip");
            yield return new WaitWhile(() => anim.GetCurrentFrame() < 1);
            _anim.Play("GFist");
            fist.SetActive(true);
            yield return new WaitWhile(() => anim.GetCurrentFrame() < 13);
            _anim.Play("GFist2");
            fist.SetActive(false);
            yield return new WaitWhile(() => anim.GetCurrentFrame() < 14);
            _anim.Play("GFist3");
            yield return new WaitWhile(() => anim.IsPlaying());
            anim.Play("Idle");
            whip.SetActive(false);
            _anim.Play("GFistEnd");
            yield return new WaitForSeconds(0.05f);
            yield return new WaitWhile(() => _anim.IsPlaying());
            ToggleIsma(false);
        }

        private IEnumerator SmashBall()
        {
            while (true)
            {
                if (!_attacking)
                {
                    foreach (GameObject go in FindObjectsOfType<GameObject>().Where(x => x.name.Contains("Dung Ball") && x.activeSelf && x.transform.GetPositionY() > 15f
                            && x.transform.GetPositionY() < 18f && x.GetComponent<Rigidbody2D>().velocity.y > 0f))
                    {
                        Vector2 pos = go.transform.position;
                        ToggleIsma(true);
                        float side = go.GetComponent<Rigidbody2D>().velocity.x > 0f ? 1f : -1f;
                        gameObject.transform.position = new Vector2(pos.x + side * 1.77f, pos.y + 0.38f);
                        float dir = FaceHero();
                        GameObject squish = gameObject.transform.Find("Squish").gameObject;
                        GameObject ball = Instantiate(gameObject.transform.Find("Ball").gameObject);
                        ball.transform.localScale *= 1.4f;
                        ball.layer = 11;
                        ball.AddComponent<DamageHero>().damageDealt = 1;
                        ball.AddComponent<DungBall>();
                        _anim.Play("BallStrike");
                        yield return new WaitForSeconds(0.05f);
                        yield return new WaitWhile(() => _anim.GetCurrentFrame() < 2);
                        Destroy(go);
                        squish.SetActive(true);
                        yield return new WaitWhile(() => _anim.GetCurrentFrame() <= 2);
                        GameObject ballFx = ball.transform.Find("BallFx").gameObject;
                        squish.SetActive(false);
                        ball.SetActive(true);
                        ball.transform.position = gameObject.transform.Find("Ball").position;
                        ballFx.transform.parent = null;
                        Vector2 diff = ball.transform.position - _target.transform.position;
                        float rot = Mathf.Atan(diff.y / diff.x);
                        rot += dir > 0 ? 0f : Mathf.PI;
                        ball.transform.SetRotation2D(rot * Mathf.Rad2Deg + 90f);
                        Vector2 vel = new Vector2(30f * Mathf.Cos(rot), 30f * Mathf.Sin(rot));
                        ball.GetComponent<Rigidbody2D>().velocity = vel;
                        yield return new WaitForSeconds(0.1f);
                        ballFx.GetComponent<Animator>().Play("FxEnd");
                        yield return new WaitForSeconds(0.1f);
                        Destroy(ballFx);
                        yield return new WaitWhile(() => _anim.IsPlaying());
                        ToggleIsma(false);
                        yield return new WaitForSeconds(1.5f);
                        break;
                    }
                }
                yield return new WaitForEndOfFrame();
            }
        }

        private void AssignFields()
        {
            EnemyHitEffectsUninfected ogrimHitEffects = dd.GetComponent<EnemyHitEffectsUninfected>();
            foreach (FieldInfo fi in typeof(EnemyHitEffectsUninfected).GetFields(BindingFlags.Instance | BindingFlags.Public))
            {
                fi.SetValue(_hitEff, fi.GetValue(ogrimHitEffects));
            }
        }

        private void OnReceiveDreamImpact(On.EnemyDreamnailReaction.orig_RecieveDreamImpact orig, EnemyDreamnailReaction self)
        {
            if (self.name.Contains("Isma"))
            {
                FlashWhite();
                Instantiate(_dnailEff, transform.position, Quaternion.identity);
                _dnailReac.SetConvoTitle(_dnailDial[_rand.Next(_dnailDial.Length)]);
            }
            orig(self);
        }

        private void HealthManager_TakeDamage(On.HealthManager.orig_TakeDamage orig, HealthManager self, HitInstance hitInstance)
        {
            if (self.name.Contains("Isma"))
            {
                if (!flashing)
                {
                    flashing = true;
                    StartCoroutine(FlashWhite());
                }
            }
            _healthPool -= hitInstance.DamageDealt;
            orig(self, hitInstance);
        }

        IEnumerator FlashWhite()
        {
            _sr.material.SetFloat("_FlashAmount", 1f);
            yield return null;
            for (float i = 1f; i >= 0f; i -= 0.05f)
            {
                _sr.material.SetFloat("_FlashAmount", i);
                yield return null;
            }
            yield return null;
            flashing = false;
        }

        private float FaceHero(bool opposite = false)
        {
            float heroSignX = Mathf.Sign(_target.transform.GetPositionX() - gameObject.transform.GetPositionX());
            heroSignX = opposite ? -1f * heroSignX : heroSignX;
            Vector3 pScale = gameObject.transform.localScale;
            gameObject.transform.localScale = new Vector3(Mathf.Abs(pScale.x) * heroSignX, pScale.y, 1f);
            return heroSignX;
        }

        private void ToggleIsma(bool visible)
        {
            _anim.Play("Idle");
            _attacking = visible;
            _sr.enabled = visible;
            _bc.enabled = visible;
        }

        private void PositionIsma()
        {
            float xPos = 80f;
            float changeXPos = 0f;
            float ddX = dd.transform.GetPositionX();
            float heroX = _target.transform.GetPositionX();

            if (FastApproximately(xPos, ddX, 2f)) changeXPos += 4f;
            if (FastApproximately(xPos, heroX, 2f)) changeXPos -= 4f;
            xPos += changeXPos;
            gameObject.transform.position = new Vector3(xPos, GROUND_Y, 1f);
        }

        public static bool FastApproximately(float a, float b, float threshold)
        {
            return ((a - b) < 0 ? ((a - b) * -1) : (a - b)) <= threshold;
        }

        private void OnDestroy()
        {
            On.HealthManager.TakeDamage -= HealthManager_TakeDamage;
            On.EnemyDreamnailReaction.RecieveDreamImpact -= OnReceiveDreamImpact;
        }

        private void Log(object o)
        {
            Modding.Logger.Log("[Isma] " + o);
        }
    }
}
/*
 * 
*/