using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BetaObject : OrganObject
{
    // Start is called before the first frame update
    public void Start()
    {
        Init();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    internal override void fire()
    {
        base.fire();

    }
}
