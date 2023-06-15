using System;
using FiveKnights.Zemer;
using UnityEngine;

namespace FiveKnights
{
    public class CollisionCheck : MonoBehaviour
    {
        public bool Hit { get; set; }
        public bool Freeze { get; set; }
        public event Action OnCollide;

        private Rigidbody2D _rb;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>() ?? transform.parent.GetComponent<Rigidbody2D>();
        }

        private void Update()
        {
            // Added this to make sure the oncollide is invoked even if ontriggerenter doesn't detect a hit
            if (Hit || _rb.velocity != Vector2.zero) return;
            OnCollide?.Invoke();
            Hit = true;
        }

        private void OnTriggerEnter2D(Collider2D col)
        {
            if (col.gameObject.layer != (int) GlobalEnums.PhysLayers.TERRAIN)
                return;
            
            Hit = true;
            OnCollide?.Invoke();
            
            if (!Freeze) 
                return;

            _rb.velocity = Vector2.zero;
        }

        private void OnTriggerStay2D(Collider2D col)
        {
            if (col.gameObject.layer == 8)
            {
                Hit = true;
            }
        }
    }
}
