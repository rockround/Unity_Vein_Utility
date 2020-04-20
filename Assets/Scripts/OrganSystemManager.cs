using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading.Tasks;
using System.Threading;
using UnityEngine.UI;

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
    public Slider[] cores, powers, temps, charges;
    public Button PauseButton, PlayButton;
    public InputField DrainRate, MaxCharge, FatBreakdown, FatGrowth, Homeostasis, BaseBps, BetaRate, MaxBoostCount, MaxM, StartHealth, Metabolism, PowerConsumption, SimRate;
    public Text HeartRate;
    public Slider SimRateSlider;
    public void Start()
    {
        //create and start a new thread in the load event.
        //cores = new Slider[] { WriterCore, CapacitorCore, MotorCore, StructureCore, BetaCore, PumpCore, VisionCore };
        //temps = new Slider[] { WriterTemp, CapacitorTemp, MotorTemp, StructureTemp, BetaTemp, PumpTemp, VisionTemp };
        //powers = new Slider[] { WriterPower, CapacitorPower, MotorPower, StructurePower, BetaPower, PumpPower, VisionPower };
        //charges = new Slider[] { WriterCharge, CapacitorCharge, MotorCharge };
        //passing it a method to be run on the new thread.
    }
    void updateStats()
    {
        HeartRate.text = s.curBps + "";
        for (int i = 0; i < s.organs.Length; i++)
        {
            if (s.organs[i].coreM + s.organs[i].dynamicM > 0)
            {
                cores[i].value = (int)(100 * s.organs[i].coreM / s.organs[i].startHealth);
                powers[i].value = (int)(100 * s.organs[i].currentPower);
                temps[i].value = Mathf.Min(100, (int)(s.organs[i].getTemperature()));
                if (i <= Organ.lastChargeableOrgan)
                {
                    charges[i].value = (int)(100 * ((ChargeableOrgan)s.organs[i]).charge / ((ChargeableOrgan)s.organs[i]).maxCharge);
                }
            }
            else
            {
                cores[i].value = 0;
                powers[i].value = 0;
                temps[i].value = 0;
                if (i <= Organ.lastChargeableOrgan)
                {
                    charges[i].value = 0;
                }

            }

        }


    }
    public IEnumerator mainLoop(float simRate)
    {
        //you need to use Invoke because the new thread can't access the UI elements directly
        s = new Structure(startHealths, metabolisms, powerConsumptions, maxMs, maxCharges, maxBoostCount, betaRate, drainRate, fatGrowth, fatBreakdown, baseBps, homeostasis);
        s.startSimulation();
        foreach (var number in s.Discrete())
        {
            while (pause)
            {
                //Thread.Sleep(50);
                yield return new WaitForSecondsRealtime(.05f);
            }
            print(s.coreM + " " + s.dynamicM);
            if (stop)
                break;
          
            Invoke("updateStats",0);
            yield return new WaitForSecondsRealtime(number / simRate);
            //Thread.Sleep((int)(number * 1000 * 1 / simRate));

            //To keep stuff from jamming up
        }

    }
    public void killMainLoop()
    {
        //stop the thread.
        pause = false;
        stop = true;
        PauseButton.enabled = PlayButton.enabled = false;
        for (int i = 0; i < 7; i++)
        {
            cores[i].value = 0;
            powers[i].value = 0;
            temps[i].value = 0;
            if (i <= Organ.lastChargeableOrgan)
            {
                charges[i].value = 0;
            }
        }
    }

    public void startLoop()
    {
        //if (t != null)
        {
            pause = false;
            stop = true;
            StopCoroutine("mainLoop");
            //t.Join();
        }
        pause = false;
        stop = false;

        //////////////////////////////t = new Thread(() => mainLoop((float)SimRate.value));
        //t.Start();
        StartCoroutine(mainLoop(SimRateSlider.value));
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