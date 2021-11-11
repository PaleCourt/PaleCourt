using FiveKnights.Misc;
using SFCore.Utils;
using System.Collections;
using TMPro;
using UnityEngine;

namespace FiveKnights
{
    public static class AreaTitleCtrl
    {
        public static void ShowBossTitle(
            MonoBehaviour owner,
            GameObject areaTitleObject,
            float hideInSeconds,
            string largeMain = "",
            string largeSuper = "",
            string largeSub = "",
            string smallMain = "",
            string smallSuper = "",
            string smallSub = "")
        {
            areaTitleObject.LocateMyFSM("Area Title Control").enabled = false;
            areaTitleObject.SetActive(true);
            areaTitleObject.transform.Find("Title Large").gameObject.SetActive(false);
            areaTitleObject.transform.Find("Title Small").gameObject.SetActive(true);
            foreach (FadeGroup componentsInChild in areaTitleObject.GetComponentsInChildren<FadeGroup>())
                componentsInChild.FadeUp();
            // areaTitleObject.FindGameObjectInChildren("Title Small Main").transform.Translate(new Vector3(4f, 0.0f, 0.0f));
            // areaTitleObject.FindGameObjectInChildren("Title Small Sub").transform.Translate(new Vector3(4f, 0.0f, 0.0f));
            // areaTitleObject.FindGameObjectInChildren("Title Small Super").transform.Translate(new Vector3(4f, 0.0f, 0.0f));
            
            areaTitleObject.FindGameObjectInChildren("Title Small Main").transform.position = new Vector3(-9.7f, -6.3f, -0.1f);
            areaTitleObject.FindGameObjectInChildren("Title Small Sub").transform.position = new Vector3(-9.7f, -7.2f, -0.1f);
            areaTitleObject.FindGameObjectInChildren("Title Small Super").transform.position = new Vector3(-9.7f, -5.2f, -0.1f);
            
            areaTitleObject.FindGameObjectInChildren("Title Small Main").GetComponent<TextMeshPro>().text = smallMain;
            areaTitleObject.FindGameObjectInChildren("Title Small Sub").GetComponent<TextMeshPro>().text = smallSub;
            areaTitleObject.FindGameObjectInChildren("Title Small Super").GetComponent<TextMeshPro>().text = smallSuper;
            areaTitleObject.FindGameObjectInChildren("Title Large Main").GetComponent<TextMeshPro>().text = largeMain;
            areaTitleObject.FindGameObjectInChildren("Title Large Sub").GetComponent<TextMeshPro>().text = largeSub;
            areaTitleObject.FindGameObjectInChildren("Title Large Super").GetComponent<TextMeshPro>().text = largeSuper;
            if (hideInSeconds <= 0.0)
                return;
            owner.StartCoroutine(HideBossTitleAfter(areaTitleObject, hideInSeconds + 3f));
        }

        private static IEnumerator HideBossTitleAfter(
            GameObject areaTitleObject,
            float time)
        {
            yield return new WaitForSeconds(time);
            HideBossTitle(areaTitleObject);
            yield return new WaitForSeconds(3f);
            areaTitleObject.SetActive(false);
            areaTitleObject.LocateMyFSM("Area Title Control").enabled = true;
            areaTitleObject.FindGameObjectInChildren("Title Small Main").transform.Translate(new Vector3(-4f, 0.0f, 0.0f));
            areaTitleObject.FindGameObjectInChildren("Title Small Sub").transform.Translate(new Vector3(-4f, 0.0f, 0.0f));
            areaTitleObject.FindGameObjectInChildren("Title Small Super").transform.Translate(new Vector3(-4f, 0.0f, 0.0f));
        }

        private static void HideBossTitle(GameObject areaTitleObject)
        {
            foreach (FadeGroup componentsInChild in areaTitleObject.GetComponentsInChildren<FadeGroup>())
                componentsInChild.FadeDown();
        }
    }
}