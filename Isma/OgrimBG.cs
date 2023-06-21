using System.Collections;
using UnityEngine;

namespace FiveKnights.Isma
{
    public class OgrimBG : MonoBehaviour
    {
        public Transform target;

        private Animator _anim;
        private bool lookingRight;

        private void Awake()
        {
            _anim = GetComponent<Animator>();
        }

        private void Start()
		{
            Log("Start following Isma");
            _anim.Play("IdleLeft");
            lookingRight = false;
            StartCoroutine(FollowIsma());
        }

        private IEnumerator FollowIsma()
        {
            while(target != null)
			{
                if(lookingRight && target.position.x < transform.position.x)
                {
                    yield return _anim.PlayBlocking("LookLeft");
                    _anim.Play("IdleLeft");
                    lookingRight = false;
                }
                else if(!lookingRight && target.position.x > transform.position.x)
                {
                    yield return _anim.PlayBlocking("LookRight");
                    _anim.Play("IdleRight");
                    lookingRight = true;
                }
				else
				{
                    yield return null;
				}
            }
        }

        private void Log(object o) => Modding.Logger.Log("[FiveKnights][OgrimBG] " + o);
    }
}