namespace IdleBar.Pixel;

public readonly record struct Patrol(float X, bool Moving, bool Leftward, float Pause)
{
    public static Patrol At(int from, int to, float time, float speed, float pauseSeconds)
    {
        float travel = (to - from) / speed;
        float cycle = 2 * (travel + pauseSeconds);
        float moment = (time % cycle + cycle) % cycle;
        return moment switch
        {
            _ when moment < travel => new Patrol(from + moment * speed, true, false, 0),
            _ when moment < travel + pauseSeconds => new Patrol(to, false, true, moment - travel),
            _ when moment < 2 * travel + pauseSeconds => new Patrol(to - (moment - travel - pauseSeconds) * speed, true, true, 0),
            _ => new Patrol(from, false, false, moment - 2 * travel - pauseSeconds),
        };
    }
}