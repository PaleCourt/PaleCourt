using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Modding;
using ModCommon;
using System.Collections;
using HutongGames.PlayMaker;
using HutongGames.PlayMaker.Actions;
using HutongGames.Utility;
using ModCommon.Util;
using Object = UnityEngine.Object;

namespace FiveKnights
{
    public class ZemerController : MonoBehaviour
    {
        private HealthManager _hm;
        private Animator _anim;
        private Rigidbody2D _rb;
        private GameObject _target;
        private const float GROUND_Y = 9.1f;

        private void Awake()
        {
            _hm = gameObject.AddComponent<HealthManager>();
            _anim = gameObject.GetComponent<Animator>();
            _rb = gameObject.GetComponent<Rigidbody2D>();
            gameObject.AddComponent<Flash>();
        }

        private IEnumerator Start()
        {
            Log("Start");
            _hm.hp = 1000;
            _target = HeroController.instance.gameObject;
            StartCoroutine(SilLeave());
            yield return new WaitForSeconds(0.15f);
            gameObject.SetActive(true);
            gameObject.transform.position = new Vector2(_target.transform.GetPositionX() - 10f, GROUND_Y);
            GameCameras.instance.cameraShakeFSM.SendEvent("BigShake");
            FaceHero();
            _anim.Play("ZapIn");
            yield return new WaitForSeconds(0.05f);
            yield return new WaitWhile(() => _anim.IsPlaying());
            yield return new WaitForSeconds(0.7f);
            _anim.Play("Turn");
            yield return new WaitForSeconds(0.05f);
            yield return new WaitWhile(() => _anim.IsPlaying());
            _anim.Play("BowStart");
            yield return new WaitForSeconds(0.05f);
            yield return new WaitWhile(() => _anim.IsPlaying());
            _anim.Play("BowEnd");
            Log("END");
        }

        private IEnumerator SilLeave()
        {
            SpriteRenderer sil = GameObject.Find("Silhouette Zemer").GetComponent<SpriteRenderer>();
            sil.transform.localScale *= 1.15f;
            sil.gameObject.AddComponent<Rigidbody2D>().velocity = new Vector2(0f, 10f);
            sil.gameObject.GetComponent<Rigidbody2D>().gravityScale = 0f;
            sil.sprite = ArenaFinder.sprites["Zem_Sil_1"];
            yield return new WaitForSeconds(0.05f);
            sil.sprite = ArenaFinder.sprites["Zem_Sil_2"];
            yield return new WaitForSeconds(0.05f);
            sil.sprite = ArenaFinder.sprites["Zem_Sil_3"];
            yield return new WaitForSeconds(0.05f);
            sil.gameObject.SetActive(false);
        }

        private float FaceHero(bool opposite = false)
        {
            float heroSignX = Mathf.Sign(gameObject.transform.GetPositionX() - _target.transform.GetPositionX());
            heroSignX = opposite ? -1f * heroSignX : heroSignX;
            Vector3 pScale = gameObject.transform.localScale;
            gameObject.transform.localScale = new Vector3(Mathf.Abs(pScale.x) * heroSignX, pScale.y, 1f);
            return heroSignX;
        }

        private void Log(object o)
        {
            Modding.Logger.Log("[Zemer] " + o);
        }
    }
}
