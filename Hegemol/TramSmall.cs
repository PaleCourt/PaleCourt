using System;
using System.Collections;
using HutongGames.PlayMaker.Actions;
using UnityEngine;

namespace FiveKnights.Hegemol;

public class TramSmall : MonoBehaviour
{
    private PlayMakerFSM _ctrl;
    
    private Rigidbody2D _rb;
    private static readonly Vector2 TramVel = new Vector2(-50f, 0f);
    private static readonly Vector3 StartPos = new Vector3(520f, 188f, 102.6f);
    private static readonly Vector3 EndPos = new Vector3(230f, 188f, 102.6f);

    private void Awake()
    {
        _ctrl = gameObject.LocateMyFSM("Tram Control");
        _ctrl.enabled = false;
        var light =
            _ctrl.GetAction<SendEventByName>("Door Open", 4).eventTarget;
        _ctrl.Fsm.Event(light, "UP");
        _rb = gameObject.AddComponent<Rigidbody2D>();
        _rb.isKinematic = true;
        _rb.gravityScale = 0f;
        gameObject.GetComponent<AudioSource>().Play();
        gameObject.GetComponent<AudioSource>().maxDistance = 100;
    }

    private void Start()
    {
        transform.position = StartPos;
        _rb.velocity = TramVel;
    }

    private void FixedUpdate()
    {
        if (transform.position.x < EndPos.x)
        {
            transform.position = StartPos;
        }
    }
}