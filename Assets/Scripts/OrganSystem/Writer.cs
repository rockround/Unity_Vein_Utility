using System.Collections.Generic;
using System.Collections;
using System.Numerics;
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
        float minM = Math.Min(metabolism, toProcessMTP.X);
        Vector3 deltaMTP = Vector3.Zero;
        float rawPsions = 0;
        float finalTemp;
        if (minM > 0)
        {
            deltaMTP = minM / toProcessMTP.X * toProcessMTP;
            rawPsions = deltaMTP.Z;
            //tKe += deltaMTP.Y;
            finalTemp = (tKe + deltaMTP.Y) / (coreM + dynamicM + deltaMTP.X);

            toProcessMTP -= deltaMTP;
            deltaMTP = new Vector3(deltaMTP.X, 0, 0);

            float usedM = Math.Min(deltaMTP.X, startHealth - (coreM + dynamicM));//if health below normal, get minimum of health needed, available matter, and regeneration
            dynamicM += usedM;
            Vector3 incorporated = deltaMTP * usedM / deltaMTP.X;
            deltaMTP -= incorporated;

            tKe = finalTemp * (coreM + dynamicM);

            //this many phonons end up going back out after absorption
            deltaMTP.Y = deltaMTP.X * finalTemp;
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