using System.Collections;
using UnityEngine;

namespace FiveKnights.Dryya
{
    public class DryyaCorpse : MonoBehaviour
    {
        public AudioClip deathClip;

        private void Start()
        {
            this.PlayAudio(deathClip, 1f);
        }

        private void Log(object message) => Modding.Logger.Log("[Dryya Corpse] " + message);
    }
}