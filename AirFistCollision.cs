using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Modding;

namespace FiveKnights
{
    public class AirFistCollision : MonoBehaviour
    {
        public bool isHit { get; set; }

        private void OnTriggerEnter2D(Collider2D col)
        {
            if (col.gameObject.layer == 8)
            {
                isHit = true;
            }
        }

        private static void Log(object obj)
        {
            Modding.Logger.Log("[Air Fist] " + obj);
        }
    }
}
