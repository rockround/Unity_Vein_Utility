using System.Collections.Generic;
using System.Collections;
using System.Numerics;
using System;

public class Beta : Organ
{
    //input static params
    public float betaRate;

    public Beta(float startHealth, float powerConsumption, float metabolism, float maxM, float betaRate) : base(startHealth, powerConsumption, metabolism, maxM)
    {
        this.betaRate = betaRate;
    }
    public override void continuousIn()
    {
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
            finalTemp = (tKe + deltaMTP.Y) / (coreM + dynamicM + deltaMTP.X);

            //tKe += deltaMTP.Y;
            toProcessMTP -= deltaMTP;
            deltaMTP = new Vector3(deltaMTP.X, 0, 0);
        }
        //TODO: Make this based on separate beta level resource
        bruise(0, startHealth * contaminationRate);//damaged with contamination (at end because this is covered up by inM in flowing

        if (minM > 0)
        {
            float usedM = Math.Min(deltaMTP.X, startHealth - (coreM + dynamicM));//if health below normal, get minimum of health needed, available matter, and regeneration
            dynamicM += usedM;
            Vector3 incorporated = deltaMTP * usedM / deltaMTP.X;
            deltaMTP -= incorporated;

            tKe = finalTemp * (coreM + dynamicM);

            //this many phonons end up going back out after absorption
            deltaMTP.Y = deltaMTP.X * finalTemp;
        }
        healthiness = (dynamicM + coreM) / startHealth;

        outMTP += deltaMTP;
    }
    public override void continuousOut()
    {
        float realFlow = betaRate;
        parent.charge += -realFlow * (1 - healthiness);//leak charge into body
        parent.c.inQ += realFlow * healthiness;
    }
    float contaminationRate
    {
        get
        {
            return (float)Math.Exp(-16 / betaRate) / 4;
        }
    }

}