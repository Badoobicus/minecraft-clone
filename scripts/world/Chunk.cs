using System;
using Godot;

public class Chunk
{
    public const int WIDTH = 16;
    public const int HEIGHT = 384;
    public const int SUBCHUNKS = 24;
    public const int SUBCHUNUK_WIDTH = 16;
    public const int SUBCHUNK_HEIGHT = 16;

    public int X { get; private set; }
    public int Z { get; set; }
    public Vector2I Pos
    {
        get => new Vector2I(this.X, this.Z);
    }
    public bool Dirty { get; private set; }

    public Node MeshHolder
    {
        get => this.meshHolder;
        set => this.meshHolder = value;
    }
    public Chunk NorthNeighbor { get; set; } = null;
    public Chunk EastNeighbor { get; set; } = null;
    public Chunk SouthNeighbor { get; set; } = null;
    public Chunk WestNeighbor { get; set; } = null;

    private Subchunk[] subchunks = new Subchunk[SUBCHUNKS];
    private Block[,,] blocks = new Block[HEIGHT, WIDTH, WIDTH];
    private Node meshHolder = null;

    public Chunk(int x, int z)
    {
        this.X = x;
        this.Z = z;

        for (int i = 0; i < this.subchunks.Length; i++)
        {
            this.subchunks[i] = new Subchunk(this);
        }
    }

    public Chunk(Vector2I pos)
        : this(pos.X, pos.Y) { }

    public void SetBlockAt(int x, int y, int z, Block block)
    {
        this.subchunks[y / SUBCHUNK_HEIGHT].Dirty = true;
        this.blocks[y, x, z] = block;
    }

    public Block GetBlockAt(int x, int y, int z)
    {
        return blocks[y, x, z];
    }

    public Subchunk GetSubchunk(int index)
    {
        return this.subchunks[index];
    }

    public void Destroy()
    {
        // TODO theres something really slow about loading or unloading multiple subchunks. investigate and optimize
        this.blocks = null;
        this.subchunks = null;
        this.meshHolder.QueueFree();

        if (this.NorthNeighbor != null)
        {
            this.NorthNeighbor.SouthNeighbor = null;
        }

        if (this.EastNeighbor != null)
        {
            this.EastNeighbor.WestNeighbor = null;
        }

        if (this.SouthNeighbor != null)
        {
            this.SouthNeighbor.NorthNeighbor = null;
        }

        if (this.WestNeighbor != null)
        {
            this.WestNeighbor.EastNeighbor = null;
        }
    }

    public class Subchunk
    {
        public Chunk Chunk
        {
            get => this.chunk;
        }
        public MeshInstance3D MeshInstance { get; set; }
        public bool Dirty
        {
            get => this.dirty;
            set
            {
                this.dirty = value;
                if (value)
                {
                    this.chunk.Dirty = true;
                }
                else
                {
                    foreach (var subchunk in this.chunk.subchunks)
                    {
                        if (subchunk.Dirty)
                        {
                            return;
                        }
                    }

                    this.chunk.Dirty = false;
                }
            }
        }
        private Chunk chunk;
        private bool dirty;

        public Subchunk(Chunk chunk)
        {
            this.chunk = chunk;
        }
    }
}
