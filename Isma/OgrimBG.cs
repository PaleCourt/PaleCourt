using System;
using UnityEngine;

namespace FiveKnights.Isma
{
    public class OgrimBG : MonoBehaviour
    {
        private Animator _anim;
        public Transform target;
        private const float Leftest = 107f;
        private const float LeftLeft = 112f;
        private const float Left = 116f;
        private const float Right = 122f;
        private const float Rightest = 132f;

        private void Awake()
        {
            _anim = GetComponent<Animator>();
        }
        private void Update()
        {
            switch (target.position.x)
            {
                case < Leftest:
                    _anim.Play("LookLeftest");
                    break;
                case < LeftLeft:
                    _anim.Play("LookLeftLeft");
                    break;
                case < Left:
                    _anim.Play("LookLeft");
                    break;
                case < Right:
                    _anim.Play("LookMid");
                    break;
                case < Rightest:
                    _anim.Play("LookRight");
                    break;
                default:
                    _anim.Play("LookRightest");
                    break;
            }
        }
    }
}