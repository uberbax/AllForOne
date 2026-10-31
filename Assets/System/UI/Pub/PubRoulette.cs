using System;

/// <summary>Visual steps only; the transaction's selected reward determines the final card.</summary>
public static class PubRoulette
{
    public static int[] Path(int count, int winner, int start)
    {
        if (count < 1 || winner < 0 || winner >= count) return Array.Empty<int>();
        start = ((start % count) + count) % count;
        int steps = count * 4 + (winner - start + count) % count;
        var path = new int[steps + 1];
        for (int i = 0; i < path.Length; i++) path[i] = (start + i) % count;
        return path;
    }
}
