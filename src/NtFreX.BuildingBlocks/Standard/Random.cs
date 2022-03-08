using System.Numerics;

namespace NtFreX.BuildingBlocks.Standard;

public static class Random
{
    private static readonly System.Random random = new ();

    public static Vector3 GetRandomVector(float minimum, float maximum) => 
        new (GetRandomNumber(minimum, maximum), GetRandomNumber(minimum, maximum), GetRandomNumber(minimum, maximum));
    public static Vector3 Noise(float x, float y, float z, float noiseFactor)
        => Noise(new Vector3(x, y, z), noiseFactor);
    public static Vector3 Noise(Vector3 value, float noiseFactor)
        => new (Noise(value.X, noiseFactor), Noise(value.Y, noiseFactor), Noise(value.Z, noiseFactor));
    public static float Noise(float value, float noiseFactor)
        => GetRandomNumber(-noiseFactor, noiseFactor) + value;
    public static int GetRandomNumber(int minimum, int maximum)
        => random.Next(minimum, maximum);
    public static float GetRandomNumber(float minimum, float maximum)
        => (float)GetRandomNumber((double)minimum, (double)maximum);
    public static double GetRandomNumber(double minimum, double maximum)
    {
        return random.NextDouble() * (maximum - minimum) + minimum;
    }
}
