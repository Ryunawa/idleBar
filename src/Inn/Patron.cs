using System;

namespace IdleBar.Inn;

public sealed class Patron
{
    public const float Patience = 90f;
    private const float WalkSpeed = 28f;
    private const float ThinkSeconds = 1.6f;

    public Patron(int look, int seat, float x, Drink order, string? regular = null)
    {
        Look = look;
        Regular = regular;
        Seat = seat;
        X = x;
        Order = order;
        Phase = PatronPhase.Entering;
    }

    public int Look { get; }

    public string? Regular { get; }

    public long? Visit { get; init; }

    public string? Guest { get; init; }

    public int Seat { get; }

    public float X { get; private set; }

    public int Heading { get; private set; }

    public PatronPhase Phase { get; private set; }

    public float PhaseTime { get; private set; }

    public Drink Order { get; private set; }

    public float Waited { get; private set; }

    public bool Incoming { get; set; }

    public int Rounds { get; private set; }

    public PreparedDrink? Glass { get; private set; }

    public float DrinkSeconds { get; private set; }

    public int StandX { get; private set; }

    public float MingleSeconds { get; private set; }

    public Patron? Partner { get; set; }

    public int Facing => Phase == PatronPhase.Mingling && Heading == 0 && Partner is { Phase: PatronPhase.Mingling } partner ? Math.Sign(partner.X - X) : 0;

    public bool Seated => Phase is PatronPhase.Thinking or PatronPhase.Waiting or PatronPhase.Drinking;

    public bool HoldsSeat => Seated || Phase == PatronPhase.Entering;

    public bool DoneMingling => Phase == PatronPhase.Mingling && PhaseTime >= MingleSeconds;

    public bool Wants(Drink drink) => Phase == PatronPhase.Waiting && !Incoming && Order == drink;

    public bool OutOfPatience => Visit is null && Phase == PatronPhase.Waiting && !Incoming && Waited >= Patience;

    public bool Awaited => Phase is PatronPhase.Entering or PatronPhase.Thinking or PatronPhase.Waiting && !Incoming;

    public bool Finished => Phase == PatronPhase.Drinking && PhaseTime >= DrinkSeconds;

    public void Update(float delta, int seatX, int doorX)
    {
        PhaseTime += delta;
        switch (Phase)
        {
            case PatronPhase.Entering:
                Walk(seatX, delta);
                if (Math.Abs(X - seatX) < 0.01f)
                {
                    Enter(PatronPhase.Thinking);
                }

                break;
            case PatronPhase.Thinking when PhaseTime >= ThinkSeconds:
                X = seatX;
                Enter(PatronPhase.Waiting);
                break;
            case PatronPhase.Waiting when !Incoming:
                X = seatX;
                Waited += delta;
                break;
            case PatronPhase.Mingling:
                Walk(StandX, delta);
                Heading = Math.Abs(X - StandX) < 0.01f ? 0 : Heading;
                break;
            case PatronPhase.Leaving:
                Walk(doorX, delta);
                if (Math.Abs(X - doorX) < 0.01f)
                {
                    Enter(PatronPhase.Gone);
                }

                break;
            default:
                if (Seated)
                {
                    X = seatX;
                }

                break;
        }
    }

    public void Serve(PreparedDrink drink, float drinkSeconds)
    {
        Glass = drink;
        Incoming = false;
        DrinkSeconds = drinkSeconds;
        Enter(PatronPhase.Drinking);
    }

    public void KeepDrinking() => PhaseTime = 0;

    public void OrderAgain(Drink order)
    {
        Order = order;
        Rounds++;
        Waited = 0;
        Glass = null;
        Enter(PatronPhase.Thinking);
    }

    public void Mingle(int standX, float seconds, Patron? partner)
    {
        StandX = standX;
        MingleSeconds = seconds;
        Partner = partner;
        Enter(PatronPhase.Mingling);
    }

    public void Leave()
    {
        Glass = null;
        Incoming = false;
        Partner = null;
        Enter(PatronPhase.Leaving);
    }

    private void Walk(int target, float delta)
    {
        Heading = Math.Sign(target - X);
        X = Steps.Toward(X, target, WalkSpeed * delta);
    }

    private void Enter(PatronPhase phase)
    {
        Phase = phase;
        PhaseTime = 0;
        Heading = 0;
    }
}
