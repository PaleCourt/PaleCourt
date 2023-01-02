using System;
using UnityEngine;
using System.Collections;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace FiveKnights.Isma
{
    // The purpose of this file is to make the whip attack Isma does flash white when it is hit by the nail
    public class WhipFlash : MonoBehaviour
    {
        private float amount;
        private float t;
        private float timeUp;
        private float stayTime;
        private float timeDown;
        private float flashTimer;
        private bool flashing;
        private FlashState flashingState;
        private float amountCurrent;
        private static readonly int FlashAmount = Shader.PropertyToID("_FlashAmount");
        private SpriteRenderer _currSR;
        private SpriteRenderer[] _whips;
        private enum FlashState
        {
            Increase,
            Stay,
            Decrease,
            Stop
        };

        private void Start()
        {
            _whips = transform.GetComponentsInChildren<SpriteRenderer>(true);
            foreach (var i in _whips.Where(x => x.GetComponent<PolygonCollider2D>()))
            {
                i.gameObject.AddComponent<HitCheck>().OnHit += ResetValues;
            }
        }

        private void ResetValues()
        {
            _currSR = GetWhipActive();
            amount = 0.85f;
            timeUp = 0.01f;
            stayTime = 0.01f;
            timeDown = 0.35f;
            _currSR.material.SetFloat(FlashAmount, 0f);
            flashingState = FlashState.Increase;
            flashTimer = 0.0f;
        }
        
        private SpriteRenderer GetWhipActive()
        {
            return _whips.FirstOrDefault(x => x.gameObject.activeSelf);
        }

        private void Update()
        {
            if (_currSR == null) return;
            _currSR.material.SetFloat(FlashAmount, 0f);
            _currSR = GetWhipActive();
            if (_currSR == null) return;
            _currSR.material = FiveKnights.Materials["flash"];
            
            if (flashingState == FlashState.Increase)
            {
                if (flashTimer < timeUp)
                {
                    flashTimer += Time.deltaTime;
                    t = flashTimer / timeUp;
                    amountCurrent = Mathf.Lerp(0.0f, amount, t);
                    _currSR.material.SetFloat(FlashAmount, amountCurrent);
                }
                else
                {
                    _currSR.material.SetFloat(FlashAmount, amount);
                    flashTimer = 0.0f;
                    flashing = false;
                    flashingState = FlashState.Stay;
                }
            }
            if (flashingState == FlashState.Stay)
            {
                if (flashTimer < stayTime)
                {
                    flashTimer += Time.deltaTime;
                }
                else
                {
                    flashTimer = 0.0f;
                    flashingState = FlashState.Decrease;
                }
            }
            if (flashingState == FlashState.Decrease)
            {
                if (flashTimer < timeDown)
                {
                    flashTimer += Time.deltaTime;
                    t = flashTimer / timeDown;
                    amountCurrent = Mathf.Lerp(amount, 0.0f, t);
                    _currSR.material.SetFloat(FlashAmount, amountCurrent);
                }
                else
                {
                    _currSR.material.SetFloat(FlashAmount, 0f);
                    flashTimer = 0.0f;
                    flashingState = FlashState.Stop;
                }
            }
        }
        
        // This class checks for hits on each whip object
        internal class HitCheck : MonoBehaviour
        {
            public event Action OnHit;
        
            private void OnTriggerEnter2D(Collider2D col) 
            {
                if (col.gameObject.layer == 17) OnHit?.Invoke();
            }
        }
    }
}
