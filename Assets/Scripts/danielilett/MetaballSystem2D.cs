using System.Collections.Generic;
using UnityEngine;

public static class MetaballSystem2D
{
    private static List<Metaballs2D> metaballs = new List<Metaballs2D>();
    private static readonly object lockObject = new object();

    public static void Add(Metaballs2D metaball)
    {
        if (metaball == null)
            return;

        lock (lockObject)
        {
            if (!metaballs.Contains(metaball))
            {
                metaballs.Add(metaball);
            }
        }
    }

    public static void Remove(Metaballs2D metaball)
    {
        if (metaball == null)
            return;

        lock (lockObject)
        {
            metaballs.Remove(metaball);
        }
    }

    public static List<Metaballs2D> Get()
    {
        lock (lockObject)
        {
            return new List<Metaballs2D>(metaballs);
        }
    }

    public static int Count
    {
        get
        {
            lock (lockObject)
            {
                return metaballs.Count;
            }
        }
    }

    public static void Clear()
    {
        lock (lockObject)
        {
            metaballs.Clear();
        }
    }
}