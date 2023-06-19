using System;
using FiveKnights.BossManagement;
using UnityEngine;

namespace FiveKnights.Zemer;

public class ExtraNailBndCheck : MonoBehaviour
{
    // I am sorry for this but I couldn't think of another way to fix this without changing Isma stuff and I didnt want
    // to break that accidentally

    private Rigidbody2D _rb;
    private readonly float _nailMaxHeightStop = (OWArenaFinder.IsInOverWorld) ? 118.5f :
        (CustomWP.boss == CustomWP.Boss.All || CustomWP.boss == CustomWP.Boss.Ogrim) ? 18.5f : 39f;
    
    private readonly float _nailMaxGroundStop = (OWArenaFinder.IsInOverWorld) ? 106f :
        (CustomWP.boss == CustomWP.Boss.All || CustomWP.boss == CustomWP.Boss.Ogrim) ? 3f : 27.3f;
    
    private readonly float _nailMaxLeftStop = (OWArenaFinder.IsInOverWorld) ? 241.9f :
        (CustomWP.boss == CustomWP.Boss.All || CustomWP.boss == CustomWP.Boss.Ogrim) ? 62f : 13f;
    
    private readonly float _nailMaxRightStop = (OWArenaFinder.IsInOverWorld) ? 271.2f :
        (CustomWP.boss == CustomWP.Boss.All || CustomWP.boss == CustomWP.Boss.Ogrim) ? 89f : 43f;
    
    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
    }

    private void FixedUpdate()
    {
        Vector2 nailPos = transform.position;
        
        if (nailPos.x > _nailMaxRightStop && _rb.velocity.x > 0)
        {
            _rb.velocity = Vector2.zero;
            transform.position = new Vector3(_nailMaxRightStop, nailPos.y);
        }
        else if (nailPos.x < _nailMaxLeftStop && _rb.velocity.x < 0)
        {
            _rb.velocity = Vector2.zero;
            transform.position = new Vector3(_nailMaxLeftStop, nailPos.y);
        }
        else if (nailPos.y > _nailMaxHeightStop && _rb.velocity.y > 0)
        {
            _rb.velocity = Vector2.zero;
            transform.position = new Vector3(nailPos.x, _nailMaxHeightStop);
        }
        else if (nailPos.y < _nailMaxGroundStop && _rb.velocity.y < 0)
        {
            Modding.Logger.Log("Stopped at gnd zem");
            _rb.velocity = Vector2.zero;
            transform.position = new Vector3(nailPos.x, _nailMaxGroundStop);
        }
    }
}