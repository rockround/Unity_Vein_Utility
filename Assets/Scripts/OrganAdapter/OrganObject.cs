using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OrganObject : MonoBehaviour
{
    public Vein[] inBound;
    public Vein[] outBound;
    internal float temperature, power, coreM, dynamicM;
    internal Vector3[] outMTPs;
    public int inCount, outCount;
    public OrganType organType;
    
    public enum OrganType
    {
        Writer=0, Capacitor, Motor, Structure, Beta, Pump, Vision
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
        outMTPs = new Vector3[outCount];
    }

    // Update is called once per frame
    void Update()
    {

    }
    internal virtual void fire()
    {
        //Second check comes into play if this one hasn't been set yet
        foreach(Vein v in outBound)
        {
            v.forceTrigger();
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
