using UnityEngine;
using System.Collections;

namespace FiveKnights
{
    public class MyAudioPlayerOneShotSingle
    {
        public float volume = 1f;
        public GameObject audioPlayer;
        public GameObject spawnPoint;
        public AudioClip audioClip;
        public float pitchMin;
        public float pitchMax;
        public bool loop;
        private AudioSource audio;
        private Coroutine musLoop;

        IEnumerator LoopMusic()
        {
            while (true)
            {
                yield return new WaitForSeconds(audioClip.length);
                AudioClip clip = audioClip;
                audio.pitch = Random.Range(pitchMin, pitchMax);
                audio.volume = volume;
                audio.PlayOneShot(clip);
                yield return null;
            }
        }

        public void UpdateMusic()
        {
            audio.pitch = Random.Range(pitchMin, pitchMax);
            audio.volume = volume;
        }
    
        public void StopMusic()
        {
            if (!loop) return;
            WDController.Instance.StopCoroutine(musLoop);
            audio.Stop();
        }
    
        public void DoPlayRandomClip()
        {
            Vector3 position = spawnPoint.transform.position;
            Vector3 up = Vector3.up;
            GameObject gameObject2 = audioPlayer.Spawn(position, Quaternion.Euler(up));
            audio = gameObject2.GetComponent<AudioSource>();
            audio.clip = null;
            audio.pitch = Random.Range(pitchMin, pitchMax);
            audio.volume = volume;
            audio.PlayOneShot(audioClip);
            if (loop)
            {
                musLoop = WDController.Instance.StartCoroutine(LoopMusic());
            }
        }
    }
}