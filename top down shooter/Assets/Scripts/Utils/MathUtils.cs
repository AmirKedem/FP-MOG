using UnityEngine;

public static class MathUtils
{
    public static Vector2 RotateVector(Vector2 v, float radian)
    {
        float _x = v.x * Mathf.Cos(radian) - v.y * Mathf.Sin(radian);
        float _y = v.x * Mathf.Sin(radian) + v.y * Mathf.Cos(radian);
        return new Vector2(_x, _y);
    }
}
