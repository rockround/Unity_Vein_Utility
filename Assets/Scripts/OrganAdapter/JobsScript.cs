using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Jobs;
using UnityEngine;
public struct JobsScript:IJob
{
    public const int wI = 0;
    public const int cI = 1;
    public const int mI = 2;
    public const int sI = 3;
    public const int bI = 4;
    public const int pI = 5;
    public const int vI = 6;
    public const int lastChargeableOrgan = 2;
    //input static parameters
    public float[] startHealth, pC, metabolism, maxM;

    //between frame static
    public float[] coreM, dynamicM, tKe, psionLevel;

    //Calculated statistic
    public float[] healthiness, currentPower;

    //dynamic variables in between frames
    public float[] inQ, outQ, usedQ;
    public Vector3[] inMTP, outMTP, toProcessMTP;


    //Simulate write psion charging
    bool usePsions;

    //Static Parameter
    public float baseBps, fatGrowth, fatBreakdown, psionBloodHomeostasis;

    //Between frame resources
    public float stomachM, fatM, crystalPsions, crystalPsionw;

    //Between frame stats
    public float[] charge;

    public float bodyCharge;

    public float curBps;

    //Organ Organization
    public float[] maxCharge;
    float maxBoostCount, drain, beta;

    bool stabilized;

    //Calculated Static Values
    float sDP, cDP, bDP, pDP, mDP, vDP, wDP, totalDemandP;
    float sDE, pDE, mDE, vDE, wDE, totalDemandE;

    //Initializes organ system
    public JobsScript(float[] startHealths, float[] metabolisms, float[] powerConsumptions, float[] maxMs, float[] maxCharges, int maxBoostCount, float betaRate, float drainRate, float fatGrowth, float fatBreakdown, float baseBps, float homeostasis)
    {
        maxM = maxMs;
        startHealth = startHealths;
        pC = powerConsumptions;
        metabolism = metabolisms;
        this.maxBoostCount = maxBoostCount;
        drain = drainRate;
        beta = betaRate;
        dynamicM = new float[startHealth.Length];
        tKe = new float[startHealth.Length];
        psionLevel = new float[startHealth.Length];
        healthiness = new float[startHealth.Length];
        currentPower = new float[startHealth.Length];
        inQ = new float[startHealth.Length];
        outQ = new float[startHealth.Length];
        usedQ = new float[startHealth.Length];
        inMTP = new Vector3[startHealth.Length];
        outMTP = new Vector3[startHealth.Length];
        toProcessMTP = new Vector3[startHealth.Length];
        coreM = new float[startHealths.Length];
        for (int i = 0; i < startHealths.Length; i++)
        {
            coreM[i] = startHealths[i];
            tKe[i] = EnergyManager.roomTemp * coreM[i];
        }
        charge = new float[maxCharges.Length];
        maxCharge = maxCharges;
        //Parameters
        this.fatBreakdown = fatBreakdown;
        this.fatGrowth = fatGrowth;
        this.baseBps = baseBps;
        this.psionBloodHomeostasis = homeostasis;

        fatM = 0;
        crystalPsions = 0;
        crystalPsionw = 0;
        stomachM = 0;
        usePsions = false;
        stabilized = false;
        bodyCharge = 0;
        curBps = 0;
        //is s.startSimulation
        //Pipe Demands
        totalDemandP = maxM.Sum();
        sDP = maxM[sI] / totalDemandP;
        cDP = maxM[cI] / totalDemandP;
        bDP = maxM[bI] / totalDemandP;
        pDP = maxM[pI] / totalDemandP;
        mDP = maxM[mI] / totalDemandP;
        vDP = maxM[vI] / totalDemandP;
        wDP = maxM[wI] / totalDemandP;

        //Power demands
        totalDemandE = pC.Sum();
        sDE = pC[sI] / totalDemandE;
        pDE = pC[pI] / totalDemandE;
        mDE = pC[mI] / totalDemandE;
        vDE = pC[vI] / totalDemandE;
        wDE = pC[wI] / totalDemandE;
    }


    public void absorb()
    {
        float minM = Mathf.Min(metabolism[sI], toProcessMTP[sI].x);
        Vector3 deltaMTP = Vector3.zero;
        float rawPsions = 0;
        float finalTemp = getTemperature(sI);
        if (minM > 0)
        {
            deltaMTP = (minM / toProcessMTP[sI].x) * toProcessMTP[sI];
            rawPsions = deltaMTP.z;
            finalTemp = (tKe[sI] + deltaMTP.y) / (coreM[sI] + dynamicM[sI] + deltaMTP.x);
            toProcessMTP[sI] -= deltaMTP;
            deltaMTP = new Vector3(deltaMTP.x, 0, 0);
        }
        if (Math.Round(toProcessMTP[sI].x + outMTP[sI].x + deltaMTP.x, 3) < totalDemandP)//if blood flow is too low, add directly to output deposit from dynamicM[sI]
        {
            float delta = Mathf.Min(totalDemandP - (outMTP[sI].x + toProcessMTP[sI].x + deltaMTP.x), metabolism[sI]);//get minimum of deficit and what can be offered via metabolism[sI]
            if (dynamicM[sI] > delta)//if enough dynamic, pull from dynamic
            {
                dynamicM[sI] -= delta;
                deltaMTP.x += delta;
            }
            else
            {
                if (coreM[sI] + dynamicM[sI] > delta)//starve
                {
                    deltaMTP.x += delta;
                    float deltaHealth = delta - dynamicM[sI];
                    psionLevel[sI] += crystalPsions * deltaHealth / coreM[sI]; //add to body psions from crystal proportional to percent of matter lost
                    crystalPsions *= 1 - deltaHealth / coreM[sI];// subtract percent of psions lost due to matter loss
                    coreM[sI] -= deltaHealth;
                    dynamicM[sI] = 0;
                }
                else
                {
                    Console.WriteLine("Death by Starvation");
                }
            }
        }
        else//care about self after caring for other organs
        {
            if (coreM[sI] + dynamicM[sI] > startHealth[sI])//if more than enough total health, get fat using matter from deltaM.
            {
                float delta = Mathf.Min(fatGrowth, (coreM[sI] + dynamicM[sI]) - startHealth[sI]);
                fatM += delta;//gain fat
                dynamicM[sI] -= delta;
            }
            else //if less  than enough total health, use deltaM
            {
                float usedM = Mathf.Min(Mathf.Min(fatM, fatBreakdown), startHealth[sI] - (coreM[sI] + dynamicM[sI]));//if health below normal, get minimum of health needed, available matter, and regeneration
                dynamicM[sI] += usedM;
                fatM -= usedM;
            }
        }
        if (Math.Round(toProcessMTP[sI].z + outMTP[sI].z, 3) < psionBloodHomeostasis * (outMTP[sI].x + deltaMTP.x + toProcessMTP[sI].x))//Is current concentration acceptable?
        {//too low
            float delta = psionBloodHomeostasis * (outMTP[sI].x + deltaMTP.x + toProcessMTP[sI].x) - (toProcessMTP[sI].z + outMTP[sI].z);
            if (delta > rawPsions)
            {
                if (psionLevel[sI] + rawPsions > delta)
                {
                    psionLevel[sI] -= delta - rawPsions;
                    outMTP[sI].z += delta;
                }
                else if (psionLevel[sI] + crystalPsions + rawPsions > delta)
                {
                    crystalPsions -= delta - rawPsions - psionLevel[sI];
                    psionLevel[sI] = 0;
                    outMTP[sI].z += delta;
                }
                else
                {
                    outMTP[sI].z += crystalPsions + psionLevel[sI] + rawPsions;
                    crystalPsions = 0;
                    psionLevel[sI] = 0;
                }
                rawPsions = 0;
            }
            else
            {
                outMTP[sI].z += delta;
                rawPsions -= delta;
                crystalPsions += rawPsions;
            }
        }
        float crystalCap = coreM[sI] * EnergyManager.psionPerKg;

        if (crystalPsions < crystalCap)//if crystal not yet fill
        {
            float delta = Mathf.Min(rawPsions, crystalCap - crystalPsions);
            crystalPsions += delta;
            rawPsions -= delta;
            psionLevel[sI] += rawPsions;
            rawPsions = 0;
        }
        else
        {
            psionLevel[sI] += rawPsions;
            rawPsions = 0;
        }
        tKe[sI] = finalTemp * (coreM[sI] + dynamicM[sI]);
        deltaMTP.y = deltaMTP.x * finalTemp;
        outMTP[sI] += deltaMTP;
        healthiness[sI] = (dynamicM[sI] + coreM[sI]) / startHealth[sI];

    }

    public void bruise(float core, float dynamic, int idx, bool chunk = false)
    {
        float dynamicDamage = Mathf.Min(dynamic, dynamicM[idx]);
        float leftOverDynamic = dynamic - dynamicDamage;
        float coreDamage = Mathf.Min(core + leftOverDynamic, coreM[idx]);
        float leftOverCore = core - coreDamage;//what is actually cascaded to structure
        float deltaPsion = coreM[idx] <= 0 ? 0 : coreDamage / coreM[idx] * psionLevel[idx];
        float deltaPhonon = coreM[idx] + dynamicM[idx] <= 0 ? 0 : (dynamicDamage + coreDamage) / (coreM[idx] + dynamicM[idx]) * tKe[idx];
        psionLevel[idx] -= deltaPsion;
        tKe[idx] += -deltaPhonon;
        if (!chunk)
        {
            stomachM += dynamicDamage + coreDamage;//add organ bruising cascade
            psionLevel[idx] += deltaPsion;
            tKe[sI] += deltaPhonon;
        }
        dynamicM[idx] -= dynamicDamage;
        coreM[idx] -= coreDamage;
        healthiness[idx] = (coreM[idx] + dynamicM[idx]) / startHealth[idx];
        if (leftOverCore > 0)
        {
            if (idx == sI)
            {
                Console.WriteLine("Total loss of functional organ");
            }
            else
                bruise(0, leftOverCore, sI, chunk);
        }
        if (coreM[idx] + dynamicM[idx] == 0)
        {
            // Debug.LogError("ORGAN DEATH");
        }
    }

    float contaminationRate
    {
        get
        {
            return (float)Mathf.Exp(-16 / beta) / 4;
        }
    }
    internal float getTemperature(int idx)
    {
        if (idx == sI)
            return tKe[idx] / (coreM[idx] + dynamicM[idx] + stomachM + fatM);
        else
            return tKe[idx] / (coreM[idx] + dynamicM[idx]);
    }

    float bps
    {
        get
        {

            float realBps = (float)Math.Round(baseBps * getTemperature(sI) / EnergyManager.roomTemp * currentPower[sI] * healthiness[sI], 3);
            //Console.WriteLine(temperature + " " + currentPower + " " + healthiness);
            if (float.IsInfinity(realBps) || float.IsNaN(realBps))
            {
                //throw new Exception("BPS IS NAN OR INFINITY " + temperature + " is temp " + currentPower + " is currentPower");
                realBps = baseBps;
                return baseBps;
            }
            if (!stabilized)
            {
                if (realBps <= 0)
                {
                    return 1;
                }
                else
                {
                    stabilized = true;
                    return realBps;
                }
            }
            else
            {
                if (realBps <= 0)
                {
                    if (tKe[sI] == 0)
                        Console.WriteLine("Death by Hypothermia");
                    else if (currentPower[sI] == 0)
                        Console.WriteLine("Death by Energy Deficiency");
                    else
                        Console.WriteLine("Death by Cardiac Damage");
                    return 1;
                }
                else
                {
                    return realBps;
                }
            }

        }
    }


    internal IEnumerable<float> Discrete()
    {

        yield return 0;

        while (true)
        {


            float time = 0;
            curBps = bps;




            //this is s.flowIn()
            toProcessMTP[sI] = inMTP[sI];
            outMTP[sI] = inMTP[sI] = Vector3.zero;

            //this is v.flowIn()
            toProcessMTP[vI] = inMTP[vI];
            outMTP[vI] = inMTP[vI] = Vector3.zero;

            //this is b.flowIn()
            toProcessMTP[bI] = inMTP[bI];
            outMTP[bI] = inMTP[bI] = Vector3.zero;

            //this is c.flowIn()
            toProcessMTP[cI] = inMTP[cI];
            outMTP[cI] = inMTP[cI] = Vector3.zero;

            //this is w.flowIn()
            toProcessMTP[wI] = inMTP[wI];
            outMTP[wI] = inMTP[wI] = Vector3.zero;

            //this is m.flowIn()
            toProcessMTP[mI] = inMTP[mI];
            outMTP[mI] = inMTP[mI] = Vector3.zero;

            //this is p.flowIn()
            toProcessMTP[pI] = inMTP[pI];
            outMTP[pI] = inMTP[pI] = Vector3.zero;

            if (time >= 1 / curBps)
            {
                yield return 0;
            }
            else
            {
                while (time < 1 / curBps)
                {

                    //this is s.continuousIn
                    usedQ[sI] = Mathf.Min(pC[sI] / healthiness[sI], inQ[sI]);//get max of inQ and power necessary to meet powerconsumption demand after heating
                    float netQs = usedQ[sI] * healthiness[sI];//usable energy
                    tKe[sI] += (usedQ[sI] - netQs);//ohmic heating (using power because the overall result would be described as the sum, or in calculus the integral.
                    outQ[sI] = inQ[sI] - usedQ[sI];//amount of energy left over
                    currentPower[sI] = netQs / pC[sI];
                    inQ[sI] = 0;


                    //this is v.continuousIn
                    usedQ[vI] = Mathf.Min(pC[vI] / healthiness[vI], inQ[vI]);//get max of inQ and power necessary to meet powerconsumption demand after heating
                    float netQv = usedQ[vI] * healthiness[vI];//usable energy
                    tKe[vI] += (usedQ[vI] - netQv);//ohmic heating (using power because the overall result would be described as the sum, or in calculus the integral.
                    outQ[vI] = inQ[vI] - usedQ[vI];//amount of energy left over
                    currentPower[vI] = netQv / pC[vI];
                    inQ[vI] = 0;
                    //this is v.absorb()
                    float minMv = Mathf.Min(metabolism[vI], toProcessMTP[vI].x);
                    Vector3 deltaMTPv = Vector3.zero;
                    if (minMv > 0)
                    {
                        deltaMTPv = minMv / toProcessMTP[vI].x * toProcessMTP[vI];
                        psionLevel[vI] += deltaMTPv.z;
                        float finalTemp = (tKe[vI] + deltaMTPv.y) / (coreM[vI] + dynamicM[vI] + deltaMTPv.x);
                        toProcessMTP[vI] -= deltaMTPv;
                        deltaMTPv = new Vector3(deltaMTPv.x, 0, 0);
                        float availableM = deltaMTPv.x;
                        float usedM = Mathf.Min(availableM, startHealth[vI] - (coreM[vI] + dynamicM[vI]));//if health below normal, get minimum of health needed, available matter, and regeneration
                        dynamicM[vI] += usedM;
                        tKe[vI] = finalTemp * (coreM[vI] + dynamicM[vI]);
                        Vector3 incorporated = deltaMTPv * usedM / availableM;
                        deltaMTPv -= incorporated;
                        deltaMTPv.y = deltaMTPv.x * finalTemp;
                    }
                    healthiness[vI] = (dynamicM[vI] + coreM[vI]) / startHealth[vI];
                    outMTP[vI] += deltaMTPv;

                    //b.continuousIn() does nothing
                    //this is b.absorb()
                    float finalTempB = getTemperature(bI);
                    float minMb = Mathf.Min(metabolism[bI], toProcessMTP[bI].x);
                    Vector3 deltaMTPb = Vector3.zero;
                    if (minMb > 0)
                    {
                        deltaMTPb = minMb / toProcessMTP[bI].x * toProcessMTP[bI];
                        psionLevel[bI] += deltaMTPb.z;
                        finalTempB = (tKe[bI] + deltaMTPb.y) / (coreM[bI] + dynamicM[bI] + deltaMTPb.x);
                        toProcessMTP[bI] -= deltaMTPb;
                        deltaMTPb = new Vector3(deltaMTPb.x, 0, 0);
                    }
                    bruise(0, startHealth[bI] * contaminationRate, bI);//damaged with contamination (at end because this is cobered up by inM in flowing
                    if (minMb > 0)
                    {
                        float usedM = Mathf.Min(deltaMTPb.x, startHealth[bI] - (coreM[bI] + dynamicM[bI]));//if health below normal, get minimum of health needed, abailable matter, and regeneration
                        dynamicM[bI] += usedM;
                        tKe[bI] = finalTempB * (coreM[bI] + dynamicM[bI]);
                        Vector3 incorporated = deltaMTPb * usedM / deltaMTPb.x;
                        deltaMTPb -= incorporated;
                        deltaMTPb.y = deltaMTPb.x * finalTempB;
                    }
                    healthiness[bI] = (dynamicM[bI] + coreM[bI]) / startHealth[bI];
                    outMTP[bI] += deltaMTPb;

                    //this is c.continuousIn
                    charge[cI] += inQ[cI];
                    charge[cI] = Mathf.Min(maxCharge[cI], charge[cI]);
                    inQ[cI] = 0;

                    //this is w.continuousIn()
                    outQ[wI] = inQ[wI] - usedQ[wI];
                    currentPower[wI] = usedQ[wI] / pC[wI];
                    inQ[wI] = 0;

                    //this is w.absorb()
                    float rawPsions = 0;
                    float minMw = Mathf.Min(metabolism[wI], toProcessMTP[wI].x);
                    Vector3 deltaMTPw = Vector3.zero;
                    if (minMw > 0)
                    {
                        deltaMTPw = minMw / toProcessMTP[wI].x * toProcessMTP[wI];
                        psionLevel[wI] += deltaMTPw.z;
                        float finalTemp = (tKe[wI] + deltaMTPw.y) / (coreM[wI] + dynamicM[wI] + deltaMTPw.x);
                        toProcessMTP[wI] -= deltaMTPw;
                        deltaMTPw = new Vector3(deltaMTPw.x, 0, 0);
                        float usedM = Mathf.Min(deltaMTPw.x, startHealth[wI] - (coreM[wI] + dynamicM[wI]));//if health below normal, get minimum of health needed, awailable matter, and regeneration
                        dynamicM[wI] += usedM;
                        tKe[wI] = finalTemp * (coreM[wI] + dynamicM[wI]);
                        Vector3 incorporated = deltaMTPw * usedM / deltaMTPw.x;
                        deltaMTPw -= incorporated;
                        deltaMTPw.y = deltaMTPw.x * finalTemp;
                    }
                    if (usePsions)
                    {
                        crystalPsionw += rawPsions;
                        rawPsions = 0;
                        usePsions = false;
                    }
                    healthiness[wI] = (dynamicM[wI] + coreM[wI]) / startHealth[wI];
                    outMTP[wI] += deltaMTPw;
                    psionLevel[wI] += rawPsions;



                    //this is m.continuousIn()
                    outQ[mI] = inQ[mI] - usedQ[mI];
                    if (outQ[mI] > 0)
                    {
                        if (charge[mI] < maxCharge[mI])
                        {
                            float deltaQ = Mathf.Min(maxCharge[mI] - charge[mI], outQ[mI]);
                            outQ[mI] -= deltaQ;
                            charge[mI] += deltaQ;
                            usedQ[mI] += deltaQ;
                        }
                    }
                    currentPower[mI] = usedQ[mI] / pC[mI];
                    inQ[mI] = 0;
                    //this is m.absorb()
                    float minMm = Mathf.Min(metabolism[mI], toProcessMTP[mI].x);
                    Vector3 deltaMTPm = Vector3.zero;
                    if (minMm > 0)
                    {
                        deltaMTPm = minMm / toProcessMTP[mI].x * toProcessMTP[mI];
                        psionLevel[mI] += deltaMTPm.z;
                        float finalTemp = (tKe[mI] + deltaMTPm.y) / (coreM[mI] + dynamicM[mI] + deltaMTPm.x);
                        toProcessMTP[mI] -= deltaMTPm;
                        deltaMTPm = new Vector3(deltaMTPm.x, 0, 0);
                        float usedM = Mathf.Min(deltaMTPm.x, startHealth[mI] - (coreM[mI] + dynamicM[mI]));//if health below normal, get minimum of health needed, available matter, and regeneration
                        dynamicM[mI] += usedM;
                        tKe[mI] = finalTemp * (coreM[mI] + dynamicM[mI]);
                        Vector3 incorporated = deltaMTPm * usedM / deltaMTPm.x;
                        deltaMTPm -= incorporated;
                        deltaMTPm.y = deltaMTPm.x * finalTemp;
                    }
                    healthiness[mI] = (dynamicM[mI] + coreM[mI]) / startHealth[mI];
                    outMTP[mI] += deltaMTPm;



                    //this is p.continuousIn
                    usedQ[pI] = Mathf.Min(pC[pI] / healthiness[pI], inQ[pI]);//get max of inQ and power necessary to meet powerconsumption demand after heating
                    float netQp = usedQ[pI] * healthiness[pI];//usable energy
                    tKe[pI] += (usedQ[pI] - netQp);//ohmic heating (using power because the operall result would be described as the sum, or in calculus the integral.
                    outQ[pI] = inQ[pI] - usedQ[pI];//amount of energy left oper
                    currentPower[pI] = netQp / pC[pI];
                    inQ[pI] = 0;







                    //this is c.absorb()
                    float minMc = Mathf.Min(metabolism[cI], toProcessMTP[cI].x);
                    Vector3 deltaMTPc = Vector3.zero;
                    if (minMc > 0)
                    {
                        deltaMTPc = minMc / toProcessMTP[cI].x * toProcessMTP[cI];
                        psionLevel[cI] += deltaMTPc.z;
                        float finalTemp = (tKe[cI] + deltaMTPc.y) / (coreM[cI] + dynamicM[cI] + deltaMTPc.x);
                        toProcessMTP[cI] -= deltaMTPc;
                        deltaMTPc = new Vector3(deltaMTPc.x, 0, 0);
                        float usedM = Mathf.Min(deltaMTPc.x, startHealth[cI] - (coreM[cI] + dynamicM[cI]));//if health below normal, get minimum of health needed, available matter, and regeneration
                        dynamicM[cI] += usedM;
                        tKe[cI] = finalTemp * (coreM[cI] + dynamicM[cI]);
                        Vector3 incorporated = deltaMTPc * usedM / deltaMTPc.x;
                        deltaMTPc -= incorporated;
                        deltaMTPc.y = deltaMTPc.x * finalTemp;
                    }
                    healthiness[cI] = (dynamicM[cI] + coreM[cI]) / startHealth[cI];
                    outMTP[cI] += deltaMTPc;






                    float minMp = Mathf.Min(metabolism[pI], toProcessMTP[pI].x);
                    Vector3 deltaMTPp = Vector3.zero;
                    if (minMp > 0)
                    {
                        deltaMTPp = minMp / toProcessMTP[pI].x * toProcessMTP[pI];
                        psionLevel[pI] += deltaMTPp.z;
                        float finalTemp = (tKe[pI] + deltaMTPp.y) / (coreM[pI] + dynamicM[pI] + deltaMTPp.x);
                        toProcessMTP[pI] -= deltaMTPp;
                        deltaMTPp = new Vector3(deltaMTPp.x, 0, 0);
                        float apailableM = deltaMTPp.x;
                        float usedM = Mathf.Min(apailableM, startHealth[pI] - (coreM[pI] + dynamicM[pI]));//if health below normal, get minimum of health needed, apailable matter, and regeneration
                        dynamicM[pI] += usedM;
                        tKe[pI] = finalTemp * (coreM[pI] + dynamicM[pI]);
                        Vector3 incorporated = deltaMTPp * usedM / apailableM;
                        deltaMTPp -= incorporated;
                        deltaMTPp.y = deltaMTPp.x * finalTemp;
                    }
                    healthiness[pI] = (dynamicM[pI] + coreM[pI]) / startHealth[pI];
                    float toCpt = Mathf.Min(stomachM, drain) * currentPower[pI];
                    stomachM -= toCpt;
                    dynamicM[sI] += toCpt;
                    float realP = toCpt * EnergyManager.psionPerKg;
                    outMTP[pI].z += realP * healthiness[pI];
                    psionLevel[pI] += realP * (1 - healthiness[pI]);
                    outMTP[pI] += deltaMTPp;

                    absorb();




                    //this is v.continuousOut
                    inQ[cI] += outQ[vI];
                    usedQ[vI] = 0;


                    //this is b.continuousOut()
                    float realFlow = beta;
                    bodyCharge += -realFlow * (1 - healthiness[bI]);//leak charge into body
                    inQ[cI] += realFlow * healthiness[bI];

                    //this is c.continuousOut
                    float rawQ = Mathf.Min(totalDemandE / healthiness[cI], charge[cI]);//Take as much eneryg as needed (taking into account heating)
                    charge[cI] -= rawQ;
                    float netQ = rawQ * healthiness[cI];
                    outQ[cI] = netQ;
                    if (coreM[cI] + dynamicM[cI] > 0)
                        tKe[cI] += rawQ - netQ;//ohmic heating (using power because the overall result would be described as the sum, or in calculus the integral.
                    inQ[wI] = netQ * wDE;
                    inQ[mI] = netQ * mDE;
                    inQ[vI] = netQ * vDE;
                    inQ[pI] = netQ * pDE;
                    inQ[sI] = netQ * sDE;


                    //this is w.continuousOut
                    inQ[cI] += outQ[wI];
                    usedQ[wI] = 0;
                    //this is m.continuousOut
                    inQ[cI] += outQ[mI];
                    usedQ[mI] = 0;
                    //this is p.continuousOut
                    inQ[cI] += outQ[pI];
                    usedQ[pI] = 0;
                    //this is s.continuousOut
                    inQ[cI] += outQ[sI];
                    usedQ[sI] = 0;


                    time += 0.05f;

                    yield return .05f;
                }

                //this is v.flowOut
                Vector3 netMTPv = outMTP[vI] * healthiness[vI];
                float psionsReleasedv = 0;
                if (coreM[vI] > 0)
                {
                    psionsReleasedv = psionLevel[vI] / coreM[vI] * netMTPv.x;
                }
                psionLevel[vI] -= psionsReleasedv;
                Vector3 remainingToFlowv = toProcessMTP[vI] * healthiness[vI];
                outMTP[vI] += new Vector3(0, 0, psionsReleasedv);
                inMTP[sI] += netMTPv + remainingToFlowv;
                stomachM += outMTP[vI].x - netMTPv.x + toProcessMTP[vI].x - remainingToFlowv.x;
                psionLevel[sI] += outMTP[vI].z - netMTPv.z + toProcessMTP[vI].z - remainingToFlowv.z;
                tKe[sI] += (outMTP[vI].y - netMTPv.y + toProcessMTP[vI].y - remainingToFlowv.y);


                //this is b.flowOut
                Vector3 netMTPb = outMTP[bI] * healthiness[bI];
                float psionsReleasedb = 0;
                if (coreM[bI] > 0)
                {
                    psionsReleasedb = psionLevel[bI] / coreM[bI] * netMTPb.x;
                }
                psionLevel[bI] -= psionsReleasedb;
                Vector3 remainingToFlowb = toProcessMTP[bI] * healthiness[bI];
                outMTP[bI] += new Vector3(0, 0, psionsReleasedb);
                inMTP[sI] += netMTPb + remainingToFlowb;
                stomachM += outMTP[bI].x - netMTPb.x + toProcessMTP[bI].x - remainingToFlowb.x;
                psionLevel[sI] += outMTP[bI].z - netMTPb.z + toProcessMTP[bI].z - remainingToFlowb.z;
                tKe[sI] += (outMTP[bI].y - netMTPb.y + toProcessMTP[bI].y - remainingToFlowb.y);



                //this is c.flowOut
                Vector3 netMTPc = outMTP[cI] * healthiness[cI];
                float psionsReleasedc = 0;
                if (coreM[cI] > 0)
                {
                    psionsReleasedc = psionLevel[cI] / coreM[cI] * netMTPc.x;
                }
                psionLevel[cI] -= psionsReleasedc;
                Vector3 remainingToFlowc = toProcessMTP[cI] * healthiness[cI];
                outMTP[cI] += new Vector3(0, 0, psionsReleasedc);
                inMTP[sI] += netMTPc + remainingToFlowc;
                stomachM += outMTP[cI].x - netMTPc.x + toProcessMTP[cI].x - remainingToFlowc.x;
                psionLevel[sI] += outMTP[cI].z - netMTPc.z + toProcessMTP[cI].z - remainingToFlowc.z;
                tKe[sI] += (outMTP[cI].y - netMTPc.y + toProcessMTP[cI].y - remainingToFlowc.y);


                //this is w.flowOut()
                Vector3 netMTPw = outMTP[wI] * healthiness[wI];
                float psionsReleasedw = 0;
                if (coreM[wI] > 0)
                {
                    psionsReleasedw = psionLevel[wI] / coreM[wI] * netMTPw.x;
                }
                psionLevel[wI] -= psionsReleasedw;
                Vector3 remainingToFloww = toProcessMTP[wI] * healthiness[wI];
                outMTP[wI] += new Vector3(0, 0, psionsReleasedw);
                inMTP[sI] += netMTPw + remainingToFloww;
                stomachM += outMTP[wI].x - netMTPw.x + toProcessMTP[wI].x - remainingToFloww.x;
                psionLevel[sI] += outMTP[wI].z - netMTPw.z + toProcessMTP[wI].z - remainingToFloww.z;
                tKe[sI] += (outMTP[wI].y - netMTPw.y + toProcessMTP[wI].y - remainingToFloww.y);


                //this is m.flowOut()
                Vector3 netMTPm = outMTP[mI] * healthiness[mI];
                float psionsReleasedm = 0;
                if (coreM[mI] > 0)
                {
                    psionsReleasedm = psionLevel[mI] / coreM[mI] * netMTPm.x;
                }
                psionLevel[mI] -= psionsReleasedm;
                Vector3 remainingToFlowm = toProcessMTP[mI] * healthiness[mI];
                outMTP[mI] += new Vector3(0, 0, psionsReleasedm);
                inMTP[sI] += netMTPm + remainingToFlowm;
                stomachM += outMTP[mI].x - netMTPm.x + toProcessMTP[mI].x - remainingToFlowm.x;
                psionLevel[sI] += outMTP[mI].z - netMTPm.z + toProcessMTP[mI].z - remainingToFlowm.z;
                tKe[sI] += (outMTP[mI].y - netMTPm.y + toProcessMTP[mI].y - remainingToFlowm.y);


                //this is p.flowOut()
                Vector3 netMTPp = outMTP[pI] * healthiness[pI];
                float psionsReleasedp = 0;
                if (coreM[pI] > 0)
                {
                    psionsReleasedp = psionLevel[pI] / coreM[pI] * netMTPp.x;
                }
                psionLevel[pI] -= psionsReleasedp;
                Vector3 remainingToFlowp = toProcessMTP[pI] * healthiness[pI];
                outMTP[pI] += new Vector3(0, 0, psionsReleasedp);
                inMTP[sI] += netMTPp + remainingToFlowp;
                stomachM += outMTP[pI].x - netMTPp.x + toProcessMTP[pI].x - remainingToFlowp.x;
                psionLevel[sI] += outMTP[pI].z - netMTPp.z + toProcessMTP[pI].z - remainingToFlowp.z;
                tKe[sI] += (outMTP[pI].y - netMTPp.y + toProcessMTP[pI].y - remainingToFlowp.y);


                //this is s.flowOut()
                float psionsReleaseds = psionLevel[sI] / coreM[sI] * outMTP[sI].x;
                psionLevel[sI] -= psionsReleaseds;
                outMTP[sI] += new Vector3(0, 0, psionsReleaseds) + toProcessMTP[sI];
                inMTP[cI] = outMTP[sI] * cDP;
                inMTP[wI] = outMTP[sI] * wDP;
                inMTP[mI] = outMTP[sI] * mDP;
                inMTP[vI] = outMTP[sI] * vDP;
                inMTP[pI] = outMTP[sI] * pDP;
                inMTP[bI] = outMTP[sI] * bDP;
                inMTP[sI] += outMTP[sI] * sDP;

                //Console.WriteLine(inMTP[sI] + " " + inMTP[bI] + " " + inMTP[pI]);

            }
            yield return .01f;
        }
    }

    public void Execute()
    {
        throw new NotImplementedException();
    }
}
