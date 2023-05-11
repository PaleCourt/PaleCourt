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
        private Rigidbody2D _rb;
        public bool isDead;
        public bool isDeflected;
        public const int EnemyDamage = 10;

        private void Awake()
        {
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
            if (posX is > LeftX and < RightX) return;
            transform.position = new Vector3(_rb.velocity.x > 0 ? RightX : LeftX, transform.position.y);
            _rb.velocity = Vector2.zero;
            transform.Find("BlurSpike").gameObject.SetActive(false);
            transform.Find("Spike").gameObject.SetActive(true);
            isDead = true;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.gameObject.layer != (int) PhysLayers.HERO_ATTACK || other.gameObject.name.Contains("Spike")) return;

            if (isDeflected || isDead) return; // || !other.IsTouchingLayers((int) PhysLayers.HERO_ATTACK)) return;
            
            isDeflected = true;
            _rb.velocity = new Vector2(_rb.velocity.x * -1f, _rb.velocity.y * -1f);
        }
    }
}