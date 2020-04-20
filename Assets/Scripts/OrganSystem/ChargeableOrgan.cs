public class ChargeableOrgan : Organ
{
    //static parameter
    public float maxCharge;

    internal float charge;

    public ChargeableOrgan(float startHealth, float powerConsumption, float metabolism, float maxM, float maxCharge) : base(startHealth, powerConsumption, metabolism, maxM)
    {
        this.maxCharge = maxCharge;
    }
    public override void bruise(float dynamic, float core, bool chunk = false)// assume charge is stored per unit mass in dynamic. Implement dynamic charge-based vaporization ease
    {
        if (dynamic > startHealth)
        {
            parent.charge -= charge;
            charge = 0;
        }
        else
        {
            parent.charge -= charge * dynamic / startHealth;
            charge -= charge * dynamic / startHealth;
        }
        base.bruise(dynamic, core, chunk);
    }
}

