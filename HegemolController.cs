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
    public class HegemolController : MonoBehaviour
    {
        private HealthManager _hm;
        private PlayMakerFSM _fsm;

        private void Awake()
        {
            _hm = gameObject.GetComponent<HealthManager>();
        }

        private void Start()
        {
            //Stuff for getting hegemol to work
            /*
            Log("STUCK1");
            GameObject fk = Instantiate(FiveKnights.preloadedGO["fk"]);
            fk.GetComponent<tk2dSprite>().GetCurrentSpriteDef().material.mainTexture = FiveKnights.SPRITES[0].texture;
            fk.SetActive(true);
            Vector2 pos = HeroController.instance.transform.position;
            fk.transform.SetPosition2D(pos.x, 23f);
            Log("STUCK2");
            PlayMakerFSM fsm = fk.LocateMyFSM("FalseyControl");
            fsm.SetState("Init");
            Log(1);
            yield return new WaitWhile(() => fsm.ActiveStateName != "Dormant");
            Log(2);
            fsm.SendEvent("BATTLE START");
            Log(3);
            //yield return new WaitWhile(()=>fsm.ActiveStateName !=)
            bool hold = false;
            while (true)
            {
                if (Input.GetKeyDown(KeyCode.T))
                {
                    hold = !hold;
                }
                if (fsm.ActiveStateName == "JA Check Hero Pos")
                {
                    float diff = HeroController.instance.transform.position.x - fk.transform.position.x;
                    if (diff > 0) fsm.SendEvent("RIGHT");
                    else fsm.SendEvent("LEFT");
                }
                Log(fsm.ActiveStateName);
                yield return new WaitForEndOfFrame();
            }
            */
        }

        private void Log(object o)
        {
            Modding.Logger.Log("[Hegemol] " + o);
        }
    }
}
