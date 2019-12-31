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

        private IEnumerator Start()
        {
            //Stuff for getting hegemol to work
            GameObject fk = Instantiate(FiveKnights.preloadedGO["fk"]);
            fk.GetComponent<tk2dSprite>().GetCurrentSpriteDef().material.mainTexture = FiveKnights.SPRITES[0].texture;
            fk.SetActive(true);
            Vector2 pos = HeroController.instance.transform.position;
            fk.transform.SetPosition2D(pos.x, 23f);
            PlayMakerFSM fsm = fk.LocateMyFSM("FalseyControl");
            fsm.SetState("Init");
            yield return new WaitWhile(() => fsm.ActiveStateName != "Dormant");
            fsm.SendEvent("BATTLE START");
            while (true)
            {
                if (fsm.ActiveStateName == "JA Check Hero Pos")
                {
                    float diff = HeroController.instance.transform.position.x - fk.transform.position.x;
                    if (diff > 0) fsm.SendEvent("RIGHT");
                    else fsm.SendEvent("LEFT");
                }
                yield return new WaitForEndOfFrame();
            }
        }

        private void Log(object o)
        {
            Modding.Logger.Log("[Hegemol] " + o);
        }
    }
}
