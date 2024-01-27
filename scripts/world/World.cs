using Godot;
using System;
using System.Collections.Generic;

public partial class World : Node
{
    [Export]
    private Player player = null;

    private const int VIEW_DISTANCE = 32;

    private int seed;
    private TerrainGenerator terrainGenerator;
    private ChunkRenderer chunkRenderer;
    private Dictionary<Vector2I, Chunk> chunks = new Dictionary<Vector2I, Chunk>();
    private List<Vector2I> relativeChunkLoadOrder = new List<Vector2I>();
    private int generatingChunkCount = 0;
    private int renderingChunkCount = 0;

    public override void _Ready()
    {
        this.seed = 0;

        this.terrainGenerator = new TerrainGenerator(seed);
        this.chunkRenderer = new ChunkRenderer();
        this.chunks = new Dictionary<Vector2I, Chunk>();
        this.relativeChunkLoadOrder = this.CalculateRelativeChunkLoadOrder();
    }

    private List<Vector2I> CalculateRelativeChunkLoadOrder()
    {
        var result = new List<Vector2I>();

        for (int chunkX = -VIEW_DISTANCE - 1; chunkX <= VIEW_DISTANCE + 1; chunkX++)
        {
            for (int chunkZ = -VIEW_DISTANCE - 1; chunkZ <= VIEW_DISTANCE + 1; chunkZ++)
            {
                result.Add(new Vector2I(chunkX, chunkZ));
            }
        }

        result.Sort((pos1, pos2) => pos1.LengthSquared().CompareTo(pos2.LengthSquared()));

        return result;
    }

    public override void _PhysicsProcess(double delta)
    {
        this.ProcessGeneratedChunks();
        this.ProcessRenderedChunks();

        this.UnloadDistantChunks();
        this.LoadNearbyChunks();
    }

    public Block GetBlockAt(int x, int y, int z)
    {
        if (y < 0 || y > Chunk.HEIGHT)
        {
            return Blocks.AIR;
        }

        var chunkX = (int)Mathf.Floor(x / (float)Chunk.WIDTH);
        var chunkZ = (int)Mathf.Floor(z / (float)Chunk.WIDTH);
        var chunkPos = new Vector2I(chunkX, chunkZ);

        return this.chunks.ContainsKey(chunkPos)
            ? this.chunks[chunkPos].GetBlockAt(
                x - chunkX * Chunk.WIDTH,
                y,
                z - chunkZ * Chunk.WIDTH
            )
            : Blocks.AIR;
    }

    public void SetBlockAt(int x, int y, int z, Block block)
    {
        if (y < 0 || y > Chunk.HEIGHT)
        {
            return;
        }

        var chunkX = (int)Mathf.Floor(x / (float)Chunk.WIDTH);
        var chunkZ = (int)Mathf.Floor(z / (float)Chunk.WIDTH);
        var chunkPos = new Vector2I(chunkX, chunkZ);

        if (this.chunks.ContainsKey(chunkPos))
        {
            this.chunks[chunkPos].SetBlockAt(
                x - chunkX * Chunk.WIDTH,
                y,
                z - chunkZ * Chunk.WIDTH,
                block
            );
        }
    }

    public Vector3 Raytrace(Vector3 origin, Vector3 direction, float length)
    {
        // Feel free to refactor, this method is a mess.

        bool xHit = false;
        Vector3 xHitPos = Vector3.Zero;
        bool yHit = false;
        Vector3 yHitPos = Vector3.Zero;
        bool zHit = false;
        Vector3 zHitPos = Vector3.Zero;

        for (int i = 0; i < length + 1; i++)
        {
            var hitPos =
                origin
                + direction
                    * (
                        (Mathf.Floor(origin.X) + (direction.X > 0 ? i : -i) - origin.X)
                        / direction.X
                    );
            var blockPos = new Vector3I(
                (int)Mathf.Floor(hitPos.X + (direction.X > 0 ? 0.1f : -0.1f)),
                (int)Mathf.Floor(hitPos.Y),
                (int)Mathf.Floor(hitPos.Z)
            );
            var block = this.GetBlockAt(blockPos.X, blockPos.Y, blockPos.Z);
            if (block != Blocks.AIR)
            {
                xHit = true;
                xHitPos = new Vector3(blockPos.X + 0.5f, blockPos.Y + 0.5f, blockPos.Z + 0.5f);
                break;
            }
        }

        for (int i = 0; i < length + 1; i++)
        {
            var hitPos =
                origin
                + direction
                    * (
                        (Mathf.Floor(origin.Y) + (direction.Y > 0 ? i : -i) - origin.Y)
                        / direction.Y
                    );
            var blockPos = new Vector3I(
                (int)Mathf.Floor(hitPos.X),
                (int)Mathf.Floor(hitPos.Y + (direction.Y > 0 ? 0.1f : -0.1f)),
                (int)Mathf.Floor(hitPos.Z)
            );
            var block = this.GetBlockAt(blockPos.X, blockPos.Y, blockPos.Z);
            if (block != Blocks.AIR)
            {
                yHit = true;
                yHitPos = new Vector3(blockPos.X + 0.5f, blockPos.Y + 0.5f, blockPos.Z + 0.5f);
                break;
            }
        }

        for (int i = 0; i < length + 1; i++)
        {
            var hitPos =
                origin
                + direction
                    * (
                        (Mathf.Floor(origin.Z) + (direction.Z > 0 ? i : -i) - origin.Z)
                        / direction.Z
                    );
            var blockPos = new Vector3I(
                (int)Mathf.Floor(hitPos.X),
                (int)Mathf.Floor(hitPos.Y),
                (int)Mathf.Floor(hitPos.Z + (direction.Z > 0 ? 0.1f : -0.1f))
            );
            var block = this.GetBlockAt(blockPos.X, blockPos.Y, blockPos.Z);
            if (block != Blocks.AIR)
            {
                zHit = true;
                zHitPos = new Vector3(blockPos.X + 0.5f, blockPos.Y + 0.5f, blockPos.Z + 0.5f);
                break;
            }
        }

        Vector3 result = Vector3.Down * -100_000;

        if (
            xHit
            && origin.DistanceSquaredTo(xHitPos) < origin.DistanceSquaredTo(result)
            && origin.DistanceSquaredTo(xHitPos) < length * length
        )
        {
            result = xHitPos;
        }

        if (
            yHit
            && origin.DistanceSquaredTo(yHitPos) < origin.DistanceSquaredTo(result)
            && origin.DistanceSquaredTo(yHitPos) < length * length
        )
        {
            result = yHitPos;
        }

        if (
            zHit
            && origin.DistanceSquaredTo(zHitPos) < origin.DistanceSquaredTo(result)
            && origin.DistanceSquaredTo(zHitPos) < length * length
        )
        {
            result = zHitPos;
        }

        return result;
    }

    private void ProcessGeneratedChunks()
    {
        var chunks = this.terrainGenerator.TakeGeneratedChunks();
        foreach (var chunk in chunks)
        {
            this.chunks[chunk.Pos] = chunk;
            this.UpdateChunkNeighbors(chunk);
        }
        this.generatingChunkCount -= chunks.Count;
    }

    private void ProcessRenderedChunks()
    {
        var chunks = this.chunkRenderer.TakeRenderedChunks();
        this.renderingChunkCount -= chunks.Count;
    }

    private void LoadNearbyChunks()
    {
        foreach (var chunkPos in this.FindChunkPositionsToLoad())
        {
            if (this.generatingChunkCount + this.renderingChunkCount < 8)
            {
                if (
                    !this.chunks.ContainsKey(chunkPos)
                    && !this.terrainGenerator.IsGeneratingChunkAtPos(chunkPos)
                )
                {
                    var chunk = new Chunk(chunkPos);
                    chunk.MeshHolder = new Node3D
                    {
                        Name = $"Chunk ({chunkPos.X}, {chunkPos.Y})",
                        Position = new Vector3(chunk.X * Chunk.WIDTH, 0, chunk.Z * Chunk.WIDTH)
                    };

                    for (int i = 0; i < Chunk.SUBCHUNKS; i++)
                    {
                        var meshInstance = new MeshInstance3D { Name = $"Subchunk ({i})" };
                        chunk.MeshHolder.AddChild(meshInstance);
                        chunk.GetSubchunk(i).MeshInstance = meshInstance;
                    }

                    this.AddChild(chunk.MeshHolder);

                    this.terrainGenerator.GenerateChunkAsync(chunk);
                    this.generatingChunkCount++;
                }

                if (
                    this.chunks.ContainsKey(chunkPos)
                    && !this.chunkRenderer.IsRenderingChunkAtPos(chunkPos)
                )
                {
                    var chunk = this.chunks[chunkPos];
                    if (
                        chunk.Dirty
                        && chunk.NorthNeighbor != null
                        && chunk.EastNeighbor != null
                        && chunk.SouthNeighbor != null
                        && chunk.WestNeighbor != null
                    )
                    {
                        this.chunkRenderer.RenderChunkAsync(chunk);
                        this.renderingChunkCount++;
                    }
                }
            }
        }
    }

    private void UnloadDistantChunks()
    {
        foreach (var chunkPos in this.FindChunkPositionsToUnload())
        {
            if (!this.terrainGenerator.IsGeneratingChunkAtPos(chunkPos))
            {
                var chunk = this.chunks[chunkPos];
                this.chunks.Remove(chunkPos);
                chunk.Destroy();
            }
        }
    }

    private List<Vector2I> FindChunkPositionsToLoad()
    {
        var result = new List<Vector2I>();

        var playerChunkPos = new Vector2I(
            (int)Mathf.Floor(player.Position.X / (float)Chunk.WIDTH),
            (int)Mathf.Floor(player.Position.Z / (float)Chunk.WIDTH)
        );

        foreach (var chunkPos in this.relativeChunkLoadOrder)
        {
            result.Add(chunkPos + playerChunkPos);
        }

        return result;
    }

    private List<Vector2I> FindChunkPositionsToUnload()
    {
        var result = new HashSet<Vector2I>(this.chunks.Keys);
        foreach (var chunkPos in this.FindChunkPositionsToLoad())
        {
            result.Remove(chunkPos);
        }
        return new List<Vector2I>(result);
    }

    private void UpdateChunkNeighbors(Chunk chunk)
    {
        var chunkPos = chunk.Pos;

        lock (this.chunks)
        {
            if (this.chunks.ContainsKey(chunkPos + Vector2I.Up))
            {
                chunk.NorthNeighbor = this.chunks[chunkPos + Vector2I.Up];
                chunk.NorthNeighbor.SouthNeighbor = chunk;
            }
            if (this.chunks.ContainsKey(chunkPos + Vector2I.Right))
            {
                chunk.EastNeighbor = this.chunks[chunkPos + Vector2I.Right];
                chunk.EastNeighbor.WestNeighbor = chunk;
            }
            if (this.chunks.ContainsKey(chunkPos + Vector2I.Down))
            {
                chunk.SouthNeighbor = this.chunks[chunkPos + Vector2I.Down];
                chunk.SouthNeighbor.NorthNeighbor = chunk;
            }
            if (this.chunks.ContainsKey(chunkPos + Vector2I.Left))
            {
                chunk.WestNeighbor = this.chunks[chunkPos + Vector2I.Left];
                chunk.WestNeighbor.EastNeighbor = chunk;
            }
        }
    }
}
