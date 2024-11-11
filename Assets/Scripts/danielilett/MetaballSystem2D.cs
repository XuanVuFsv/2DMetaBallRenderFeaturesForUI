using System.Collections.Generic;

public static class MetaballSystem2D
{
    private static List<Metaballs2D> metaballs;

    static MetaballSystem2D()
    {
        metaballs = new List<Metaballs2D>();
    }

    public static void Add(Metaballs2D metaball)
    {
        metaballs.Add(metaball);
    }

    public static List<Metaballs2D> Get()
    {
        return metaballs;
    }

    public static void Remove(Metaballs2D metaball)
    {
        metaballs.Remove(metaball);
    }
}
