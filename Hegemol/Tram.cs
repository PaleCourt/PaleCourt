using System;
using System.Collections;
using HutongGames.PlayMaker.Actions;
using UnityEngine;

namespace FiveKnights.Hegemol;

public class Tram : MonoBehaviour
{
    private PlayMakerFSM _ctrl;
    private AudioSource _aud;
    private Rigidbody2D _rb;
    private bool _hasStopped;
    private bool _playerPassed;
    private static readonly Vector2 MaxVel = new Vector2(30f, 0f);
    private static readonly Vector3 Left = new Vector3(230f, 171.1914f, 41.88f);
    private static readonly Vector3 Right = new Vector3(530f, 171.1914f, 41.88f);
    private static readonly Vector3 StopAt = new Vector3(437f, 171.1914f, 41.88f);
    
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
        _aud = gameObject.GetComponent<AudioSource>();
        _aud.Play();
        _aud.maxDistance = 100;
        Modding.Logger.Log("Did a thing with tram did light?");
    }

    private IEnumerator Start()
    {
        transform.position = Left;
        _hasStopped = false;
        _playerPassed = false;
        
        yield return new WaitWhile(() => HeroController.instance.transform.position.x < transform.position.x);
        _playerPassed = true;
        _rb.velocity = MaxVel;
    }

    private IEnumerator Accelerate(float from, float to)
    {
        float duration = 2f;
        float time = 0f;
        
        while (time < duration)
        {
            time += Time.deltaTime;
            float xVel = Mathf.Lerp(from, to, time / duration);
            _rb.velocity = new Vector2(xVel, 0f);
            yield return null;
        }

        _rb.velocity = new Vector2(to, 0f);
    }

    private void FixedUpdate()
    {
        if (!_playerPassed) return;
        
        // Go back to orig position
        if (transform.position.x > Right.x)
        {
            transform.position = Left;
            _hasStopped = false;
            _rb.velocity = MaxVel;
        }
        // Stop in the middle of arena for 3 seconds
        else if (!_hasStopped && transform.position.x > StopAt.x - 30f)
        {
            _hasStopped = true;
            StartCoroutine(DelayedEnd());
        }
    }

    private IEnumerator DelayedStart()
    {
        yield return new WaitForSeconds(3f);
        _aud.loop = true;
        _aud.Play();
        yield return Accelerate(0f, MaxVel.x); 
    }
    
    private IEnumerator DelayedEnd()
    {
        _aud.loop = false;
        yield return Accelerate(MaxVel.x, 0f);
        yield return DelayedStart();
    }

    public void FadeAudio()
	{
        IEnumerator Fade()
        {
            float changeCounter = 0f;
            while(changeCounter < 1f)
            {
                _aud.volume -= Time.deltaTime / 1.5f;
                yield return null;
            }
        }
        StartCoroutine(Fade());
    }
}