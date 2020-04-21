using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using System;

public class Writer : ChargeableOrgan
{
    //between frame resource
    public float crystalPsions;

    //used for internal realtime psion charging control
    bool usePsions;

    public Writer(float startHealth, float powerConsumption, float metabolism, float maxM, float maxCharge) : base(startHealth, powerConsumption, metabolism, maxM, maxCharge)
    {
    }
    public override void bruise(float dynamic, float core, bool chunk = false)// assume charge is stored per unit mass in dynamic. Implement dynamic charge-based vaporization ease
    {

        float initCore = coreM;
        base.bruise(core, dynamic, chunk);
        float deltaPsion = coreM / initCore * crystalPsions;
        crystalPsions -= deltaPsion;
        if (!chunk)
            psionLevel += deltaPsion;
    }
    public override void continuousIn()
    {
        outQ = inQ - usedQ;
        currentPower = usedQ / pC;
        inQ = 0;
    }
    public override void absorb()
    {
        float minM = Math.Min(metabolism, toProcessMTP.x);
        Vector3 deltaMTP = Vector3.zero;
        float rawPsions = 0;
        float finalTemp;
        if (minM > 0)
        {
            deltaMTP = minM / toProcessMTP.x * toProcessMTP;
            rawPsions = deltaMTP.z;
            //tKe += deltaMTP.y;
            finalTemp = (tKe + deltaMTP.y) / (coreM + dynamicM + deltaMTP.x);

            toProcessMTP -= deltaMTP;
            deltaMTP = new Vector3(deltaMTP.x, 0, 0);

            float usedM = Math.Min(deltaMTP.x, startHealth - (coreM + dynamicM));//if health below normal, get minimum of health needed, available matter, and regeneration
            dynamicM += usedM;
            Vector3 incorporated = deltaMTP * usedM / deltaMTP.x;
            deltaMTP -= incorporated;

            tKe = finalTemp * (coreM + dynamicM);

            //this many phonons end up going back out after absorption
            deltaMTP.y = deltaMTP.x * finalTemp;
        }

        if (usePsions)
        {
            crystalPsions += rawPsions;
            rawPsions = 0;
            usePsions = false;
        }

        healthiness = (dynamicM + coreM) / startHealth;

        psionLevel += rawPsions;
        outMTP += deltaMTP;
    }
    public void tapPsion()
    {
        usePsions = true;
    }
    public void tapCharge(float flowDegree = 1)
    {
        float inQUsed = inQ * flowDegree;
        float netQ = inQUsed * healthiness;

        tKe += (inQUsed - netQ);//ohmic heating (using power because the overall result would be described as the sum, or in calculus the integral.
        charge += netQ;
        usedQ = inQUsed;//input all, output some      
    }
}