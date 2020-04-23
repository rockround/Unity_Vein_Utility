using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StomachObject : OrganObject
{
    internal override void AwakeCode()
    {
        outMTPs = new Dictionary<Vein, Vector3>();
    }
    internal override void updateCode()
    {
        if (dynamicM > 0)
        {
            float innerRadius = Mathf.Pow(dynamicM / EnergyManager.dynamicDensity, 1f / 3);
            dynamicBlob.localScale = innerRadius * 2 * Vector3.one;
        }
        else
        {
            dynamicBlob.localScale = new Vector3(.001f, .001f, .001f);
            //print(dynamicM);
        }
    }
    internal override void fire()
    {
        //Second check comes into play if this one hasn't been set yet
        foreach (Vein v in outBound)
        {
            Vector3 mtp = outMTPs[v];
            v.mat.SetFloat("temperature", (mtp.y / mtp.x) / 100);
            v.mat.SetFloat("psionLevel", mtp.z / (mtp.x * EnergyManager.psionPerKg));
            v.forceTrigger(mtp.x / 7);
        }
        /*print(outBound.Length + " " + outCount);
        if (outCount > 0 && outBound.Length >0)
            for (int i = 0; i < outBound.Length; i++)
            {
                outBound[i].forceTrigger(/*outMTPs[i].x / 10);
            }*/
    }
}
