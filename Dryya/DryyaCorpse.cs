using System.Collections;
using UnityEngine;

namespace FiveKnights.Dryya
{
    public class DryyaCorpse : MonoBehaviour
    {
        private void Start()
        {
            this.PlayAudio(FiveKnights.Clips["DryyaVoiceDeath"], 1f);
        }

        private void Log(object message) => Modding.Logger.Log("[Dryya Corpse] " + message);
    }
}