using System.Collections;
using UnityEngine;

namespace FiveKnights.Dryya
{
    public class DryyaCorpse : MonoBehaviour
    {
        private const float AnimFPS = 1.0f / 12;
        private const int Gravity = 30;
        
        private Animator _anim;
        private BoxCollider2D _collider;
        private Rigidbody2D _rb;
        private SpriteRenderer _sr;
        public int direction;

        private void Awake()
        {
            Log("DryyaCorpse Awake");
            gameObject.layer = 26;
            
            _anim = GetComponent<Animator>();
            _collider = GetComponent<BoxCollider2D>();
            _rb = GetComponent<Rigidbody2D>();
            _sr = GetComponent<SpriteRenderer>();
            
            _sr.material = new Material(Shader.Find("Sprites/Default"));
        }

        private IEnumerator Start()
        {
            yield return new WaitWhile(() => this == null);
            
            Log("DryyaCorpse Start");
            _anim.Play("Defeated");
            _rb.velocity = new Vector2(-direction * 10, 10);
 
            yield return new WaitWhile(() => {
                _rb.velocity += Vector2.down * Gravity;
                return !IsGrounded();
            });

            Log("Defeat Land");
            _anim.Play("Defeat Land");
            _rb.velocity = Vector2.zero;
            
            yield return new WaitForSeconds(1.0f);

            Log("Defeat Leave");
            _anim.Play("Defeat Leave");
            yield return new WaitForSeconds(AnimFPS);
            _rb.velocity = new Vector2(direction * 30, 30);

            yield return new WaitForSeconds(2 * AnimFPS);
            
            Destroy(gameObject);
        }
        
        private const float Extension = 0.01f;
        private const int CollisionMask = 1 << 8;
        private bool IsGrounded()
        {
            float rayLength = _collider.bounds.extents.y + Extension;
            return Physics2D.Raycast(transform.position, Vector2.down, rayLength, CollisionMask);
        }

        private void Log(object message) => Modding.Logger.Log("[Dryya Corpse] " + message);
    }
}