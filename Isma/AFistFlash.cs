using System;
using UnityEngine;
using System.Collections;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace FiveKnights.Isma
{
    // The purpose of this file is to make the whip attack Isma does flash white when it is hit by the nail
    public class AFistFlash : MonoBehaviour
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
        private SpriteRenderer _sr;
        private enum FlashState
        {
            Increase,
            Stay,
            Decrease,
            Stop
        };

        private void Start()
        {
            _sr = GetComponent<SpriteRenderer>();
            _sr.material = FiveKnights.Materials["flash"];
            foreach (var i in transform.GetComponentsInChildren<PolygonCollider2D>(true))
            {
                i.gameObject.AddComponent<HitCheck>().OnHit += ResetValues;
            }
        }

        private void ResetValues()
        {
            amount = 0.85f;
            timeUp = 0.01f;
            stayTime = 0.01f;
            timeDown = 0.15f;
            _sr.material.SetFloat(FlashAmount, 0f);
            flashingState = FlashState.Increase;
            flashTimer = 0.0f;
        }

        private void Update()
        {
            if (flashingState == FlashState.Increase)
            {
                if (flashTimer < timeUp)
                {
                    flashTimer += Time.deltaTime;
                    t = flashTimer / timeUp;
                    amountCurrent = Mathf.Lerp(0.0f, amount, t);
                    _sr.material.SetFloat(FlashAmount, amountCurrent);
                }
                else
                {
                    _sr.material.SetFloat(FlashAmount, amount);
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
                    _sr.material.SetFloat(FlashAmount, amountCurrent);
                }
                else
                {
                    _sr.material.SetFloat(FlashAmount, 0f);
                    flashTimer = 0.0f;
                    flashingState = FlashState.Stop;
                }
            }
        }
        
        // This class checks for hits on each whip object
        class HitCheck : MonoBehaviour
        {
            public event Action OnHit;

            private void OnTriggerEnter2D(Collider2D col)
            {
                if (col.gameObject.layer == 17)
                {
                    OnHit?.Invoke();
                }
            }
        }
    }
}
