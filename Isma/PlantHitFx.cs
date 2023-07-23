using System;
using System.Collections.Generic;
using UnityEngine;
using HutongGames.PlayMaker.Actions;

namespace FiveKnights.Isma
{
    // Recreation of the Thorn Collider - Slash Reaction FSM in Greenpath/Queen's Gardens
    public class PlantHitFx : MonoBehaviour
    {
        private MusicPlayer _ap;
        public AudioClip hitSound;
        private GameObject hitEffectPrefab;
        private GameObject thornCutGrassPrefab;
        private GameObject spatterPrefab;

        private void Start()
        {
            PlayMakerFSM spellControl = HeroController.instance.gameObject.LocateMyFSM("Spell Control");
            GameObject fireballParent = spellControl.GetAction<SpawnObjectFromGlobalPool>("Fireball 2", 3).gameObject.Value;
            PlayMakerFSM fireballCast = fireballParent.LocateMyFSM("Fireball Cast");
            GameObject actor = fireballCast.GetAction<AudioPlayerOneShotSingle>("Cast Right", 3).audioPlayer.Value;

            _ap = new MusicPlayer
            {
                Volume = 1f,
                Player = actor,
                MaxPitch = 1f,
                MinPitch = 1f,
                Spawn = gameObject
            };

            PlayMakerFSM thornFSM = FiveKnights.preloadedGO["Thorn Collider"].LocateMyFSM("Slash Reaction");
            if(hitSound == null) hitSound = thornFSM.GetAction<AudioPlay>("Get Direction", 3).oneShotClip.Value as AudioClip;
            hitEffectPrefab = thornFSM.GetAction<CreateObject>("Hit Right", 3).gameObject.Value;
            thornCutGrassPrefab = thornFSM.GetAction<CreateObject>("Hit Right", 7).gameObject.Value;

            foreach(var pool in ObjectPool.instance.startupPools)
            {
                if(pool.prefab.name == "Spatter White R")
                {
                    spatterPrefab = pool.prefab;
                }
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if(other.gameObject.tag == "Nail Attack") DoStuff(other.gameObject);
        }

        public void DoStuff(GameObject other)
        {
            _ap.Clip = hitSound;
            _ap.DoPlayRandomClip();

            float dir = other.LocateMyFSM("damages_enemy").FsmVariables.FindFsmFloat("direction").Value;
            Vector2 effectOrigin = other.transform.position;

            if(dir < 45f)
            {
                effectOrigin += 1.62f * Vector2.right;
                FlingObjects(spatterPrefab, effectOrigin, 110f, 150f);
                GameObject hitEffect = Instantiate(hitEffectPrefab, effectOrigin, Quaternion.Euler(0f, 0f, UnityEngine.Random.Range(340f, 380f)));
                hitEffect.transform.SetScaleX(0.85f);
                hitEffect.transform.SetScaleY(0.85f);
                hitEffect.SetActive(true);
                GameObject thornCutGrass = Instantiate(thornCutGrassPrefab, effectOrigin, Quaternion.Euler(0f, 270f, 180f));
                thornCutGrass.SetActive(true);
            }
            else if(dir < 135f)
            {
                effectOrigin += 1.1f * Vector2.up;
                FlingObjects(spatterPrefab, effectOrigin, 225f, 315f);
                GameObject hitEffect = Instantiate(hitEffectPrefab, effectOrigin, Quaternion.Euler(0f, 0f, UnityEngine.Random.Range(610f, 650f)));
                hitEffect.transform.SetScaleX(-0.85f);
                hitEffect.transform.SetScaleY(0.85f);
                hitEffect.SetActive(true);
                GameObject thornCutGrass = Instantiate(thornCutGrassPrefab, effectOrigin, Quaternion.Euler(90f, 90f, 0f));
                thornCutGrass.SetActive(true);
            }
            else if(dir < 225f)
            {
                effectOrigin += 1.62f * Vector2.left;
                FlingObjects(spatterPrefab, effectOrigin, 30f, 70f);
                GameObject hitEffect = Instantiate(hitEffectPrefab, effectOrigin, Quaternion.Euler(0f, 0f, UnityEngine.Random.Range(340f, 380f)));
                hitEffect.transform.SetScaleX(-0.85f);
                hitEffect.transform.SetScaleY(0.85f);
                hitEffect.SetActive(true);
                GameObject thornCutGrass = Instantiate(thornCutGrassPrefab, effectOrigin, Quaternion.Euler(0f, 90f, 0f));
                thornCutGrass.SetActive(true);
            }
            else
            {
                effectOrigin += 1.26f * Vector2.down;
                FlingObjects(spatterPrefab, effectOrigin, 45f, 115f);
                GameObject hitEffect = Instantiate(hitEffectPrefab, effectOrigin, Quaternion.Euler(0f, 0f, UnityEngine.Random.Range(430f, 470f)));
                hitEffect.transform.SetScaleX(-0.85f);
                hitEffect.transform.SetScaleY(0.85f);
                hitEffect.SetActive(true);
                GameObject thornCutGrass = Instantiate(thornCutGrassPrefab, effectOrigin, Quaternion.Euler(270f, 90f, 0f));
                thornCutGrass.SetActive(true);
            }
        }

        private void FlingObjects(GameObject go, Vector2 pos, float angleMin, float angleMax)
        {
            if(go != null)
            {
                int num = UnityEngine.Random.Range(2, 4);
                for(int i = 1; i <= num; i++)
                {
                    GameObject gameObject = go.Spawn(pos, Quaternion.identity);
                    gameObject.transform.position += new Vector3(0f, 0.5f);
                    float speed = UnityEngine.Random.Range(14f, 18f);
                    float dir = UnityEngine.Random.Range(angleMin, angleMax);
                    float velx = speed * Mathf.Cos(dir * Mathf.Deg2Rad);
                    float vely = speed * Mathf.Sin(dir * Mathf.Deg2Rad);
                    gameObject.GetComponent<Rigidbody2D>().velocity = new Vector2(velx, vely);
                }
            }
        }
    }
}
