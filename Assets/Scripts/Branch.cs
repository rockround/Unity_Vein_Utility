using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Branch : MonoBehaviour
{
    public Vein parent;
    public List<Vein> children;
    // Start is called before the first frame update
    void Start()
    {
        if(children == null || children.Count ==0)
        children = new List<Vein>();
        parent.onTrigger += triggerOthers;

    }

    void triggerOthers(float lag)
    {
        foreach(Vein v in children)
        {
            v.phaseLength = lag * 2;
            v.forceTrigger();
        }
    }
}
