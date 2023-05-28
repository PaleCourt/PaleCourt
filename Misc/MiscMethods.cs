using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace FiveKnights.Misc;

public static class MiscMethods
{
    private static Random _rand = new();

    public static Func<IEnumerator> ChooseAttack(List<Func<IEnumerator>> attLst, Dictionary<Func<IEnumerator>, int> rep,
        Dictionary<Func<IEnumerator>, int> max)
    {
        List<Func<IEnumerator>> cpyList = new List<Func<IEnumerator>>(attLst);
        Func<IEnumerator> currAtt = cpyList[_rand.Next(0, cpyList.Count)];

        while (currAtt != null && cpyList.Count > 0 && rep[currAtt] >= max[currAtt])
        {
            currAtt = cpyList[_rand.Next(0, cpyList.Count)];
            cpyList.Remove(currAtt);
        }

        // Right side of AND is required so we don't skip the last attack
        if (cpyList.Count == 0 && (currAtt == null || rep[currAtt] >= max[currAtt]))
        {
            foreach (var att in attLst.Where(x => x != null)) rep[att] = 0;
            currAtt = attLst[_rand.Next(0, attLst.Count)];
        }

        if (currAtt != null) rep[currAtt]++;

        return currAtt;
    }
}