using System;
using UnityEngine;

namespace FiveKnights.Tiso
{

    public class Shield : MonoBehaviour
    {
        private Rigidbody2D _rb;
        private float _force = 2f;
        private Vector3 _startPos;
        private bool _isready;
        private float _time;
        private const float ShieldSpeed = 40f;
        public float vertDir = 1f;
        public float horizDir;
        public bool isDoneFlag;
        
        private void Start()
        {
            _rb = transform.gameObject.AddComponent<Rigidbody2D>();
            _rb.isKinematic = false;
            _rb.gravityScale = 0f;
            _rb.angularDrag = 0f;
            _rb.velocity = new Vector2(-horizDir * ShieldSpeed, 0f);
            GetComponent<Animator>().speed = 2f;
            _startPos = transform.position;
            _time = 0f;
        }

        private void FixedUpdate()
        {
            Vector2 force = new Vector2(_force * horizDir, 0f);
            transform.position = new Vector3(transform.position.x, _startPos.y + vertDir * Mathf.Sin(_time * 8f) * 2f);
            _rb.AddForce(force, ForceMode2D.Impulse);
            _time += Time.fixedDeltaTime;
        }
    }
}