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
    public class WDController : MonoBehaviour
    {
        private HealthManager _hm;
        private PlayMakerFSM _fsm;

        private void Awake()
        {
            _hm = gameObject.GetComponent<HealthManager>();
            _fsm = gameObject.LocateMyFSM("Dung Defender");
            FiveKnights.preloadedGO["WD"] = gameObject;
        }

        private IEnumerator Start()
        {
            _hm.hp = 601; //Set to 950 normally
            yield return new WaitWhile(() => _hm.hp > 600);
            _fsm.GetAction<Wait>("Rage Roar", 9).time = 3.5f;
            yield return new WaitWhile(() => !_fsm.ActiveStateName.Contains("Rage Roar"));
            //FightController.Instance.CreateIsma();
            
            GameObject dryyaSilhouette = GameObject.Find("Silhouette Dryya");
            dryyaSilhouette.GetComponent<SpriteRenderer>().sprite = ArenaFinder.sprites["Dryya_Silhouette_1"];
            yield return new WaitForSeconds(0.125f);
            dryyaSilhouette.GetComponent<SpriteRenderer>().sprite = ArenaFinder.sprites["Dryya_Silhouette_2"];
            yield return new WaitForSeconds(0.125f);
            Destroy(dryyaSilhouette);
            yield return new WaitForSeconds(0.5f);
            FightController.Instance.CreateDryya();
        }

        private void Log(object o)
        {
            Modding.Logger.Log("[White Defender] " + o);
        }
    }
}
