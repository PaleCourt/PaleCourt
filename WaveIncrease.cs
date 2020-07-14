using UnityEngine;

namespace FiveKnights
{
    public class WaveIncrease : MonoBehaviour
    {
        void Update ()
        {
            transform.localScale += new Vector3(0.02f, 0.02f, 0f);
            if(transform.localScale.x > 5f)
            {
                Modding.Logger.Log("KILL");
                Destroy(gameObject);
            }

        }
    }
}