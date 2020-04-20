using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OrganObject : MonoBehaviour
{
    internal List<Vein> inBound;
    internal List<Vein> outBound;
    // Start is called before the first frame update
    void Start()
    {
        inBound = new List<Vein>();
        outBound = new List<Vein>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
