using Godot;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;

public class ChunkRenderer
{
    private readonly TextureAtlas textureAtlas;
    private readonly Material material;
    private readonly ConcurrentDictionary<Vector2I, byte> renderingChunks =
        new ConcurrentDictionary<Vector2I, byte>();
    private readonly ConcurrentBag<Chunk> renderedChunks = new ConcurrentBag<Chunk>();

    public List<Thread> threads = new List<Thread>();

    public ChunkRenderer()
    {
        this.textureAtlas = new TextureAtlas();

        this.material = ResourceLoader.Load<Material>("materials/chunk.tres");
        this.material.Set("albedo_texture", textureAtlas.Texture);
    }

    public List<Chunk> TakeRenderedChunks()
    {
        foreach (var thread in this.threads)
        {
            thread.Join();
        }
        this.threads.Clear();

        var chunks = new List<Chunk>();
        Chunk chunk;
        while (this.renderedChunks.TryTake(out chunk))
        {
            chunks.Add(chunk);
            this.renderingChunks.Remove(chunk.Pos, out _);
        }
        return chunks;
    }

    public bool IsRenderingChunkAtPos(Vector2I chunkPos)
    {
        return this.renderingChunks.ContainsKey(chunkPos);
    }

    public void RenderChunkAsync(Chunk chunk)
    {
        this.renderingChunks[chunk.Pos] = 0;
        var thread = new Thread(() =>
        {
            this.RenderChunk(chunk);
            this.renderedChunks.Add(chunk);
        });
        threads.Add(thread);
        thread.Start();
    }

    private void RenderChunk(Chunk chunk)
    {
        for (int i = 0; i < Chunk.SUBCHUNKS; i++)
        {
            var subchunk = chunk.GetSubchunk(i);
            if (subchunk.Dirty)
            {
                this.RenderSubchunk(chunk, i);
                subchunk.Dirty = false;
            }
        }
    }

    private void RenderSubchunk(Chunk chunk, int subchunk)
    {
        var surfaceArray = new Godot.Collections.Array();
        surfaceArray.Resize((int)Mesh.ArrayType.Max);

        var verts = new List<Vector3>();
        var normals = new List<Vector3>();
        var uvs = new List<Vector2>();

        for (int x = 0; x < Chunk.WIDTH; x++)
        {
            for (int z = 0; z < Chunk.WIDTH; z++)
            {
                for (
                    int y = subchunk * Chunk.SUBCHUNK_HEIGHT;
                    y < (subchunk + 1) * Chunk.SUBCHUNK_HEIGHT;
                    y++
                )
                {
                    var block = chunk.GetBlockAt(x, y, z);

                    if (block != Blocks.AIR)
                    {
                        if (this.GetBlockAt(chunk, x, y, z - 1) == Blocks.AIR)
                        {
                            verts.Add(new Vector3(x + 1, y, z));
                            verts.Add(new Vector3(x + 1, y + 1, z));
                            verts.Add(new Vector3(x, y, z));
                            verts.Add(new Vector3(x + 1, y + 1, z));
                            verts.Add(new Vector3(x, y + 1, z));
                            verts.Add(new Vector3(x, y, z));

                            for (int i = 0; i < 6; i++)
                            {
                                normals.Add(Vector3.Forward);
                            }

                            var atlasRect = this.textureAtlas.GetBlockFrontUVs(block);
                            var x0 = atlasRect.Position.X;
                            var x1 = atlasRect.Position.X + atlasRect.Size.X;
                            var y0 = atlasRect.Position.Y;
                            var y1 = atlasRect.Position.Y + atlasRect.Size.Y;
                            uvs.Add(new Vector2(x0, y1));
                            uvs.Add(new Vector2(x0, y0));
                            uvs.Add(new Vector2(x1, y1));
                            uvs.Add(new Vector2(x0, y0));
                            uvs.Add(new Vector2(x1, y0));
                            uvs.Add(new Vector2(x1, y1));
                        }

                        if (this.GetBlockAt(chunk, x + 1, y, z) == Blocks.AIR)
                        {
                            verts.Add(new Vector3(x + 1, y, z + 1));
                            verts.Add(new Vector3(x + 1, y + 1, z + 1));
                            verts.Add(new Vector3(x + 1, y, z));
                            verts.Add(new Vector3(x + 1, y + 1, z + 1));
                            verts.Add(new Vector3(x + 1, y + 1, z));
                            verts.Add(new Vector3(x + 1, y, z));

                            for (int i = 0; i < 6; i++)
                            {
                                normals.Add(Vector3.Right);
                            }

                            var atlasRect = this.textureAtlas.GetBlockRightUVs(block);
                            var x0 = atlasRect.Position.X;
                            var x1 = atlasRect.Position.X + atlasRect.Size.X;
                            var y0 = atlasRect.Position.Y;
                            var y1 = atlasRect.Position.Y + atlasRect.Size.Y;
                            uvs.Add(new Vector2(x0, y1));
                            uvs.Add(new Vector2(x0, y0));
                            uvs.Add(new Vector2(x1, y1));
                            uvs.Add(new Vector2(x0, y0));
                            uvs.Add(new Vector2(x1, y0));
                            uvs.Add(new Vector2(x1, y1));
                        }

                        if (this.GetBlockAt(chunk, x, y, z + 1) == Blocks.AIR)
                        {
                            verts.Add(new Vector3(x, y, z + 1));
                            verts.Add(new Vector3(x, y + 1, z + 1));
                            verts.Add(new Vector3(x + 1, y, z + 1));
                            verts.Add(new Vector3(x, y + 1, z + 1));
                            verts.Add(new Vector3(x + 1, y + 1, z + 1));
                            verts.Add(new Vector3(x + 1, y, z + 1));

                            for (int i = 0; i < 6; i++)
                            {
                                normals.Add(Vector3.Back);
                            }

                            var atlasRect = this.textureAtlas.GetBlockBackUVs(block);
                            var x0 = atlasRect.Position.X;
                            var x1 = atlasRect.Position.X + atlasRect.Size.X;
                            var y0 = atlasRect.Position.Y;
                            var y1 = atlasRect.Position.Y + atlasRect.Size.Y;
                            uvs.Add(new Vector2(x0, y1));
                            uvs.Add(new Vector2(x0, y0));
                            uvs.Add(new Vector2(x1, y1));
                            uvs.Add(new Vector2(x0, y0));
                            uvs.Add(new Vector2(x1, y0));
                            uvs.Add(new Vector2(x1, y1));
                        }

                        if (this.GetBlockAt(chunk, x - 1, y, z) == Blocks.AIR)
                        {
                            verts.Add(new Vector3(x, y, z));
                            verts.Add(new Vector3(x, y + 1, z));
                            verts.Add(new Vector3(x, y, z + 1));
                            verts.Add(new Vector3(x, y + 1, z));
                            verts.Add(new Vector3(x, y + 1, z + 1));
                            verts.Add(new Vector3(x, y, z + 1));

                            for (int i = 0; i < 6; i++)
                            {
                                normals.Add(Vector3.Left);
                            }

                            var atlasRect = this.textureAtlas.GetBlockLeftUVs(block);
                            var x0 = atlasRect.Position.X;
                            var x1 = atlasRect.Position.X + atlasRect.Size.X;
                            var y0 = atlasRect.Position.Y;
                            var y1 = atlasRect.Position.Y + atlasRect.Size.Y;
                            uvs.Add(new Vector2(x0, y1));
                            uvs.Add(new Vector2(x0, y0));
                            uvs.Add(new Vector2(x1, y1));
                            uvs.Add(new Vector2(x0, y0));
                            uvs.Add(new Vector2(x1, y0));
                            uvs.Add(new Vector2(x1, y1));
                        }

                        if (this.GetBlockAt(chunk, x, y + 1, z) == Blocks.AIR)
                        {
                            verts.Add(new Vector3(x + 1, y + 1, z));
                            verts.Add(new Vector3(x + 1, y + 1, z + 1));
                            verts.Add(new Vector3(x, y + 1, z));
                            verts.Add(new Vector3(x + 1, y + 1, z + 1));
                            verts.Add(new Vector3(x, y + 1, z + 1));
                            verts.Add(new Vector3(x, y + 1, z));

                            for (int i = 0; i < 6; i++)
                            {
                                normals.Add(Vector3.Up);
                            }

                            var atlasRect = this.textureAtlas.GetBlockTopUVs(block);
                            var x0 = atlasRect.Position.X;
                            var x1 = atlasRect.Position.X + atlasRect.Size.X;
                            var y0 = atlasRect.Position.Y;
                            var y1 = atlasRect.Position.Y + atlasRect.Size.Y;
                            uvs.Add(new Vector2(x0, y1));
                            uvs.Add(new Vector2(x0, y0));
                            uvs.Add(new Vector2(x1, y1));
                            uvs.Add(new Vector2(x0, y0));
                            uvs.Add(new Vector2(x1, y0));
                            uvs.Add(new Vector2(x1, y1));
                        }

                        if (this.GetBlockAt(chunk, x, y - 1, z) == Blocks.AIR)
                        {
                            verts.Add(new Vector3(x, y, z));
                            verts.Add(new Vector3(x, y, z + 1));
                            verts.Add(new Vector3(x + 1, y, z));
                            verts.Add(new Vector3(x, y, z + 1));
                            verts.Add(new Vector3(x + 1, y, z + 1));
                            verts.Add(new Vector3(x + 1, y, z));

                            for (int i = 0; i < 6; i++)
                            {
                                normals.Add(Vector3.Down);
                            }

                            var atlasRect = this.textureAtlas.GetBlockBottomUVs(block);
                            var x0 = atlasRect.Position.X;
                            var x1 = atlasRect.Position.X + atlasRect.Size.X;
                            var y0 = atlasRect.Position.Y;
                            var y1 = atlasRect.Position.Y + atlasRect.Size.Y;
                            uvs.Add(new Vector2(x0, y1));
                            uvs.Add(new Vector2(x0, y0));
                            uvs.Add(new Vector2(x1, y1));
                            uvs.Add(new Vector2(x0, y0));
                            uvs.Add(new Vector2(x1, y0));
                            uvs.Add(new Vector2(x1, y1));
                        }
                    }
                }
            }
        }

        if (verts.Count > 0)
        {
            surfaceArray[(int)Mesh.ArrayType.Vertex] = verts.ToArray();
            surfaceArray[(int)Mesh.ArrayType.Normal] = normals.ToArray();
            surfaceArray[(int)Mesh.ArrayType.TexUV] = uvs.ToArray();

            var mesh = new ArrayMesh();
            mesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, surfaceArray);
            mesh.SurfaceSetMaterial(0, this.material);
            chunk.GetSubchunk(subchunk).MeshInstance.Mesh = mesh;
        }
        else
        {
            chunk.GetSubchunk(subchunk).MeshInstance.Mesh = null;
        }
    }

    private Block GetBlockAt(Chunk chunk, int x, int y, int z)
    {
        if (y < 0 || y >= Chunk.HEIGHT)
        {
            return Blocks.AIR;
        }
        else if (x < 0)
        {
            return chunk.WestNeighbor.GetBlockAt(x + Chunk.WIDTH, y, z);
        }
        else if (x >= Chunk.WIDTH)
        {
            return chunk.EastNeighbor.GetBlockAt(x - Chunk.WIDTH, y, z);
        }
        else if (z < 0)
        {
            return chunk.NorthNeighbor.GetBlockAt(x, y, z + Chunk.WIDTH);
        }
        else if (z >= Chunk.WIDTH)
        {
            return chunk.SouthNeighbor.GetBlockAt(x, y, z - Chunk.WIDTH);
        }
        else
        {
            return chunk.GetBlockAt(x, y, z);
        }
    }
}
