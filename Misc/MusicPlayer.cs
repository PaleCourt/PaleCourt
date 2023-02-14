using UnityEngine;
using System.Collections;
using FiveKnights.BossManagement;

namespace FiveKnights
{
    public class MusicPlayer
    {
        public float Volume = 1f;
        public GameObject Player;
        public GameObject Spawn;
        public AudioClip Clip;
        public float MinPitch;
        public float MaxPitch;
        public bool Loop;

        private AudioSource audio;

        public void UpdateMusic()
        {
            audio.pitch = Random.Range(MinPitch, MaxPitch);
            audio.volume = Volume;
        }

        public void StopMusic()
        {
            audio.Stop();
        }

        private IEnumerator FixSpawn()
        {
            yield return new WaitWhile(() => HeroController.instance == null);
            Spawn = HeroController.instance.gameObject;
            DoPlayRandomClip();
        }
        
        public void DoPlayRandomClip()
        {
            if (Spawn == null)
            {
                GameManager.instance.StartCoroutine(FixSpawn());
                return;
            }
            
            GameObject audioPlayer = Player.Spawn
            (
                Spawn.transform.position,
                Quaternion.Euler(Vector3.up)
            );

            if(Loop)
			{
                audio = audioPlayer.GetComponent<AudioSource>();
                audio.clip = Clip;
                audio.pitch = Random.Range(MinPitch, MaxPitch);
                audio.volume = Volume;
                audio.loop = Loop;
                audio.Play();
            }
			else
			{
                audio = audioPlayer.GetComponent<AudioSource>();
                audio.clip = null;
                audio.pitch = Random.Range(MinPitch, MaxPitch);
                audio.volume = Volume;
                audio.PlayOneShot(Clip);
            }
        }
    }
}