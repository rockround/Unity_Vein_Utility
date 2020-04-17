using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Branch : MonoBehaviour
{
    public Vein parent;
    public Vein[] veins;

    // Start is called before the first frame update
    void Start()
    {
        parent.onTrigger += triggerOthers;

    }

    void triggerOthers(float lag)
    {
        foreach(Vein v in veins)
        {
            v.phaseLength = lag * 2;
            v.trigger = true;
        }
    }
}
