using UnityEngine;
using Random = UnityEngine.Random;

namespace FiveKnights 
{
    internal class ParryTink : MonoBehaviour
    {
        internal static AudioClip TinkClip { get; set; }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.gameObject.name != "Clash Tink" && !other.gameObject.CompareTag("Nail Attack")) return;
            // Change the type to get the normal freeze, this is smaller than normal.
            GameManager.instance.StartCoroutine(GameManager.instance.FreezeMoment(0.01f, 0.15f, 0.1f, 0.0f));
            HeroController.instance.NailParry();
            GameCameras.instance.cameraShakeFSM.SendEvent("EnemyKillShake");

            this.PlayAudio(TinkClip, 1f, 0.15f);
        }
    }
}