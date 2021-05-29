using UnityEngine;
using Logger = Modding.Logger;
using Random = UnityEngine.Random;

namespace FiveKnights 
{
    internal class Tink : MonoBehaviour
    {
        internal static AudioClip TinkClip { get; set; }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.gameObject.name != "Clash Tink" && !other.gameObject.CompareTag("Nail Attack")) return;
            
            // Change the type to get the normal freeze, this is smaller than normal.
            GameManager.instance.FreezeMoment(1);
            
            HeroController.instance.NailParry();
            
            GameCameras.instance.cameraShakeFSM.SendEvent("EnemyKillShake");
            
            var go = new GameObject("Blocker Effect", typeof(AudioSource), typeof(AutoDestroy));

            go.transform.position = other.transform.position;

            var source = go.GetComponent<AudioSource>();

            source.clip = TinkClip;
            source.pitch = Random.Range(0.85f, 1.15f);
            source.volume = GameManager.instance.GetImplicitCinematicVolume();
            source.Play();
        }
    }
}