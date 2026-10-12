using System;

namespace IdleBar.Scene;

public sealed class FlyingCoin
{
    private const float Gravity = 110;
    private const float Bounce = -0.4f;

    private float _speedX;
    private float _speedY;
    private float _time;
    private float _startX = float.NaN;
    private float _startY;

    public FlyingCoin(float x, float y, float speedX, float speedY, float pop, float flight)
    {
        X = x;
        Y = y;
        _speedX = speedX;
        _speedY = speedY;
        Pop = pop;
        Flight = flight;
    }

    public float X { get; private set; }

    public float Y { get; private set; }

    public float Pop { get; }

    public float Flight { get; }

    public bool Landed { get; private set; }

    public void Update(float dt)
    {
        _time += dt;
        if (_time < Pop)
        {
            X += _speedX * dt;
            _speedY += Gravity * dt;
            Y += _speedY * dt;
            if (Y > 22)
            {
                Y = 22;
                _speedY *= Bounce;
            }

            return;
        }

        if (float.IsNaN(_startX))
        {
            _startX = X;
            _startY = Y;
            _speedX = 0;
        }

        float progress = Math.Min(1, (_time - Pop) / Flight);
        float eased = progress * progress;
        float middle = _startX / 2;
        X = (1 - eased) * (1 - eased) * _startX + 2 * (1 - eased) * eased * middle;
        Y = (1 - eased) * (1 - eased) * _startY + eased * eased * 6;
        Landed = progress >= 1;
    }
}
