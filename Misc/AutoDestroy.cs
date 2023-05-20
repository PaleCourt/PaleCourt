using System;
using System.Collections;
using UnityEngine;

namespace FiveKnights
{
    internal class AutoDestroy : MonoBehaviour
    {
        internal float Time { get; set; } = 0.7f;

        public Func<bool> ShouldDestroy;

        private void Start() => StartCoroutine(Wait());

        private IEnumerator Wait()
        {
            if (ShouldDestroy != null)
            {
                yield return new WaitWhile(() => !ShouldDestroy.Invoke());
            }
            else
            {
                yield return new WaitForSeconds(Time);
            }

            Destroy(gameObject);
        }
    }
}