using System;
using UnityEngine;

namespace FiveKnights
{
    public class CollisionCheck : MonoBehaviour
    {
        public bool isHit { get; set; }
        public bool shouldStopForMe { get; set; }
        public string collider { get; set; }
        public Action action { get; set; }
        public Collider2D collision { get; set; }

        private void OnTriggerEnter2D(Collider2D col)
        {
            if (col.gameObject.layer == 8)
            {
                collision = col;
                isHit = true;
                collider = col.name;
                if (shouldStopForMe)
                {
                    Rigidbody2D rb = (GetComponent<Rigidbody2D>() != null)
                        ? GetComponent<Rigidbody2D>()
                        : transform.parent.GetComponent<Rigidbody2D>();
                    rb.velocity = Vector2.zero;
                    
                    if (action == null) return;
                    action.Invoke();
                }
            }
        }
        
        private void OnTriggerStay2D(Collider2D col)
        {
            if (col.gameObject.layer == 8)
            {
                isHit = true;
                collider = col.name;
            }
        }
    }
}
