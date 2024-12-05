using System.Text;
using UnityEngine;

public static class StringExtensions 
{
    private static StringBuilder sharedBuilder = new StringBuilder(32);
    private static readonly string[] cachedNumbers = new string[1000];
    private static readonly string[] cachedDecimals = new string[100];
    
    // 초기화
    static StringExtensions()
    {
        // 자주 사용되는 정수 캐싱
        for (int i = 0; i < cachedNumbers.Length; i++)
        {
            cachedNumbers[i] = i.ToString();
        }
        
        // 자주 사용되는 소수점 한자리 숫자 캐싱 (0.0 ~ 9.9)
        for (int i = 0; i < cachedDecimals.Length; i++)
        {
            cachedDecimals[i] = (i / 10f).ToString("F1");
        }
    }

    // 정수를 빠르게 문자열로 변환
    public static string ToFastString(this int value)
    {
        if (value >= 0 && value < cachedNumbers.Length)
        {
            return cachedNumbers[value];
        }
        
        return value.ToString();
    }

    // Vector3를 효율적으로 문자열로 변환
    public static string ToFastString(this Vector3 vector)
    {
        lock (sharedBuilder)
        {
            sharedBuilder.Clear();
            sharedBuilder.Append('(');
            sharedBuilder.Append(vector.x.ToString("F2"));
            sharedBuilder.Append(", ");
            sharedBuilder.Append(vector.y.ToString("F2"));
            sharedBuilder.Append(", ");
            sharedBuilder.Append(vector.z.ToString("F2"));
            sharedBuilder.Append(')');
            return sharedBuilder.ToString();
        }
    }

    // 소수점 한자리 실수를 빠르게 변환
    public static string ToFastString(this float value, int decimals = 1)
    {
        if (decimals == 1 && value >= 0f && value < 10f)
        {
            int index = Mathf.RoundToInt(value * 10f);
            if (index < cachedDecimals.Length)
            {
                return cachedDecimals[index];
            }
        }
        
        return value.ToString($"F{decimals}");
    }

    // 시간을 mm:ss 형식으로 변환
    public static string ToTimeString(this float seconds)
    {
        int minutes = Mathf.FloorToInt(seconds / 60f);
        int secs = Mathf.FloorToInt(seconds % 60f);
        
        lock (sharedBuilder)
        {
            sharedBuilder.Clear();
            if (minutes < 10) sharedBuilder.Append('0');
            sharedBuilder.Append(minutes);
            sharedBuilder.Append(':');
            if (secs < 10) sharedBuilder.Append('0');
            sharedBuilder.Append(secs);
            return sharedBuilder.ToString();
        }
    }
}