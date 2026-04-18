using System;
using UnityEngine;

// 골드(double) 메모리 변조 방어용
[Serializable]
public struct SafeDouble
{
    private long obscuredValue;
    private long key;

    public SafeDouble(double value)
    {
        key = UnityEngine.Random.Range(100000, 999999);
        obscuredValue = BitConverter.DoubleToInt64Bits(value) ^ key;
    }

    public double Value
    {
        get { return BitConverter.Int64BitsToDouble(obscuredValue ^ key); }
    }

    public static implicit operator SafeDouble(double value) => new SafeDouble(value);
    public static implicit operator double(SafeDouble safe) => safe.Value;
}

// 공격력/스탯(float) 메모리 변조 방어용
[Serializable]
public struct SafeFloat
{
    private int obscuredValue;
    private int key;

    public SafeFloat(float value)
    {
        key = UnityEngine.Random.Range(100000, 999999);
        obscuredValue = BitConverter.SingleToInt32Bits(value) ^ key;
    }

    public float Value
    {
        get { return BitConverter.Int32BitsToSingle(obscuredValue ^ key); }
    }

    public static implicit operator SafeFloat(float value) => new SafeFloat(value);
    public static implicit operator float(SafeFloat safe) => safe.Value;
}
