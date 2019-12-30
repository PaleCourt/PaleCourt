using System.Collections;
using System.Collections.Generic;
using System.Linq;
using HutongGames.PlayMaker;
using HutongGames.PlayMaker.Actions;
using JetBrains.Annotations;
using ModCommon.Util;
using ModCommon;
using Modding;
using UnityEngine;
using UnityEngine.UI;
using Logger = Modding.Logger;
using USceneManager = UnityEngine.SceneManagement.SceneManager;

namespace FiveKnights
{
    internal class DungPillar : MonoBehaviour
    {
        private tk2dSpriteAnimator _anim;
        private float waitTime = 0f;
        private bool startSecondAnim;
        private bool startLastAnim;

        void Start()
        {
            try
            {
                _anim = gameObject.GetComponent<tk2dSpriteAnimator>();
                gameObject.GetComponent<PolygonCollider2D>().enabled = false;
                gameObject.GetComponent<MeshRenderer>().enabled = true;
                _anim.Play("Dung Pillar Up");
                waitTime = 0.1f;
            }
            catch (System.Exception e)
            {
                Log(e);
            }
        }

        void Update()
        {
            waitTime -= Time.deltaTime;
            if (waitTime <= 0f)
            {
                if (!startSecondAnim)
                {
                    _anim.Play("Dung Pillar Idle");
                    waitTime = 0.07f;
                    gameObject.GetComponent<PolygonCollider2D>().enabled = true;
                    startSecondAnim = true;
                }
                else if (startSecondAnim && !startLastAnim)
                {
                    _anim.Play("Dung Pillar Break");
                    waitTime = _anim.GetClipByName("Dung Pillar Break").Duration;
                    gameObject.GetComponent<PolygonCollider2D>().enabled = false;
                    startLastAnim = true;
                }
                else if (startSecondAnim && startLastAnim)
                {
                    Destroy(gameObject);
                }
            }
        }
        private static void Log(object obj)
        {
            Logger.Log("[Dung Pillars] " + obj);
        }
    }
}