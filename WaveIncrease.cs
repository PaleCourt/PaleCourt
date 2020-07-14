using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Modding;
using System.Collections;
using HutongGames.PlayMaker.Actions;
using ModCommon.Util;
using ModCommon;
using HutongGames.PlayMaker;
using Object = UnityEngine.Object;
using System.Reflection;

namespace FiveKnights
{
    public class WaveIncrease : MonoBehaviour
    {
        void Update ()
        {
            transform.localScale += new Vector3(0.02f, 0.02f, 0f);
            if(transform.localScale.x > 5f)
            {
                Modding.Logger.Log("KILL");
                Destroy(gameObject);
            }

        }
    }
}