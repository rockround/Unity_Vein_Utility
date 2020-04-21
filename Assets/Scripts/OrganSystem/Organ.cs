using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using System;

public class Organ
{
    public const int WriterI = 0;
    public const int CapacitorI = 1;
    public const int MotorI = 2;
    public const int StructureI = 3;
    public const int BetaI = 4;
    public const int PumpI = 5;
    public const int VisionI = 6;
    public const int lastChargeableOrgan = 2;
    //input static parameters
    public float startHealth, pC, metabolism, maxM;

    //between frame static
    public float coreM, dynamicM, tKe, psionLevel;

    //Calculated statistic
    public float healthiness, currentPower;

    //dynamic variables in between frames
    public float inQ, outQ, usedQ;
    public Vector3 inMTP, outMTP, toProcessMTP;

    public Structure parent;

    public Organ(float startHealth, float powerConsumption, float metabolism, float maxM)
    {
        coreM = startHealth;
        this.startHealth = startHealth;
        pC = powerConsumption;
        this.metabolism = metabolism;
        this.maxM = maxM;
        tKe = startHealth * EnergyManager.roomTemp;
    }
    public virtual void flowIn()
    {
        toProcessMTP = inMTP;
        outMTP = inMTP = Vector3.zero;
    }
    public virtual void flowOut()
    {
        //how much actually is released
        Vector3 netMTP = outMTP * healthiness;

        //float phononsReleased = 0;
        float psionsReleased = 0;

        /*if (coreM + dynamicM > 0)
        {
            phononsReleased = getTemperature() * netMTP.x;
        }*/
        if (coreM > 0)
        {
            psionsReleased = psionLevel / coreM * netMTP.x;
        }

        //Handled in matter absorption step
        //tKe -= phononsReleased;
        psionLevel -= psionsReleased;

        Vector3 remainingToFlow = toProcessMTP * healthiness;

        outMTP += new Vector3(0, 0/*phononsReleased*/, psionsReleased);

        parent.inMTP += netMTP + remainingToFlow;

        parent.stomachM += outMTP.x - netMTP.x + toProcessMTP.x - remainingToFlow.x;     //health inefficiencies leak into stomach
        parent.psionLevel += outMTP.z - netMTP.z + toProcessMTP.z - remainingToFlow.z;
        parent.addPhonons(outMTP.y - netMTP.y + toProcessMTP.y - remainingToFlow.y);
    }
    public virtual void absorb()
    {
        float minM = Math.Min(metabolism, toProcessMTP.x);
        Vector3 deltaMTP = Vector3.zero;
        if (minM > 0)
        {

            deltaMTP = minM / toProcessMTP.x * toProcessMTP;
            //Psions distribute by mass evenly (Like charge)
            psionLevel += deltaMTP.z;

            //Calculate total diffusion of blood and organ regardless of how much blood is used
            float finalTemp = (tKe + deltaMTP.y) / (coreM + dynamicM + deltaMTP.x);

            toProcessMTP -= deltaMTP;
            deltaMTP = new Vector3(deltaMTP.x, 0, 0);

            float availableM = deltaMTP.x;
            float usedM = Math.Min(availableM, startHealth - (coreM + dynamicM));//if health below normal, get minimum of health needed, available matter, and regeneration
            dynamicM += usedM;
            tKe = finalTemp * (coreM + dynamicM);

            Vector3 incorporated = deltaMTP * usedM / availableM;
            deltaMTP -= incorporated;
            //this many phonons end up going back out after absorption
            deltaMTP.y = deltaMTP.x * finalTemp;

        }

        healthiness = (dynamicM + coreM) / startHealth;

        outMTP += deltaMTP;
    }
    public virtual void continuousIn()
    {
        usedQ = Math.Min(pC / healthiness, inQ);//get max of inQ and power necessary to meet powerconsumption demand after heating
        float netQ = usedQ * healthiness;//usable energy
        tKe += (usedQ - netQ);//ohmic heating (using power because the overall result would be described as the sum, or in calculus the integral.

        outQ = inQ - usedQ;//amount of energy left over
        currentPower = netQ / pC;
        inQ = 0;
    }
    public virtual void continuousOut()
    {

        parent.c.inQ += outQ;
        usedQ = 0;
    }
    public virtual void bruise(float core, float dynamic, bool chunk = false)
    {
        float dynamicDamage = Math.Min(dynamic, dynamicM);
        float leftOverDynamic = dynamic - dynamicDamage;
        float coreDamage = Math.Min(core + leftOverDynamic, coreM);
        float leftOverCore = core - coreDamage;//what is actually cascaded to structure
        float deltaPsion = coreM <= 0 ? 0 : coreDamage / coreM * psionLevel;
        float deltaPhonon = coreM + dynamicM <= 0 ? 0 : (dynamicDamage + coreDamage) / (coreM + dynamicM) * tKe;
        //        print((dynamicDamage + coreDamage) / (coreM + dynamicM) + " " + totalThermalEnergy);
        psionLevel -= deltaPsion;
        addPhonons(-deltaPhonon);
        if (!chunk)
        {
            parent.stomachM += dynamicDamage + coreDamage;//add organ bruising cascade
            parent.psionLevel += deltaPsion;
            parent.addPhonons(deltaPhonon);
        }
        else
        {
            //print(parent.body);
            //float deltaM = Mathf.Min(dynamicDamage + coreDamage, parent.body.transform.mass - .01f);
            //parent.realMass.mass = parent.body.transform.mass -= deltaM;
            //parent.body.updateMatter(-deltaM);            
        }
        dynamicM -= dynamicDamage;
        coreM -= coreDamage;
        healthiness = (coreM + dynamicM) / startHealth;
        if (leftOverCore > 0)
        {
            if (this == parent)
            {
                parent.death("Total loss of functional organ");
            }
            else
                parent.bruise(0, leftOverCore, chunk);
        }
        if (coreM + dynamicM == 0)
        {
            // Debug.LogError("ORGAN DEATH");
        }
    }
    public float getTemperature()
    {
        return tKe / (coreM + dynamicM);
    }
    public void addPhonons(float phonons)
    {
        tKe += phonons;
    }
}