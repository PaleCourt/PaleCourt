using UnityEngine;
using Random = UnityEngine.Random;

namespace FiveKnights 
{
    internal class Tink : MonoBehaviour
    {
        internal static AudioClip TinkClip { get; set; }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.gameObject.name != "Clash Tink" && !other.gameObject.CompareTag("Nail Attack")) return;
            GameCameras.instance.cameraShakeFSM.SendEvent("EnemyKillShake");

            this.PlayAudio(TinkClip, 1f, 0.15f);
        }
    }
}