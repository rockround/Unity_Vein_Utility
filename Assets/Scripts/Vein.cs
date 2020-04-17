﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Vein : MonoBehaviour
{
    public MeshRenderer mr;
    public MeshFilter mf;
    public bool trigger;
    public float phaseLength = 1;
    public delegate void onTriggered(float lag);
    public event onTriggered onTrigger;
    Material mat;
    public Vector3 start, end, startNorm, endNorm;
    // Start is called before the first frame update
    void Start()
    {
        mat = mr.material;
    }

    // Update is called once per frame
    void Update()
    {

        if (trigger)
        {
            float time = Time.timeSinceLevelLoad;
            float period = phaseLength;
            float angularVelocity = 2 * Mathf.PI / period;
            float frameSize = Mathf.PI / (period + 2);
            mat.SetFloat("windowSize", frameSize);
            mat.SetFloat("speed", angularVelocity);
            mat.SetFloat("offsetTime", time);
            mat.SetFloat("maxTime", time + period / 2);
            StartCoroutine(eventTrigger(period / 2));
            trigger = false;
        }
    }
    public void SetMesh(Mesh m, Vector3 start, Vector3 end, Vector3 startN, Vector3 endN)
    {
        mf.mesh = Instantiate(m);
        this.start = start;
        this.end = end;
        startNorm = startN;
        endNorm = endN;
    }
    public IEnumerator eventTrigger(float lag)
    {
        yield return new WaitForSecondsRealtime(lag/2);
        //Debug.LogError("halt");
        onTrigger?.Invoke(lag);
    }
}
