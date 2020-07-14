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
        public static bool ParryFlag;

        private void OnTriggerEnter2D(Collider2D col)
        {
            Log("Out " + ParryFlag);
            Log("Out2 " + col.gameObject.layer);
            if (ParryFlag || col.gameObject.layer != 16) return;
            Log("In " + col.name);
            ParryFlag = true;
            StartCoroutine(GameManager.instance.FreezeMoment(0.03f, 0.1f, 0.03f, 0f));
            HeroController.instance.NailParry();
            GameCameras.instance.cameraShakeFSM.SendEvent("EnemyKillShake");
            FiveKnights.preloadedGO["ClashTink"].transform.SetPosition2D(transform.position);
            FiveKnights.preloadedGO["ClashTink"].GetComponent<AudioSource>().Play();
            GameObject slash = col.transform.parent.gameObject;
            float degrees = 0f;
            PlayMakerFSM damagesEnemy = PlayMakerFSM.FindFsmOnGameObject(slash, "damages_enemy");
            if (damagesEnemy == null) return;
            degrees = damagesEnemy.FsmVariables.FindFsmFloat("direction").Value;
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
            Log("Trying to end parry1");
            StartCoroutine(DoNextFrame());
        }

        IEnumerator DoNextFrame()
        {
            yield return null;
            HeroController.instance.NailParryRecover();
            Log("Time");
            yield return new WaitForSecondsRealtime(0.5f);
            Log("Time2");
            ParryFlag = false;
        }

        private void Log(object ob)
        {
            Logger.Log("[Parryable] " + ob);
        }
    }
}