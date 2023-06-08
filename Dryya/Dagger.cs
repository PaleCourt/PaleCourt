using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace FiveKnights.Dryya
{
    public class Dagger : MonoBehaviour
    {
        public float Speed = 69f; // unused now, keeping so I don't need to rebuild the bundle
        private const float ActualSpeed = 50f;

        private void Start()
        {
            StartCoroutine(WaitDestroy());
        }

        private IEnumerator WaitDestroy()
        {
            yield return new WaitForSeconds(10f);
            Destroy(gameObject);
        }

        private void FixedUpdate()
        {
            transform.position += transform.up * Time.fixedDeltaTime * ActualSpeed;
        }
    }
}
