using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading.Tasks;
using System.Threading;
using UnityEngine.UI;
using Unity.Collections;
using Unity.Jobs;
using System;

public class OrganSystemManager : MonoBehaviour
{
    Thread t;
    bool stop, pause;
    public Structure s;
    int lastIdx = -1;
    float[] startHealths = { 8, 3, 3, 200, 6, 3, 3 };
    float[] metabolisms = { .1f, .1f, .1f, .1f, .1f, .1f, .1f };
    float[] maxMs = { 7, 1, 1, 1, 1, 1, 1 };
    float[] powerConsumptions = { 2, .01f, 3, .01f, .01f, .01f, .01f };
    float[] maxCharges = { 100, 100, 100 };
    int maxBoostCount = 2;
    float betaRate = 1.7f;
    float drainRate = 1;
    float fatGrowth = 1;
    float fatBreakdown = 1;
    float baseBps = 1;
    float homeostasis = 1;
    public Slider[] coreSliders, powerSliders, tempSliders, chargeSliders;
    public Button PauseButton, PlayButton;
    public InputField DrainRate, MaxCharge, FatBreakdown, FatGrowth, Homeostasis, BaseBps, BetaRate, MaxBoostCount, MaxM, StartHealth, Metabolism, PowerConsumption, SimRate;
    public Text HeartRate;
    public Slider SimRateSlider;
    NativeArray<float> coreMs;
    NativeArray<float> currentPowers;
    NativeArray<float> dynamicMs;
    NativeArray<float> temperatures;
    NativeArray<float> charges;
    NativeArray<float> otherData;
    NativeHashMap<Vector2Int, Vector3> mtps;
    int totalOrgans = 7;
    public OrganObject[] objects;
    public Dictionary<Vector2Int, Vein> pipe2Vein;
    public Vein[] allVeins;
    public void Awake()
    {
        coreMs = new NativeArray<float>(7, Allocator.Persistent);
        currentPowers = new NativeArray<float>(7, Allocator.Persistent);
        dynamicMs = new NativeArray<float>(7, Allocator.Persistent);
        temperatures = new NativeArray<float>(7, Allocator.Persistent);
        charges = new NativeArray<float>(3, Allocator.Persistent);
        otherData = new NativeArray<float>(2, Allocator.Persistent);
        mtps = new NativeHashMap<Vector2Int, Vector3>(13, Allocator.Persistent);
        pipe2Vein = new Dictionary<Vector2Int, Vein>();
        for(int i = 0; i < 7; i++)
        {
            Vector2Int key1 = new Vector2Int(i, Organ.StructureI);
            Vector2Int key2 = new Vector2Int(Organ.StructureI, i);
            mtps.Add(key1,Vector3.zero);
            if (i != Organ.StructureI)
            {
                mtps.Add(key2, Vector3.zero);
            }
            objects[i].startHealth = startHealths[i];
        }
        foreach(Vein v in allVeins)
        {
            GameObject from = v.from;
            GameObject to = v.to;
            int fromIdx = (int)from.GetComponent<OrganObject>().organType;
            int toIdx = (int)to.GetComponent<OrganObject>().organType;
            pipe2Vein.Add(new Vector2Int(fromIdx, toIdx), v);
        }


    }
    public void Start()
    {
        foreach (Vector2Int fromTo in pipe2Vein.Keys)
        {
            objects[fromTo.x].outMTPs.Add(pipe2Vein[fromTo], Vector3.zero);
        }
        //create and start a new thread in the load event.
        //cores = new Slider[] { WriterCore, CapacitorCore, MotorCore, StructureCore, BetaCore, PumpCore, VisionCore };
        //temps = new Slider[] { WriterTemp, CapacitorTemp, MotorTemp, StructureTemp, BetaTemp, PumpTemp, VisionTemp };
        //powers = new Slider[] { WriterPower, CapacitorPower, MotorPower, StructurePower, BetaPower, PumpPower, VisionPower };
        //charges = new Slider[] { WriterCharge, CapacitorCharge, MotorCharge };
        //passing it a method to be run on the new thread.
    }
    void updateStats()
    {
        HeartRate.text = otherData[0] + "";
        //set mtps
        if(otherData[1] == .01f)
        {
            //Adds to each organ object the mtp associated with the vein connection for trigger load
            foreach(Vector2Int fromTo in pipe2Vein.Keys)
            {
                objects[fromTo.x].outMTPs[pipe2Vein[fromTo]]= mtps[fromTo];
            }
        }
        for (int i = 0; i < totalOrgans; i++)
        {
            if (coreMs[i] + dynamicMs[i] > 0)
            {
                coreSliders[i].value = (int)(100 * coreMs[i] / startHealths[i]);
                powerSliders[i].value = (int)(100 * currentPowers[i]);
                tempSliders[i].value = Mathf.Min(100, (int)(temperatures[i]));
                if (i <= Organ.lastChargeableOrgan)
                {
                    chargeSliders[i].value = (int)(100 * charges[i] / maxCharges[i]);
                }
                objects[i].coreM = coreMs[i];
                objects[i].dynamicM = dynamicMs[i];
                objects[i].temperature = temperatures[i];
                //if pulse done
                if (otherData[1] == .01f)
                {
                    objects[i].fire();
                }
            }
            else
            {
                coreSliders[i].value = 0;
                powerSliders[i].value = 0;
                tempSliders[i].value = 0;
                if (i <= Organ.lastChargeableOrgan)
                {
                    chargeSliders[i].value = 0;
                }

            }

        }


    }
    void Update()
    {
        if (t != null)
            updateStats();
    }
    public void mainLoop(float simRate)
    {
        //you need to use Invoke because the new thread can't access the UI elements directly
        s = new Structure(startHealths, metabolisms, powerConsumptions, maxMs, maxCharges, maxBoostCount, betaRate, drainRate, fatGrowth, fatBreakdown, baseBps, homeostasis);
        s.mtps = mtps;
        s.startSimulation();
        foreach (var number in s.Discrete())
        {
            while (pause)
            {
                Thread.Sleep(50);
                //yield return new WaitForSecondsRealtime(.05f);
            }
            //print(s.coreM + " " + s.dynamicM);
            otherData[0] = s.curBps;

            for (int i = 0; i < 7; i++)
            {
                coreMs[i] = s.organs[i].coreM;
                dynamicMs[i] = s.organs[i].dynamicM;
                currentPowers[i] = s.organs[i].currentPower;
                temperatures[i] = s.organs[i].getTemperature();
                if (i <= Organ.lastChargeableOrgan)
                {
                    charges[i] = ((ChargeableOrgan)s.organs[i]).charge;

                }
            }
            if (stop)
                break;

            //If was a blood pump cycle
            //if (number < .05f)
            //    otherData[1] = 1;
            //Invoke("updateStats",0);
            // yield return new WaitForSecondsRealtime(number / simRate);
            Thread.Sleep((int)(number * 1000 * 1 / simRate));
            otherData[1] = number;

            //if (number < .05f)
            //    otherData[1] = 0;

            //To keep stuff from jamming up
        }

    }
    public void OnApplicationQuit()
    {
        coreMs.Dispose();
        dynamicMs.Dispose();
        temperatures.Dispose();
        charges.Dispose();
        currentPowers.Dispose();
        otherData.Dispose();
        mtps.Dispose();
        t?.Join();
    }
    public void killMainLoop()
    {
        //stop the thread.
        pause = false;
        stop = true;
        PauseButton.enabled = PlayButton.enabled = false;
        for (int i = 0; i < 7; i++)
        {
            coreSliders[i].value = 0;
            powerSliders[i].value = 0;
            tempSliders[i].value = 0;
            if (i <= Organ.lastChargeableOrgan)
            {
                chargeSliders[i].value = 0;
            }
        }
    }

    public void startLoop()
    {
        if (t != null)
        {
            pause = false;
            stop = true;
            //StopCoroutine("mainLoop");
            t.Join();
        }
        pause = false;
        stop = false;

        t = new Thread(() => mainLoop(SimRateSlider.value));
        t.Start();
        //StartCoroutine(mainLoop(SimRateSlider.value));
        PauseButton.enabled = true;
        PlayButton.enabled = false;
    }

    public void PauseButton_Click()
    {
        pause = true;
        PauseButton.enabled = false;
        PlayButton.enabled = true;
    }

    public void PlayButton_Click()
    {
        pause = false;
        PauseButton.enabled = true;
        PlayButton.enabled = false;
    }

    public void OrganSelection(int idx)
    {

        if (lastIdx != -1)
        {
            startHealths[lastIdx] = float.Parse(StartHealth.text);
            maxMs[lastIdx] = float.Parse(MaxM.text);
            powerConsumptions[lastIdx] = float.Parse(PowerConsumption.text);
            metabolisms[lastIdx] = float.Parse(Metabolism.text);
        }
        StartHealth.text = startHealths[idx] + "";
        MaxM.text = maxMs[idx] + "";
        PowerConsumption.text = powerConsumptions[idx] + "";
        Metabolism.text = metabolisms[idx] + "";

        if (idx == Organ.MotorI || idx == Organ.WriterI || idx == Organ.CapacitorI)
        {
            MaxCharge.enabled = true;
            MaxCharge.text = maxCharges[idx] + "";
        }
        else
        {
            if (lastIdx == Organ.MotorI || lastIdx == Organ.WriterI || lastIdx == Organ.CapacitorI)
            {
                maxCharges[lastIdx] = int.Parse(MaxCharge.text);
            }
            MaxCharge.enabled = false;
            MaxCharge.text = "";
        }

        if (idx == Organ.StructureI)
        {
            FatBreakdown.text = fatBreakdown + "";
            FatGrowth.text = fatGrowth + "";
            Homeostasis.text = homeostasis + "";
            BaseBps.text = baseBps + "";
            FatBreakdown.enabled = true;
            FatGrowth.enabled = true;
            Homeostasis.enabled = true;
            BaseBps.enabled = true;
        }
        else
        {
            if (lastIdx == Organ.StructureI)
            {
                fatBreakdown = float.Parse(FatBreakdown.text);
                fatGrowth = float.Parse(FatGrowth.text);
                homeostasis = float.Parse(Homeostasis.text);
                baseBps = float.Parse(BaseBps.text);
            }
            FatBreakdown.text = "";
            FatGrowth.text = "";
            Homeostasis.text = "";
            BaseBps.text = "";
            FatBreakdown.enabled = false;
            FatGrowth.enabled = false;
            Homeostasis.enabled = false;
            BaseBps.enabled = false;
        }

        if (idx == Organ.BetaI)
        {
            BetaRate.text = betaRate + "";
            BetaRate.enabled = true;
        }
        else
        {
            if (lastIdx == Organ.BetaI)
            {
                betaRate = float.Parse(BetaRate.text);
            }
            BetaRate.enabled = false;
            BetaRate.text = "";
        }

        if (idx == Organ.MotorI)
        {
            MaxBoostCount.text = maxBoostCount + "";
            MaxBoostCount.enabled = true;
        }
        else
        {
            if (lastIdx == Organ.MotorI)
            {
                maxBoostCount = int.Parse(MaxBoostCount.text);
            }
            MaxBoostCount.enabled = false;
            MaxBoostCount.text = "";
        }

        if (idx == Organ.PumpI)
        {
            DrainRate.text = drainRate + "";
            DrainRate.enabled = true;
        }
        else
        {
            if (lastIdx == Organ.PumpI)
            {
                drainRate = float.Parse(DrainRate.text);
            }
            DrainRate.enabled = false;
            DrainRate.text = "";
        }

        lastIdx = idx;

    }
    public void updateSimRateSliderValue(float value)
    {
        SimRate.text = value + "";
    }
    public void updateSimRateTextFieldValue(string text)
    {
        SimRateSlider.value = float.Parse(text);
    }


}