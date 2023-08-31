using System.Collections.Generic;
using UnityEngine;

public static class Util
{
    public static Vector2 RandomVectorInCircle(float radius) // Non uniform distribution :(
    {
        float theta = Random.Range(0, 2 * Mathf.PI);
        float r = Random.Range(0, radius);

        return new Vector2(Mathf.Cos(theta) * r, Mathf.Sin(theta) * r);
    }

    public static T GetComponentInParentsRecursive<T>(GameObject obj) where T : Component
    {
        T comp = obj.GetComponent<T>();

        if (comp != null) return comp;

        if (obj.transform.parent != null)
            return GetComponentInParentsRecursive<T>(obj.transform.parent.gameObject);

        return null;
    }

    /// <summary>
    /// Toggles the specified amount of random flags on the enum
    /// Assumes that the enum has 'none = 0' and 'maxValue' flags
    /// </summary>
    public static int GetRandomFlags<T>(int flagCount) where T : System.Enum
    {   
        int maxVal = typeof(T).GetEnumValues().Length - 2; // Ignore none

        if (flagCount > maxVal) throw new System.Exception("Too much flags!!");

        int sum = 0;

        for (int i = 0; i < flagCount; i++)
        {
            int rand = UnityEngine.Random.Range(0, maxVal);
            int pos = 1 << rand;

            if ((sum & pos) == 0) 
                sum |= pos;
            else
                i--;
        }

        return sum;
    }

    public static bool IsUpsideDown(float angle, float maxAngle)
    {
        return Mathf.Abs(Mathf.DeltaAngle(angle, 0)) > maxAngle;
    }

    public static void RemoveChildren(Transform t)
    {
        foreach (Transform child in t)
        {
            Object.Destroy(child);
        }
    }
}
