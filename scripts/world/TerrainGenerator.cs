using Godot;
using Godot.Collections;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;

public partial class TerrainGenerator
{
    private readonly int seed;
    private readonly ConcurrentDictionary<Vector2I, byte> generatingChunks =
        new ConcurrentDictionary<Vector2I, byte>();
    private readonly ConcurrentBag<Chunk> generatedChunks = new ConcurrentBag<Chunk>();
    public List<Thread> threads = new List<Thread>();

    private readonly int[] heights = new int[] { 0, 64, 128, 175, Chunk.HEIGHT };
    private readonly float[] biases = new float[] { 0.5f, 0.15f, 0.33f, -1, -1 };
    private readonly float[] heightBiases = new float[Chunk.HEIGHT];
    private readonly Vector3[] gradients = new Vector3[]
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
    private readonly ConcurrentBag<RenderingDevice> renderingDevicePool =
        new ConcurrentBag<RenderingDevice>();
    private readonly RDShaderFile shaderFile = GD.Load<RDShaderFile>(
        "res://scripts/world/terrain_generator.glsl"
    );

    public TerrainGenerator(int seed)
    {
        this.seed = seed;

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

    public List<Chunk> TakeGeneratedChunks()
    {
        foreach (var thread in this.threads)
        {
            thread.Join();
        }
        this.threads.Clear();

        var chunks = new List<Chunk>();
        Chunk chunk;
        while (this.generatedChunks.TryTake(out chunk))
        {
            chunks.Add(chunk);
            this.generatingChunks.Remove(chunk.Pos, out _);
        }
        return chunks;
    }

    public bool IsGeneratingChunkAtPos(Vector2I chunkPos)
    {
        return this.generatingChunks.ContainsKey(chunkPos);
    }

    public void GenerateChunkAsync(Chunk chunk)
    {
        this.generatingChunks[chunk.Pos] = 0;
        var thread = new Thread(() =>
        {
            RenderingDevice renderingDevice;
            if (!this.renderingDevicePool.TryTake(out renderingDevice))
            {
                renderingDevice = RenderingServer.CreateLocalRenderingDevice();
            }
            this.GenerateChunk(chunk, renderingDevice);
            this.renderingDevicePool.Add(renderingDevice);

            this.generatedChunks.Add(chunk);
        });
        threads.Add(thread);
        thread.Start();
    }

    private void GenerateChunk(Chunk chunk, RenderingDevice renderingDevice)
    {
        float[] noise = this.RenderNoise(chunk, renderingDevice);

        for (int localX = 0; localX < Chunk.WIDTH; localX++)
        {
            for (int localZ = 0; localZ < Chunk.WIDTH; localZ++)
            {
                var depth = 0;

                for (int localY = Chunk.HEIGHT - 1; localY >= 0; localY--)
                {
                    var block = Blocks.AIR;
                    float value = noise[
                        localY * Chunk.WIDTH * Chunk.WIDTH + localX * Chunk.WIDTH + localZ
                    ];
                    bool containsBlock = value >= 0;

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

    private float[] RenderNoise(Chunk chunk, RenderingDevice renderingDevice)
    {
        var shaderBytecode = this.shaderFile.GetSpirV();
        var shader = renderingDevice.ShaderCreateFromSpirV(shaderBytecode);

        // Noise Buffer  -----------------------------------------------------------------------

        float[] noiseInput = new float[Chunk.HEIGHT * Chunk.WIDTH * Chunk.WIDTH];
        byte[] noiseInputBytes = new byte[noiseInput.Length * sizeof(float)];
        Buffer.BlockCopy(noiseInput, 0, noiseInputBytes, 0, noiseInputBytes.Length);
        Rid noiseBuffer = renderingDevice.StorageBufferCreate(
            (uint)noiseInputBytes.Length,
            noiseInputBytes
        );
        RDUniform noiseUniform = new RDUniform
        {
            UniformType = RenderingDevice.UniformType.StorageBuffer,
            Binding = 0
        };
        noiseUniform.AddId(noiseBuffer);

        // Gradient Buffer -----------------------------------------------------------------------

        float[] gradientInput = new float[this.gradients.Length * 4];
        for (int i = 0; i < this.gradients.Length; i++)
        {
            gradientInput[i * 4] = this.gradients[i].X;
            gradientInput[i * 4 + 1] = this.gradients[i].Y;
            gradientInput[i * 4 + 2] = this.gradients[i].Z;
            gradientInput[i * 4 + 3] = 0;
        }
        byte[] gradientInputBytes = new byte[gradientInput.Length * sizeof(float)];
        Buffer.BlockCopy(gradientInput, 0, gradientInputBytes, 0, gradientInputBytes.Length);
        Rid gradientBuffer = renderingDevice.StorageBufferCreate(
            (uint)gradientInputBytes.Length,
            gradientInputBytes
        );
        RDUniform gradientUniform = new RDUniform
        {
            UniformType = RenderingDevice.UniformType.StorageBuffer,
            Binding = 1
        };
        gradientUniform.AddId(gradientBuffer);

        // Hash Buffer -----------------------------------------------------------------------

        int[] hashInput = new int[256];
        for (int i = 0; i < hashInput.Length; i++)
        {
            hashInput[i] = i;
        }
        var random = new Random(this.seed);
        for (int i = 0; i < hashInput.Length; i++)
        {
            var j = random.Next() & 255;
            var temp = hashInput[i];
            hashInput[i] = hashInput[j];
            hashInput[j] = temp;
        }
        byte[] hashInputBytes = new byte[hashInput.Length * sizeof(int)];
        Buffer.BlockCopy(hashInput, 0, hashInputBytes, 0, hashInputBytes.Length);
        Rid hashBuffer = renderingDevice.StorageBufferCreate(
            (uint)hashInputBytes.Length,
            hashInputBytes
        );
        RDUniform hashUniform = new RDUniform
        {
            UniformType = RenderingDevice.UniformType.StorageBuffer,
            Binding = 2
        };
        hashUniform.AddId(hashBuffer);

        // Chunk Pos Buffer -----------------------------------------------------------------------

        int[] chunkPosInput = new int[] { chunk.X, chunk.Z };
        byte[] chunkPosInputBytes = new byte[chunkPosInput.Length * sizeof(float)];
        Buffer.BlockCopy(chunkPosInput, 0, chunkPosInputBytes, 0, chunkPosInputBytes.Length);
        Rid chunkPosBuffer = renderingDevice.StorageBufferCreate(
            (uint)chunkPosInputBytes.Length,
            chunkPosInputBytes
        );
        RDUniform chunkPosUniform = new RDUniform
        {
            UniformType = RenderingDevice.UniformType.StorageBuffer,
            Binding = 3
        };
        chunkPosUniform.AddId(chunkPosBuffer);

        // Height Biases Buffer -----------------------------------------------------------------------

        byte[] heightBiasesInputBytes = new byte[heightBiases.Length * sizeof(float)];
        Buffer.BlockCopy(heightBiases, 0, heightBiasesInputBytes, 0, heightBiasesInputBytes.Length);
        Rid heightBiasesBuffer = renderingDevice.StorageBufferCreate(
            (uint)heightBiasesInputBytes.Length,
            heightBiasesInputBytes
        );
        RDUniform heightBiasesUniform = new RDUniform
        {
            UniformType = RenderingDevice.UniformType.StorageBuffer,
            Binding = 4
        };
        heightBiasesUniform.AddId(heightBiasesBuffer);

        // Execution -----------------------------------------------------------------------

        Rid uniformSet = renderingDevice.UniformSetCreate(
            new Array<RDUniform>
            {
                noiseUniform,
                gradientUniform,
                hashUniform,
                chunkPosUniform,
                heightBiasesUniform
            },
            shader,
            0
        );

        // Create a compute pipeline
        Rid pipeline = renderingDevice.ComputePipelineCreate(shader);
        long computeList = renderingDevice.ComputeListBegin();
        renderingDevice.ComputeListBindComputePipeline(computeList, pipeline);
        renderingDevice.ComputeListBindUniformSet(computeList, uniformSet, 0);
        renderingDevice.ComputeListDispatch(
            computeList,
            xGroups: 1,
            yGroups: Chunk.HEIGHT,
            zGroups: 1
        );
        renderingDevice.ComputeListEnd();

        // Submit to GPU and wait for sync
        renderingDevice.Submit();
        renderingDevice.Sync();

        // Read back the data from the buffers
        byte[] outputBytes = renderingDevice.BufferGetData(noiseBuffer);
        float[] output = new float[noiseInput.Length];
        Buffer.BlockCopy(outputBytes, 0, output, 0, outputBytes.Length);

        // Cleanup -----------------------------------------------------------------------

        renderingDevice.FreeRid(pipeline);
        renderingDevice.FreeRid(uniformSet);
        renderingDevice.FreeRid(noiseBuffer);
        renderingDevice.FreeRid(gradientBuffer);
        renderingDevice.FreeRid(hashBuffer);
        renderingDevice.FreeRid(chunkPosBuffer);
        renderingDevice.FreeRid(heightBiasesBuffer);
        renderingDevice.FreeRid(shader);

        return output;
    }
}
