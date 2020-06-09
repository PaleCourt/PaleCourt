namespace FiveKnights
{
    using UnityEngine;
    using System.Collections;

    public class AfterimageFader : MonoBehaviour
    {
        //Edited from Katie's original Fennel stuff

        private SpriteRenderer sr;
        void Start ()
        {
            sr = GetComponent<SpriteRenderer>();
        }

        public void BeginFade()
        {
            StartCoroutine(Fade());
        }

        IEnumerator Fade()
        {
            float f = 0.5f;
            while (f >= 0)
            {
                Color c = sr.material.GetColor("_Color");
                c.a = f;
                sr.material.SetColor("_Color", c);
                f -= Time.deltaTime;
                yield return null;
            }
            Color b = sr.material.GetColor("_Color");
            b.a = 0;
            sr.material.SetColor("_Color", b);
        }
    }


}