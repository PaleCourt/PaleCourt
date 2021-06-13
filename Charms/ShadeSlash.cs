using HutongGames.PlayMaker.Actions;
using UnityEngine;

namespace FiveKnights
{
    public class ShadeSlash : MonoBehaviour
    {
        public GameObject audioPlayer;

        private void OnTriggerEnter2D(Collider2D collider)
        {
            if (collider.gameObject.layer != 11) return;

            GameObject radiance = FiveKnights.preloadedGO["Radiance"];
            PlayMakerFSM radControl = radiance.LocateMyFSM("Control");

            AudioClip slashClip = (AudioClip)radControl.GetAction<AudioPlayerOneShotSingle>("Slash", 1).audioClip.Value;
            audioPlayer.Spawn().GetComponent<AudioSource>().PlayOneShot(slashClip);
        }
    }
}
