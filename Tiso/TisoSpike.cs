using System;
using System.Collections;
using System.Collections.Generic;
using FiveKnights.Misc;
using GlobalEnums;
using UnityEngine;
using Random = UnityEngine.Random;

namespace FiveKnights.Tiso
{
    public class TisoSpike : MonoBehaviour
    {
        public static List<GameObject> AllSpikes;
        private const int MaxSpikes = 50;
        private const float LeftX = 51.5f;
        private const float RightX = 71.4f;
        private const float GroundY = 3.2f;
        private Rigidbody2D _rb;
        public bool isDead;
        // this one is for if the player hits the spike after it has landed once already (we want to permanently destroy it now)
        public bool isDead2;
        public bool isDeflected;
        public const int EnemyDamage = 10;
        private const float SpikeVel = 60f;

        private void Awake()
        {
            AllSpikes.RemoveAll(spike => spike == null);
            if (AllSpikes.Count > MaxSpikes)
            {
                for (int i = 0; i < 5; i++)
                {
                    int ind = Random.Range(0, AllSpikes.Count);
                    GameObject toDestroy = AllSpikes[ind];
                    AllSpikes.RemoveAt(ind);
                    Destroy(toDestroy);
                }
            }
            _rb = GetComponent<Rigidbody2D>();
            transform.Find("BlurSpike").gameObject.SetActive(false);
            transform.Find("Spike").gameObject.SetActive(true);
            AllSpikes.Add(gameObject);
        }

        private void Update()
        {
            if (isDead) return;
            float posX = transform.position.x;
            float posY = transform.position.y;
            // Only keep the ground spikes if they were from the player deflecting them in order to avoid
            // horizontal spikes sticking to the gnd
            if ((posX is > LeftX and < RightX) && (posY > GroundY || !isDeflected)) return;
            transform.position = posY > GroundY
                ? new Vector3(_rb.velocity.x > 0 ? RightX : LeftX, posY)
                : new Vector3(posX, GroundY);
            _rb.velocity = Vector2.zero;
            TisoAudio.PlayAudio(this, TisoAudio.Clip.SpikeHitWall);
            transform.Find("BlurSpike").gameObject.SetActive(false);
            transform.Find("Spike").gameObject.SetActive(true);
            isDead = true;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.gameObject.layer != (int)PhysLayers.HERO_ATTACK ||
                other.gameObject.name.Contains("Spike") || 
                other.gameObject.name.Contains("Tiso")) return;

            if (other.CompareTag("Hero Spell"))
            {
                Destroy(gameObject);
                return;
            }
            
            if (isDead && !isDead2)
            {
                if (ShootBasedOnHit(other))
                {
                    StartCoroutine(DestroyAfterTime());
                    isDeflected = true;
                    isDead2 = true;
                }
                return;
            }
            
            if (isDeflected || isDead) return;
            if (ShootBasedOnHit(other))
            {
                isDeflected = true;   
            }
        }

        private bool ShootBasedOnHit(Collider2D other)
        {
            if (other.name.Contains("Up"))
            {
                float rot = Random.Range(70, 110) * Mathf.Deg2Rad;
                transform.SetRotation2D(rot * Mathf.Rad2Deg);
                _rb.velocity = new Vector2(40f * Mathf.Cos(rot), 40f * Mathf.Sin(rot));
            }
            else if (other.name.Contains("Down")) 
            {
                float rot = Random.Range(250, 290) * Mathf.Deg2Rad;
                transform.SetRotation2D(rot * Mathf.Rad2Deg);
                _rb.velocity = new Vector2(40f * Mathf.Cos(rot), 40f * Mathf.Sin(rot));
            }
            else if (other.CompareTag("Nail Attack"))
            {
                // Positive if spike is on left of player
                float dir = -Mathf.Sign(HeroController.instance.transform.localScale.x);
                transform.SetRotation2D(0);
                _rb.velocity = new Vector2(SpikeVel * dir, 0f);
            }
            else
            {
                return false;
            }

            return true;
        }

        private IEnumerator DestroyAfterTime()
        {
            yield return new WaitForSeconds(10f);
            if (gameObject == null) yield break;
            Destroy(gameObject);
        }
    }
}