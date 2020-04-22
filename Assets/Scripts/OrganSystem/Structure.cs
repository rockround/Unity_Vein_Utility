using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using System;
using Unity.Collections;
using UnityEngine.Experimental.AI;

public class Structure : Organ
{
    //Static Parameter
    public float baseBps, fatGrowth, fatBreakdown, psionBloodHomeostasis;

    //Between frame resources
    public float stomachM, fatM, crystalPsion;

    //Between frame stats
    public float charge, curBps;

    //Organ Organization

    public Writer w;
    public Capacitor c;
    public Motor m;
    public Beta b;
    public Pump p;
    public Vision v;
    public Organ[] organs;

    bool stabilized;

    //Calculated Static Values
    float totalDemand;
    float[] demands;
    internal NativeHashMap<Vector2Int, Vector3> mtps;



    //Initializes organ system
    public Structure(float[] startHealths, float[] metabolisms, float[] powerConsumptions, float[] maxMs, float[] maxCharges, int maxBoostCount, float betaRate, float drainRate, float fatGrowth, float fatBreakdown, float baseBps, float homeostasis) : base(startHealths[StructureI], powerConsumptions[StructureI], metabolisms[StructureI], maxMs[StructureI])
    {
        //Instantiate Organs
        c = new Capacitor(startHealths[CapacitorI], powerConsumptions[CapacitorI], metabolisms[CapacitorI], maxMs[CapacitorI], maxCharges[CapacitorI]);
        w = new Writer(startHealths[WriterI], powerConsumptions[WriterI], metabolisms[WriterI], maxMs[WriterI], maxCharges[WriterI]);
        m = new Motor(startHealths[MotorI], powerConsumptions[MotorI], metabolisms[MotorI], maxMs[MotorI], maxCharges[MotorI], maxBoostCount);
        b = new Beta(startHealths[BetaI], powerConsumptions[BetaI], metabolisms[BetaI], maxMs[BetaI], betaRate);
        p = new Pump(startHealths[PumpI], powerConsumptions[PumpI], metabolisms[PumpI], maxMs[PumpI], drainRate);
        v = new Vision(startHealths[VisionI], powerConsumptions[VisionI], metabolisms[VisionI], maxMs[VisionI]);

        organs = new Organ[] { w, c, m, this, b, p, v };
        demands = new float[7];
        c.parent = w.parent = m.parent = b.parent = p.parent = v.parent = parent = this;

        c.p = p;
        c.s = this;
        c.v = v;
        c.w = w;
        c.m = m;
        //Parameters
        this.fatBreakdown = fatBreakdown;
        this.fatGrowth = fatGrowth;
        this.baseBps = baseBps;
        this.psionBloodHomeostasis = homeostasis;
        OrganIndex = 3;
    }

    //Actually starts it
    public void startSimulation()
    {


        initPipeDemands();
        c.initPowerDemands();
    }
    public override void absorb()
    {
        float minM = Math.Min(metabolism, toProcessMTP.x);
        //Console.WriteLine(minM);
        Vector3 deltaMTP = Vector3.zero;
        float rawPsions = 0;
        float finalTemp = getTemperature();
        if (minM > 0)
        {
            deltaMTP = (minM / toProcessMTP.x) * toProcessMTP;
            rawPsions = deltaMTP.z;


            //Calculate total diffusion of blood and organ regardless of how much blood is used
            finalTemp = (tKe + deltaMTP.y) / (coreM + dynamicM + deltaMTP.x);

            toProcessMTP -= deltaMTP;
            deltaMTP = new Vector3(deltaMTP.x, 0, 0);
        }



        //addPhonons(deltaMTP.y);


        //Console.WriteLine(toProcessMTP + " " + deltaMTP.x);
        //continuous starvation - healing -> body uses inM to heal itself, finds that it doesn't have enough for everyone else, gives part of itself to everyone. This oscillates until equilibrium
        //If blood flow will be low regardless of how much blood is currently here
        if (Math.Round(toProcessMTP.x + outMTP.x + deltaMTP.x, 3) < totalDemand)//if blood flow is too low, add directly to output deposit from dynamicM
        {
            //Console.WriteLine("LESS THAN ENOUGH BY " + (totalDemand - Math.Round(toProcessMTP.x + outMTP.x + deltaMTP.x, 3)));
            //TODO: Do I need this?
            //  if (deltaMTP.x == 0)//if toProcessM is depleted but the stuff in outM isn't enough -> deltaM would be zero. If this isn't here, even if there is stuff ready to process, it will detect a defecit.  DO I NEED THIS?
            {

                //print(totalDemand - (outM + toProcessM + deltaM) + " " + deltaM);
                float delta = Math.Min(totalDemand - (outMTP.x + toProcessMTP.x + deltaMTP.x), metabolism);//get minimum of deficit and what can be offered via metabolism

                if (dynamicM > delta)//if enough dynamic, pull from dynamic
                {
                    dynamicM -= delta;
                    deltaMTP.x += delta;
                }
                else
                {

                    if (coreM + dynamicM > delta)//starve
                    {
                        deltaMTP.x += delta;
                        float deltaHealth = delta - dynamicM;
                        psionLevel += crystalPsion * deltaHealth / coreM; //add to body psions from crystal proportional to percent of matter lost
                        crystalPsion *= 1 - deltaHealth / coreM;// subtract percent of psions lost due to matter loss
                        coreM -= deltaHealth;
                        dynamicM = 0;
                    }
                    else
                    {
                        death("Death by Starvation");
                        //death by starvation
                    }
                }
            }

        }
        else//care about self after caring for other organs
        {
            //if (deltaMTP.x > 0)//if I have more than enough health left
            //    print(deltaMTP.x);
            //Seems like using this area of code destroys things
            //print(startHealth - (coreM + dynamicM));
            if (coreM + dynamicM > startHealth)//if more than enough total health, get fat using matter from deltaM.
            {
                // if (Math.Round(toProcessM + outM + deltaM, 3) >= totalDemand)//if enough dynamic + core to get fat, and dynamic isn't needed for future healing or resupplying
                // {
                //if full health
                float delta = Math.Min(fatGrowth, (coreM + dynamicM) - startHealth);
                fatM += delta;//gain fat

                //if (dynamicM < delta)
                //    print("THIS SHOULDN't BE HAPPENING!");

                dynamicM -= delta;
                //Vector3 incorporated = deltaMTP * delta / deltaMTP.x;
                //deltaMTP -= incorporated;

                // }
            }
            //stomachM already pumped into dynamicM by default. Deducting anything from deltaM will cause a defecit in flowM, leading to decrease in overall flow statically. To heal dynamicM, can only use fatM for direct. Because of beta, flow will always return less than needed, forcing deltaM to always be zero here
            else //if less  than enough total health, use deltaM
            {
                float usedM = Math.Min(Math.Min(fatM, fatBreakdown), startHealth - (coreM + dynamicM));//if health below normal, get minimum of health needed, available matter, and regeneration
                dynamicM += usedM;
                fatM -= usedM;
            }
        }

        //print(toProcessMTP.z + outMTP.z + " " + (outMTP.x + deltaMTP.x + toProcessMTP.x) + " rawpsions " + rawPsions);
        ///Psions are like fat -> fat doesn't go directly to any one place in the body on command, but when sugar levels are low fat is put there. There is a baseline sugar concentration expected.

        if (Math.Round(toProcessMTP.z + outMTP.z, 3) < psionBloodHomeostasis * (outMTP.x + deltaMTP.x + toProcessMTP.x))//Is current concentration acceptable?
        {//too low
         //print("TOO LOW " + rawPsions);
            float delta = psionBloodHomeostasis * (outMTP.x + deltaMTP.x + toProcessMTP.x) - (toProcessMTP.z + outMTP.z);
            if (delta > rawPsions)
            {
                if (psionLevel + rawPsions > delta)
                {
                    psionLevel -= delta - rawPsions;
                    outMTP.z += delta;
                }
                else if (psionLevel + crystalPsion + rawPsions > delta)
                {
                    crystalPsion -= delta - rawPsions - psionLevel;
                    psionLevel = 0;
                    outMTP.z += delta;
                }
                else
                {
                    outMTP.z += crystalPsion + psionLevel + rawPsions;
                    crystalPsion = 0;
                    psionLevel = 0;
                }
                rawPsions = 0;
            }
            else
            {
                outMTP.z += delta;
                rawPsions -= delta;
                crystalPsion += rawPsions;
            }

        }
        float crystalCap = coreM * EnergyManager.psionPerKg;

        //Add whatever is left
        {//there is a surplus, so put it somewhere  -> Body has no way of decreasing blood psion level unless it is directly stored into glyphWriter manually, or leakage.
         //this part uses remaining rawpsions
            ///print("TOO HIGH " + rawPsions);
            if (crystalPsion < crystalCap)//if crystal not yet fill
            {
                float delta = Math.Min(rawPsions, crystalCap - crystalPsion);
                crystalPsion += delta;
                rawPsions -= delta;
                psionLevel += rawPsions;
                rawPsions = 0;
                //psionLevel += usedP - delta;//add whatever is left over to body
            }
            else
            {
                psionLevel += rawPsions;
                rawPsions = 0;
            }

        }


        //Console.WriteLine("LESS THAN ENOUGH BY " + (totalDemand - Math.Round(toProcessMTP.x + outMTP.x + deltaMTP.x, 3)));
        //Console.WriteLine(deltaMTP);


        tKe = finalTemp * (coreM + dynamicM);
        //this many phonons end up going back out after absorption
        deltaMTP.y = deltaMTP.x * finalTemp;


        outMTP += deltaMTP;

        healthiness = (dynamicM + coreM) / startHealth;

    }

    public void initPipeDemands()
    {
        totalDemand = c.maxM + b.maxM + p.maxM + m.maxM + v.maxM + w.maxM + maxM;
        for(int i = 0; i < 7; i++)
        {
            demands[i] = organs[i].maxM / totalDemand;
        }
    }
    float temperature
    {
        get
        {
            return tKe / (coreM + dynamicM + stomachM + fatM);
        }
    }

    float bps
    {
        get
        {

            float realBps = (float)Math.Round(baseBps * temperature / EnergyManager.roomTemp * currentPower * healthiness, 3);
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
                    if (tKe == 0)
                        death("Death by Hypothermia");
                    else if (currentPower == 0)
                        death("Death by Energy Deficiency");
                    else
                        death("Death by Cardiac Damage");
                    return 1;
                }
                else
                {
                    return realBps;
                }
            }

        }
    }
    /// <summary>
    /// </summary>
    public override void flowOut()
    {
        //float netMatter = outM;// * healthiness;
        //stomachM += outM - netMatter;

        // float netPsion = inP;// * healthiness;
        //psionLevel += netPsion;
        //body.psionLevel += inP - netPsion;

        //reason to not have psionDemand: Since distribution is already happening via matter diffusion on matter mtp exchange, it is unnecessary and also unnatural. This would give double edged blade effect for increasing blood flow

        //float phononsReleased = getTemperature() * outMTP.x;
        float psionsReleased = psionLevel / coreM * outMTP.x;
        //addPhonons(-phononsReleased);
        psionLevel -= psionsReleased;
        //print(psionsReleased);
        outMTP += new Vector3(0, 0/*phononsReleased*/, psionsReleased) + toProcessMTP;

        Vector2Int key = new Vector2Int(StructureI, CapacitorI);
        Vector3 mtp;
        for (int i = 0; i < 7; i++)
        {
            key.y = i;
            mtp = outMTP * demands[i];
            mtps[key] = mtp;
            if(i!=StructureI)
            organs[i].inMTP = mtp;
        }

        inMTP += outMTP * demands[StructureI];
        /*c.inMTP = outMTP * cD;
        w.inMTP = outMTP * wD;
        m.inMTP = outMTP * mD;
        v.inMTP = outMTP * vD;
        p.inMTP = outMTP * pD;
        b.inMTP = outMTP * bD;
        inMTP += outMTP * sD;*/
    }
    public void death(string cause)
    {

    }
    internal IEnumerable<float> Discrete()
    {
        yield return 0;
        while (true)
        {
            //            print(body.temperature);


            float time = 0;
            curBps = bps;
            //v.time = (1 / curBps) / .05f;

            flowIn();
            v.flowIn();
            b.flowIn();
            c.flowIn();
            w.flowIn();
            m.flowIn();
            p.flowIn();

            if (time >= 1 / curBps)
            {
                //print(curBps + " WHAT");
                yield return 0;
            }
            else
            {
                while (time < 1 / curBps)
                {
                    continuousIn();

                    v.continuousIn();
                    b.continuousIn();
                    c.continuousIn();
                    w.continuousIn();
                    m.continuousIn();
                    p.continuousIn();

                    v.absorb();
                    b.absorb();
                    c.absorb();
                    w.absorb();
                    m.absorb();
                    p.absorb();
                    absorb();
                    //Console.WriteLine(v.getTemperature() + " " + b.getTemperature() + " " + c.getTemperature() + " " + w.getTemperature() + " " + m.getTemperature() + " " + p.getTemperature() + " " + getTemperature());

                    v.continuousOut();
                    b.continuousOut();
                    c.continuousOut();
                    w.continuousOut();
                    m.continuousOut();
                    p.continuousOut();
                    continuousOut();
                    time += 0.05f;

                    yield return .05f;
                }

                v.flowOut();
                b.flowOut();
                c.flowOut();
                w.flowOut();
                m.flowOut();
                p.flowOut();

                flowOut();
            }
            yield return .01f;
        }
    }
}