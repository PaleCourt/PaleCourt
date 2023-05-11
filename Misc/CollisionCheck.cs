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

        private void OnTriggerEnter2D(Collider2D col)
        {
            if (col.gameObject.layer != (int) GlobalEnums.PhysLayers.TERRAIN)
                return;

            Hit = true;
            OnCollide?.Invoke();
            
            if (!Freeze) 
                return;

            Rigidbody2D rb = GetComponent<Rigidbody2D>() ?? transform.parent.GetComponent<Rigidbody2D>();
            
            rb.velocity = Vector2.zero;
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
