using SFCore.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using Random = System.Random;

namespace FiveKnights.Misc;

public static class MiscMethods
{
    private static Random _rand = new();
    
    public static GameObject FindGameObject(this Scene scene, string name)
    {
        if (!scene.IsValid())
            return null;
        GameObject[] rootGameObjects = scene.GetRootGameObjects();
        try
        {
            foreach (GameObject gameObject in rootGameObjects)
            {
                if (!(gameObject == null))
                {
                    GameObject objectInChildren = gameObject.FindGameObjectInChildren(name);
                    if (objectInChildren != null)
                        return objectInChildren;
                }
                else
                    break;
            }
        }
        catch (Exception ex)
        {
            Modding.Logger.Log("Exception: " + ex.Message);
        }
        return null;
    }

    public static Func<IEnumerator> ChooseAttack(List<Func<IEnumerator>> attLst, Dictionary<Func<IEnumerator>, int> rep,
        Dictionary<Func<IEnumerator>, int> max)
    {
        List<Func<IEnumerator>> cpyList = new List<Func<IEnumerator>>(attLst);
        Func<IEnumerator> currAtt = cpyList[_rand.Next(0, cpyList.Count)];
        cpyList.Remove(currAtt);
        
        while (currAtt != null && cpyList.Count > 0 && rep[currAtt] >= max[currAtt])
        {
            currAtt = cpyList[_rand.Next(0, cpyList.Count)];
            cpyList.Remove(currAtt);
        }

        if (currAtt == null) return null;

        // Right side of AND is required so we don't skip the last attack
        if (cpyList.Count == 0 && rep[currAtt] >= max[currAtt])
        {
            foreach (var att in attLst.Where(x => x != null)) rep[att] = 0;
            currAtt = attLst[_rand.Next(0, attLst.Count)];
        }

        if (currAtt != null) rep[currAtt]++;

        return currAtt;
    }
}