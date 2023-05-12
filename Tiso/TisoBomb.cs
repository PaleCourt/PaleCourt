using System;
using System.Collections;
using System.Collections.Generic;
using GlobalEnums;
using HutongGames.PlayMaker.Actions;
using UnityEngine;
using Random = UnityEngine.Random;

namespace FiveKnights.Tiso;

public class TisoBomb : MonoBehaviour
{
    private Animator _anim;
    private Rigidbody2D _rb;
    private bool hasExploded;
    public static List<GameObject> AllBombs;

    private void Awake()
    {
        _anim = GetComponent<Animator>();
        _rb = GetComponent<Rigidbody2D>();
        transform.localScale *= 2.5f;
        transform.position += new Vector3(0f, 0f, 0.3f);
    }

    private IEnumerator Start()
    {
        _rb.isKinematic = false;
        _rb.gravityScale = 2f;
        _anim.speed = 0.7f;
        AllBombs.Add(gameObject);
        yield return _anim.PlayToEnd("BombAir");
        if (hasExploded) yield break;
        StartCoroutine(Explode());
    }

    private void Update()
    {
        transform.Rotate(0f, 0f, 25f);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (hasExploded) return;
        if (other.gameObject.layer != (int)PhysLayers.TERRAIN &&
            other.gameObject.layer != (int)PhysLayers.HERO_BOX) return;
        StartCoroutine(Explode());
    }
    
    private IEnumerator Explode()
    {
        hasExploded = true;
        GameObject explosion = Instantiate(FiveKnights.preloadedGO["Explosion"]);
        Destroy(explosion.LocateMyFSM("damages_enemy"));
        explosion.transform.localScale /= 1.65f;
        explosion.transform.position = transform.position;
        explosion.SetActive(true);
        yield return null;
        AllBombs.Remove(gameObject);
        Destroy(gameObject);
    }
}