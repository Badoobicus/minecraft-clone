using Godot;
using System;

public class NoiseGenerator
{
    private int[] hash = new int[256];
    private Vector3[] gradients = new Vector3[]
    {
        new Vector3(1, 1, 0),
        new Vector3(-1, 1, 0),
        new Vector3(1, -1, 0),
        new Vector3(-1, -1, 0),
        new Vector3(1, 0, 1),
        new Vector3(-1, 0, 1),
        new Vector3(1, 0, -1),
        new Vector3(-1, 0, -1),
        new Vector3(0, 1, 1),
        new Vector3(0, -1, 1),
        new Vector3(0, 1, -1),
        new Vector3(0, -1, -1),
        new Vector3(1, 1, 0),
        new Vector3(-1, 1, 0),
        new Vector3(0, -1, 1),
        new Vector3(0, -1, -1)
    };

    public NoiseGenerator(int seed)
    {
        for (int i = 0; i < hash.Length; i++)
        {
            hash[i] = i;
        }

        var random = new Random(seed);

        for (int i = 0; i < hash.Length; i++)
        {
            var j = random.Next() & 255;
            var temp = hash[i];
            hash[i] = hash[j];
            hash[j] = temp;
        }
    }

    public float GetNoise(float x, float y)
    {
        int x0 = (int)Mathf.Floor(x);
        int x1 = x0 + 1;
        int y0 = (int)Mathf.Floor(y);
        int y1 = y0 + 1;

        float dx = x - x0;
        float dy = y - y0;

        float tx = this.Fade(dx);
        float ty = this.Fade(dy);

        float n00 = this.DotGradient(x0, y0, x, y);
        float n10 = this.DotGradient(x1, y0, x, y);

        float n01 = this.DotGradient(x0, y1, x, y);
        float n11 = this.DotGradient(x1, y1, x, y);

        return Mathf.Lerp(Mathf.Lerp(n00, n10, tx), Mathf.Lerp(n01, n11, tx), ty);
    }

    public float GetNoise(float x, float y, float z)
    {
        int x0 = (int)Mathf.Floor(x);
        int x1 = x0 + 1;
        int y0 = (int)Mathf.Floor(y);
        int y1 = y0 + 1;
        int z0 = (int)Mathf.Floor(z);
        int z1 = z0 + 1;

        float dx = x - x0;
        float dy = y - y0;
        float dz = z - z0;

        float tx = this.Fade(dx);
        float ty = this.Fade(dy);
        float tz = this.Fade(dz);

        float n000 = this.DotGradient(x0, y0, z0, x, y, z);
        float n100 = this.DotGradient(x1, y0, z0, x, y, z);
        float n010 = this.DotGradient(x0, y1, z0, x, y, z);
        float n110 = this.DotGradient(x1, y1, z0, x, y, z);
        float n001 = this.DotGradient(x0, y0, z1, x, y, z);
        float n101 = this.DotGradient(x1, y0, z1, x, y, z);
        float n011 = this.DotGradient(x0, y1, z1, x, y, z);
        float n111 = this.DotGradient(x1, y1, z1, x, y, z);

        return Mathf.Lerp(
            Mathf.Lerp(Mathf.Lerp(n000, n100, tx), Mathf.Lerp(n010, n110, tx), ty),
            Mathf.Lerp(Mathf.Lerp(n001, n101, tx), Mathf.Lerp(n011, n111, tx), ty),
            tz
        );
    }

    private float DotGradient(int x0, int y0, float x, float y)
    {
        int hx = (int)(x0 * 2654435761);
        int hy = (int)(y0 * 2654435761);
        var gradient = this.gradients[this.hash[(this.hash[hx & 255] + hy) & 255] & 15];

        float dx = x - x0;
        float dy = y - y0;

        return dx * gradient.X + dy * gradient.Y;
    }

    private float DotGradient(int x0, int y0, int z0, float x, float y, float z)
    {
        int hx = (int)(x0 * 2654435761);
        int hy = (int)(y0 * 2654435761);
        int hz = (int)(z0 * 2654435761);
        var gradient = this.gradients[
            this.hash[(this.hash[(this.hash[hx & 255] + hy) & 255] + hz) & 255] & 15
        ];

        float dx = x - x0;
        float dy = y - y0;
        float dz = z - z0;

        return dx * gradient.X + dy * gradient.Y + dz * gradient.Z;
    }

    private float Fade(float t)
    {
        return (t * (t * 6 - 15) + 10) * t * t * t;
    }
}
