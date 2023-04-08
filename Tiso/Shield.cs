using System;
using UnityEngine;

namespace FiveKnights.Tiso;

public class Shield : MonoBehaviour
{
    public float dir;
    
    private Rigidbody2D _rb;
    private const float ShieldSpeed = 50f;
    private const float ReturnForce = 1f;

    private void Awake()
    {
        _rb = gameObject.AddComponent<Rigidbody2D>();
    }

    private void Start()
    {
        _rb.velocity = new Vector2(dir * ShieldSpeed, 0f);
    }

    private void FixedUpdate()
    {
        Vector2 force = new Vector2(-dir * ReturnForce, 0f);
        _rb.AddForce(force);
    }
}