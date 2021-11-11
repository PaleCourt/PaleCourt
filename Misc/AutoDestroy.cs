using System.Collections;
using UnityEngine;

namespace FiveKnights
{
    internal class AutoDestroy : MonoBehaviour
    {
        internal float Time { get; set; } = 0.7f;
        
        private void Start() => StartCoroutine(Wait());

        private IEnumerator Wait()
        {
            yield return new WaitForSeconds(Time);

            Destroy(gameObject);
        }
    }
}