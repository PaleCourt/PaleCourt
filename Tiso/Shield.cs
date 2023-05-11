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
        private const float LeftX = 51.2f;
        private const float RightX = 71.7f;
        private const float ShieldSpeed = 47f;
        public float vertDir = 1f;
        public float yOffset = 0f;
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
            if (transform.position.x is < LeftX or > RightX)
            {
                _rb.velocity *= new Vector2(-1f, 0f);
                isDoneFlag = true;
            }

            // Vector2.Lerp(transform.position, _startPos, Time.deltaTime).y
            float yNew = _startPos.y + vertDir * Mathf.Sin(_time * 8f) * 2f + yOffset;
            if (isDoneFlag)
            {
                _force = 2.5f;
            }
            Vector2 force = new Vector2(_force * horizDir, 0f);
            transform.position = new Vector3(transform.position.x, yNew);
            _rb.AddForce(force, ForceMode2D.Impulse);
            if (_rb.velocity.x * horizDir > 0)
            {
                isDoneFlag = true;
            }
            _time += Time.fixedDeltaTime;
        }
    }
}