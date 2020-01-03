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

namespace FiveKnights
{
    public class IsmaController : MonoBehaviour
    {
        private bool flashing;
        private HealthManager _hm;
        private BoxCollider2D _bc;
        private HealthManager _hmDD;
        private SpriteRenderer _sr;
        private GameObject _target;
        public GameObject dd;
        private Animator _anim;
        private int _healthPool;
        private bool _attacking;
        private const float LEFT_X = 60.3f;
        private const float RIGHT_X = 90.6f;
        private const float GROUND_Y = 5.9f;

        private void Awake()
        {
            _hm = gameObject.GetComponent<HealthManager>();
            _sr = gameObject.GetComponent<SpriteRenderer>();
            _anim = gameObject.GetComponent<Animator>();
            _bc = gameObject.GetComponent<BoxCollider2D>();
            _healthPool = 600;
        }

        private IEnumerator Start()
        {
            Log("Begin Isma");
            yield return new WaitWhile(() => !dd);
            _hmDD = dd.GetComponent<HealthManager>();
            SpriteRenderer sil = GameObject.Find("Silhouette Isma").GetComponent<SpriteRenderer>();
            sil.sprite = ArenaFinder.sprites["isma_sil_0"];
            sil.transform.localScale *= 1.15f;
            yield return new WaitForSeconds(0.15f);
            sil.gameObject.SetActive(false);
            yield return new WaitForSeconds(1f);
            On.HealthManager.TakeDamage += HealthManager_TakeDamage;
            _hm.hp = 600;
            gameObject.layer = 11;
            _target = HeroController.instance.gameObject;
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
            //StartCoroutine(SmashBall());
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
            GameObject rstrip = arm.transform.Find("Strip").gameObject;
            GameObject rfist = arm.transform.Find("SpikeFistB").gameObject;
            GameObject strip = Instantiate(rstrip, rstrip.transform.position, rstrip.transform.rotation);
            GameObject fist = Instantiate(rfist, rfist.transform.position, rfist.transform.rotation);
            Rigidbody2D fstRB = fist.GetComponent<Rigidbody2D>();
            SpriteRenderer stpSR = strip.GetComponent<SpriteRenderer>();
            strip.SetActive(false);
            fist.SetActive(false);
            Vector3 strSC = strip.transform.localScale;
            Vector3 fstSc = fist.transform.localScale;
            strip.transform.localScale = new Vector3(dir * strSC.x, strSC.y, strSC.z) * 1.3f;
            fist.transform.localScale = new Vector3(dir * fstSc.x, fstSc.y, fstSc.z) * 1.4f;

            _anim.Play("AFistAntic");
            yield return new WaitForSeconds(0.05f);
            yield return new WaitWhile(() => _anim.IsPlaying());
            spike.SetActive(true);
            yield return new WaitForSeconds(0.01f);
            spike.SetActive(false);
            arm.SetActive(true);
            armSpr.enabled = true;
            strip.SetActive(true);
            fist.SetActive(true);
            _anim.Play("AFist");
            fstRB.velocity = new Vector2(dir * 60f * Mathf.Cos(rot), dir * 60f * Mathf.Sin(rot));
            float time = 0.45f;
            int i = 0;
            while (time > 0f)
            {
                stpSR.size = new Vector2(stpSR.size.x + 0.75f, 0.11f);
                time -= 0.03f;
                i++;
                yield return new WaitForSeconds(0.01f);
            }
            fstRB.velocity = new Vector2(dir * -60f * Mathf.Cos(rot), dir * -60f * Mathf.Sin(rot));
            while (i > 0)
            {
                stpSR.size = new Vector2(stpSR.size.x - 0.75f, 0.11f);
                i--;
                yield return new WaitForSeconds(0.01f);
            }
            fstRB.velocity = Vector2.zero;
            armSpr.enabled = false;
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
                    foreach (GameObject go in FindObjectsOfType<GameObject>().Where(x => x.name.Contains("Dung Ball") && x.activeSelf && x.transform.GetPositionY() > 16f))
                    {
                        Vector2 pos = go.transform.position;
                        Destroy(go);
                        ToggleIsma(true);
                        gameObject.transform.position = new Vector2(pos.x + 1.77f, pos.y - 0.38f);
                        float dir = FaceHero();
                        GameObject squish = gameObject.transform.Find("Squish").gameObject;
                        GameObject ball = Instantiate(gameObject.transform.Find("Ball").gameObject);
                        ball.transform.localScale *= 1.4f;
                        squish.SetActive(true);
                        ball.layer = 11;
                        ball.AddComponent<DamageHero>().damageDealt = 1;
                        ball.AddComponent<DungBall>();
                        _anim.Play("BallStrike");
                        yield return new WaitForSeconds(0.05f);
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