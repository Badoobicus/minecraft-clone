using Godot;

public class TerrainGeneratorOld
{
    private NoiseGenerator noiseGenerator;
    private int[] heights = new int[] { 0, 64, 128, 175, 384 };
    private float[] biases = new float[] { 0.5f, 0.15f, 0.33f, -1, -1 };
    private float[] heightBiases = new float[384];

    public TerrainGeneratorOld(int seed)
    {
        this.noiseGenerator = new NoiseGenerator(seed);

        for (int y = 0; y < heightBiases.Length; y++)
        {
            int i1 = 0;
            while (heights[i1] <= y)
            {
                i1++;
            }
            int i0 = i1 - 1;
            float t = (float)(y - heights[i0]) / (float)(heights[i1] - heights[i0]);
            this.heightBiases[y] = Mathf.Lerp(
                biases[i0],
                biases[i1],
                (t * (t * 6 - 15) + 10) * t * t * t
            );
        }
    }

    public void GenerateChunk(Chunk chunk)
    {
        for (int localX = 0; localX < Chunk.WIDTH; localX++)
        {
            for (int localZ = 0; localZ < Chunk.WIDTH; localZ++)
            {
                var x = chunk.X * Chunk.WIDTH + localX;
                var z = chunk.Z * Chunk.WIDTH + localZ;

                // var height = this.noiseGenerator.GetNoise(x / 128f, z / 128f) * 16;
                var depth = 0;

                for (int localY = Chunk.HEIGHT - 1; localY >= 0; localY--)
                {
                    var y = localY;
                    bool containsBlock;

                    // Shortcut evaluation when the height-based weighting makes it impossible
                    // for the block to be anything but air.
                    if (this.heightBiases[y] <= -0.99f)
                    {
                        containsBlock = false;
                    }
                    else if (this.heightBiases[y] >= 0.99f)
                    {
                        containsBlock = true;
                    }
                    else
                    {
                        var noise =
                            (
                                this.noiseGenerator.GetNoise(x / 128f, y / 128f, z / 128f) * 8
                                + this.noiseGenerator.GetNoise(x / 64f, y / 64f, z / 64f) * 4
                                + this.noiseGenerator.GetNoise(x / 32f, y / 32f, z / 32f) * 2
                                + this.noiseGenerator.GetNoise(x / 16f, y / 16f, z / 16f) * 1
                            ) / 15;
                        noise += this.heightBiases[y];
                        containsBlock = noise >= 0;
                    }

                    var block = Blocks.AIR;

                    if (containsBlock || depth > 0)
                    {
                        if (containsBlock)
                        {
                            if (depth == 0)
                            {
                                block = Blocks.GRASS;
                            }
                            else if (depth <= 2)
                            {
                                block = Blocks.DIRT;
                            }
                            else
                            {
                                block = Blocks.STONE;
                            }
                        }

                        depth++;
                    }

                    chunk.SetBlockAt(localX, localY, localZ, block);
                }
            }
        }
    }
}
