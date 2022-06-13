using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace FiveKnights.Dryya
{
    public class Dagger : MonoBehaviour
    {
        public float Speed = 10f;
        private void FixedUpdate()
        {
            transform.position += transform.up * Time.fixedDeltaTime * Speed;
        }
    }
}
