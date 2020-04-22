using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using System;

public class Motor : ChargeableOrgan
{
    //Static parameter
    public int maxBoostCount;


    //Calculated internal variable
    float chargePerBoost;
    public Motor(float startHealth, float powerConsumption, float metabolism, float maxM, float maxCharge, int maxBoostCount) : base(startHealth, powerConsumption, metabolism, maxM, maxCharge)
    {
        this.maxBoostCount = maxBoostCount;
        chargePerBoost = maxCharge / maxBoostCount;
        OrganIndex = 2;
    }
    public override void continuousIn()
    {

        outQ = inQ - usedQ;
        if (outQ > 0)
        {
            if (charge < maxCharge)
            {
                float deltaQ = Math.Min(maxCharge - charge, outQ);
                outQ -= deltaQ;
                charge += deltaQ;
                usedQ += deltaQ;
            }
        }
        currentPower = usedQ / pC;
        inQ = 0;
    }
    internal float getEnergyMultiplier()//called in update, before cycle
    {
        float netQ = inQ * healthiness;
        tKe += (inQ - netQ);//ohmic heating (using power because the overall result would be described as the sum, or in calculus the integral.
        usedQ = inQ;
        return netQ * EnergyManager.speedPerCharge;
    }
    internal float getBoost()
    {
        if (charge > chargePerBoost)
        {

            float netQ = chargePerBoost * healthiness;
            tKe += (chargePerBoost - netQ);//ohmic heating (using power because the overall result would be described as the sum, or in calculus the integral.
            charge -= chargePerBoost;
            return netQ * EnergyManager.speedPerCharge;

        }
        else
        {
            return 0;
        }
    }
}