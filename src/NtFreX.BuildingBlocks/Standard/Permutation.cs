namespace NtFreX.BuildingBlocks.Standard;

public static class Permutation
{
    public static bool[][] GetAllPermutations(int length)
    {
        long max = (long)(Math.Pow(2, length) - 1);
        List<bool[]> permutations = new();
        for (long count = 0; count <= max; count++)
        {
            List<bool> data = new();
            for (int j = length - 1; j >= 0; j--)
            {
                data.Add((count >> j & 1) == 0 ? true : false);
            }
            permutations.Add(data.ToArray());
        }
        return permutations.ToArray();
    }
}
