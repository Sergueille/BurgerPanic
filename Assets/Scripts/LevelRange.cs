using System.Collections.Generic;
using UnityEngine;
using System;

[Serializable]
public class LevelRange
{
    public Vector2[] ranges;

    public float GetValue(int level)
    {
        int i = level >= ranges.Length ? ranges.Length - 1: level;
        return UnityEngine.Random.Range(ranges[i].x, ranges[i].y);
    }

    public int GetIntValue(int level)
    {
        int i = level >= ranges.Length ? ranges.Length - 1: level;
        return Mathf.RoundToInt(UnityEngine.Random.Range(ranges[i].x, ranges[i].y));
    }
}
