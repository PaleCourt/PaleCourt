namespace FiveKnights
{
    using UnityEngine;
    using System.Collections;

    public class AfterimageFader : MonoBehaviour
    {
        //Edited from Katie's original Fennel stuff

        private SpriteRenderer _sr;

        private void Start ()
        {
            _sr = GetComponent<SpriteRenderer>();
        }

        public void BeginFade()
        {
            StartCoroutine(Fade());
        }

        private IEnumerator Fade()
        {
            float f = 0.5f;
            
            while (f >= 0)
            {
                Color c = _sr.material.GetColor("_Color");
                c.a = f;
                _sr.material.SetColor("_Color", c);
                f -= Time.deltaTime;
                yield return null;
            }
            
            Color b = _sr.material.GetColor("_Color");
            b.a = 0;
            
            _sr.material.SetColor("_Color", b);
        }
    }


}