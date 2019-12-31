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
            gameObject.transform.localScale *= 1.4f;
            PositionIsma();
            FaceHero();
            _anim.Play("Bow");
            yield return new WaitForSeconds(0.1f);
            yield return new WaitWhile(() => _anim.IsPlaying());
            yield return new WaitForSeconds(1f);
            _anim.Play("EndBow");
            yield return new WaitForSeconds(0.1f);
            yield return new WaitWhile(() => _anim.IsPlaying());
            StartCoroutine(SmashBall());
            ToggleIsma(false);
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
                yield return new WaitForEndOfFrame();
            }
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