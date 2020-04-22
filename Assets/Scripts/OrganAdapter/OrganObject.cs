using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OrganObject : MonoBehaviour
{
    public Vein[] inBound;
    public Vein[] outBound;
    internal float temperature, power, psions, coreM, dynamicM, startHealth;
    internal Dictionary<Vein, Vector3> outMTPs;
    public int inCount, outCount;
    public OrganType organType;
    internal Material coreMat;
    public Transform coreBlob;
    public Transform dynamicBlob;
    
    public enum OrganType
    {
        Writer=0, Capacitor, Motor, Structure, Beta, Pump, Vision
    }
    void Awake()
    {
        outMTPs = new Dictionary<Vein, Vector3>();
        coreMat = coreBlob.GetComponent<MeshRenderer>().material;
    }
    // Start is called before the first frame update
    public void Start()
    {
        Init();
    }
    public virtual void Init()
    {
        //inBound = new Vein[inCount];
        //outBound = new Vein[outCount];
        //outMTPs = new Vector3[outCount];
    }

    // Update is called once per frame
    internal void Update()
    {
        coreMat.SetColor("_color",new Color(temperature/100, 0, 0, 0));
        float innerRadius = Mathf.Pow(coreM / EnergyManager.coreDensity, 1f / 3);
        //coreBlob.localScale = innerRadius * 2 * Vector3.one;
        //dynamicBlob.localScale = getOuterRadius(innerRadius, dynamicM / EnergyManager.dynamicDensity) * Vector3.one * 2;
    }
    float getOuterRadius(float innerRadius, float volume)
    {
        return Mathf.Pow(3 * volume / (4 * Mathf.PI) + Mathf.Pow(innerRadius, 3), 1f / 3);
    }
    internal virtual void fire()
    {
        //Second check comes into play if this one hasn't been set yet
        foreach(Vein v in outBound)
        {
            Vector3 mtp = outMTPs[v];
            v.mat.SetFloat("temperature",(mtp.y / mtp.x) / 100);
            v.mat.SetFloat("psionLevel", mtp.z / (mtp.x * EnergyManager.psionPerKg));
            v.forceTrigger(mtp.x/8);
        }
        /*print(outBound.Length + " " + outCount);
        if (outCount > 0 && outBound.Length >0)
            for (int i = 0; i < outBound.Length; i++)
            {
                outBound[i].forceTrigger(/*outMTPs[i].x / 10);
            }*/
    }
    
    internal virtual void load()
    {

    }
}
