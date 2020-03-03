using HutongGames.PlayMaker.Actions;
using ModCommon.Util;
using ModCommon;
using Modding;
using On;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using Logger = Modding.Logger;

namespace FiveKnights
{
    internal class Parryable : MonoBehaviour
    {
        public static bool doingOne = false;

        private void OnTriggerEnter2D(Collider2D col)
        {
            if (doingOne || col.gameObject.layer != 16) return;
            doingOne = true;
            //GameManager.instance.FreezeMoment(1);
            StartCoroutine(GameManager.instance.FreezeMoment(0.04f, 0.15f, 0.04f, 0f));
            HeroController.instance.NailParry();
            GameCameras.instance.cameraShakeFSM.SendEvent("EnemyKillShake");
            FiveKnights.preloadedGO["ClashTink"].transform.SetPosition2D(transform.position);
            FiveKnights.preloadedGO["ClashTink"].GetComponent<AudioSource>().Play();
            GameObject slash = col.transform.parent.gameObject;
            float degrees = 0f;
            PlayMakerFSM damagesEnemy = PlayMakerFSM.FindFsmOnGameObject(slash, "damages_enemy");
            if (damagesEnemy != null)
            {
                degrees = damagesEnemy.FsmVariables.FindFsmFloat("direction").Value;
            }
            else return;
            Vector3 pos = new Vector3();
            if (degrees < 45f)
            {
                HeroController.instance.RecoilLeft();
                pos = new Vector2(1.5f, 0f);
            }
            else if (degrees < 135f)
            {
                HeroController.instance.RecoilDown();
                pos = new Vector2(0f, 1.5f);
            }
            else if (degrees < 225f)
            {
                HeroController.instance.RecoilRight();
                pos = new Vector2(-1.5f, 0f);
            }
            else
            {
                HeroController.instance.Bounce();
                pos = new Vector2(0f, -1.5f);
            }

            GameObject fx = Instantiate(FiveKnights.preloadedGO["parryFX"]);
            fx.transform.SetPosition2D(HeroController.instance.transform.position);
            fx.transform.position += pos;
            fx.SetActive(true);
            StartCoroutine(EndParry());
        }

        IEnumerator EndParry()
        {
            yield return null;
            HeroController.instance.NailParryRecover();
            yield return new WaitForSeconds(0.2f);
            doingOne = false;
            Log("No Error2");
        }

        private void Log(object ob)
        {
            Logger.Log("[Parryable] " + ob);
        }
    }
}