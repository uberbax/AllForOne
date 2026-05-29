using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TimeManager : MonoBehaviour
{
    public static TimeManager instance;

    public float tm;
    public float spd = 1;
    public static float LAST_DT = 0;
    
    private void Awake()
    {
        instance = this; 
        EventManager.SUB("plus_time", (x) => tm += x.num);
    }
    
    private void OnDestroy()
    {
        instance = null;
    }

    public void ResetTime()
    {
        tm = 0;
    }

    public void IncreaseTime(int amount = 1)
    {
        if (ConfigLoader.GetMetaParamValue("use_delta_time") > 0)
        {
            tm += Time.deltaTime * spd;
        }
        else
        {
            tm += amount;            
        }

    }

    public void AddForceTime(float amount)
    {
        tm += amount;
    }
    //algo for increasing time
    private void Update()
    {
        if (ConfigLoader.GetMetaParamValue("sim_time_cont") > 0)
        {
            LAST_DT = Time.deltaTime * spd;
            tm += LAST_DT;
        }
    }

    //
    [ContextMenu("TIME")]
    public DateTime GetCurrentTime()
    {
        return DateTime.UtcNow.AddSeconds(addedSecs);
    }
    public long GetCurrentTimeLong()
    {
        return DateTimeOffset.Now.ToUnixTimeSeconds() + addedSecs;
    }

    public long addedSecs = 0;
    
    public DateTime GetDayEnd()
    {
        return DateTime.UtcNow.EndOfDay();
    }

    public string GetStringTime(int uu)
    {
        var sec = uu % 60;
        var mins = (uu / 60) % 60;
        return mins + "m" + sec + "s";
    }
    //
    public string GetStringTillEndDay()
    {
        var uu = GetTimeTillDayEnd();

        var sec = uu % 60;
        var mins = (uu / 60) % 60;
        var hrs = (uu / 3600);

        return hrs + "h" + mins + "m";
    }
    public string GetStringTillEnd(long end, long dt = 1, bool doSec = false)
    {
        //тут какая-то хуйня для почт и авторевард, надо разобраться
        long uu = 0;
        
        if (dt > 0)
            uu = dt * (end - GetCurrentTime().Ticks) / 10000000;
        else uu = dt * (end - GetCurrentTimeLong()); // / 10000000;

        var sec = uu % 60;
        var mins = (uu / 60) % 60;
        var hrs = (uu / 3600);

        return hrs + "h" + mins + "m" + (doSec ? sec + "s" : "");
    }
    
    public int GetTimeTillDayEnd()
    {
        var t0 = DateTime.UtcNow.EndOfDay() - GetCurrentTime();
        int t1 = (int)t0.TotalSeconds - (int)addedSecs;
        if (t1 < 0) t1 += 24 * 3600;
        return t1;
    }
    
    public TimeSpan GetTimeDifferenceEndDay(DateTime what)
    {
        var ee = GetDayEnd() - what;
        return ee;
    }

    [ContextMenu("TIME TEST")]
    public void TestTime()
    {
        var a1 = GetCurrentTime();
        var a2 = GetTimeDifferenceEndDay(a1);
        Debug.Log(a2.Seconds);
    }

}

public static class DayExt
{
    public static DateTime StartOfDay(this DateTime theDate)
    {
        return theDate.Date;
    }
    public static DateTime EndOfDay(this DateTime theDate)
    {
        return theDate.Date.AddDays(1).AddTicks(-1);
    }    
}