using UnityEngine;

public class Ticker : MonoBehaviour
{
    public static float tickTimeSlow = 1f; // Time interval for each tick in seconds
    private float _tickerTimeSlow;

    public static float tickTimeFast = 0.2f; // Time interval for each tick in seconds
    private float _tickerTimeFast;

    public delegate void SlowTickAction();
    public static event SlowTickAction OnSlowTick;

    public delegate void FastTickAction();
    public static event FastTickAction OnFastTick;

    // Update is called once per frame
    void Update()
    {
        _tickerTimeSlow += Time.deltaTime;
        _tickerTimeFast += Time.deltaTime;

        if (_tickerTimeSlow >= tickTimeSlow)
        {
            OnSlowTick?.Invoke();
            _tickerTimeSlow = 0f;
        }

        if (_tickerTimeFast >= tickTimeFast)
        {
            OnFastTick?.Invoke();
            _tickerTimeFast = 0f;
        }
    }
}
