using System.Collections.Generic;
using System.Collections;
using System.Numerics;
using System;

public class Pump : Organ
{
    //input static parameters
    public float maxDrainRate;


    public Pump(float startHealth, float powerConsumption, float metabolism, float maxM, float maxDrainRate) : base(startHealth, powerConsumption, metabolism, maxM)
    {
        this.maxDrainRate = maxDrainRate;
    }

    public override void absorb()
    {
        float minM = Math.Min(metabolism, toProcessMTP.X);
        Vector3 deltaMTP = Vector3.Zero;
        float finalTemp = getTemperature();
        if (minM > 0)
        {

            deltaMTP = minM / toProcessMTP.X * toProcessMTP;
            psionLevel += deltaMTP.Z;
            //tKe += deltaMTP.Y;
            finalTemp = (tKe + deltaMTP.Y) / (coreM + dynamicM + deltaMTP.X);

            toProcessMTP -= deltaMTP;
            deltaMTP = new Vector3(deltaMTP.X, 0, 0);
            //now look to get more health maybe
            float usedM = Math.Min(Math.Min(deltaMTP.X, startHealth - (coreM + dynamicM)), 0);//if health below normal, get minimum of health needed, available matter, and regeneration
            dynamicM += usedM;
            Vector3 incorporated = deltaMTP * usedM / deltaMTP.X;
            deltaMTP -= incorporated;

            tKe = finalTemp * (coreM + dynamicM);

            //this many phonons end up going back out after absorption
            deltaMTP.Y = deltaMTP.X * finalTemp;

        }
        healthiness = (dynamicM + coreM) / startHealth;


        float toCvt = drainAmt * currentPower;
        parent.stomachM -= toCvt;
        parent.dynamicM += toCvt;

        //psion pump unique, splits how much toCvt is sent to output, how much is leaked into organ psion level
        float realP = toCvt * EnergyManager.psionPerKg;
        outMTP.Z += realP * healthiness;
        psionLevel += realP * (1 - healthiness);

        outMTP += deltaMTP;
    }

    public float drainAmt
    {
        get
        {
            return Math.Min(parent.stomachM, maxDrainRate);//get minimum of what is in there and what can be taken
        }

    }
}
