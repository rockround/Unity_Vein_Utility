using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using System;

public class Capacitor : ChargeableOrgan
{
    public Pump p;
    public Structure s;//this is just parent
    public Vision v;
    public Writer w;
    public Motor m;

    float sD, pD, mD, vD, wD, totalDemand;

    public Capacitor(float startHealth, float powerConsumption, float metabolism, float maxM, float maxCharge) : base(startHealth, powerConsumption, metabolism, maxM, maxCharge)
    {
    }

    public void initPowerDemands()
    {
        s = parent;

        totalDemand = p.pC + m.pC + v.pC + w.pC + s.pC;
        sD = pC / totalDemand;
        pD = p.pC / totalDemand;
        mD = m.pC / totalDemand;
        vD = v.pC / totalDemand;
        wD = w.pC / totalDemand;
    }
    public override void continuousIn()
    {
        charge += inQ;
        charge = Math.Min(maxCharge, charge);
        inQ = 0;
    }
    public override void continuousOut()
    {
        float rawQ;
        // if (charge <= maxQ)// if leq charge than maxQ, flow the smallest of what is needed and what there is
        {
            rawQ = Math.Min(totalDemand / healthiness, charge);
        }
        /*  else// if more charge than maxQ, have over + demand
          {
              rawQ = charge - maxQ + totalDemand;
          }*/
        charge -= rawQ;
        //parent.body.temperature += (rawEnergy * (1 - healthiness))/parent.body.transform.mass;

        float netQ = rawQ * healthiness;
        outQ = netQ;
        if (coreM + dynamicM > 0)
            tKe += rawQ - netQ;//ohmic heating (using power because the overall result would be described as the sum, or in calculus the integral.

        s.inQ += netQ * sD;
        v.inQ += netQ * vD;
        m.inQ += netQ * mD;
        w.inQ += netQ * wD;
        p.inQ += netQ * pD;

    }
}
