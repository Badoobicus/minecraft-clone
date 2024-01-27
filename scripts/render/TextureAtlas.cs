using Godot;
using System.Collections.Generic;

public class TextureAtlas
{
    public Texture Texture
    {
        get => this.texture;
    }
    private Texture texture = null;
    private Dictionary<string, Rect2> oldUvLookup = new Dictionary<string, Rect2>();
    private Dictionary<string, Rect2> uvLookup = new Dictionary<string, Rect2>();

    public TextureAtlas()
    {
        var blockFaceToImageName = new Dictionary<string, string>();

        // TODO maybe put these definitions somewhere else
        blockFaceToImageName[$"{Blocks.GRASS.Id}_front"] = "grass.png";
        blockFaceToImageName[$"{Blocks.GRASS.Id}_right"] = "grass.png";
        blockFaceToImageName[$"{Blocks.GRASS.Id}_back"] = "grass.png";
        blockFaceToImageName[$"{Blocks.GRASS.Id}_left"] = "grass.png";
        blockFaceToImageName[$"{Blocks.GRASS.Id}_top"] = "grass_top.png";
        blockFaceToImageName[$"{Blocks.GRASS.Id}_bottom"] = "dirt.png";

        blockFaceToImageName[$"{Blocks.DIRT.Id}_front"] = "dirt.png";
        blockFaceToImageName[$"{Blocks.DIRT.Id}_right"] = "dirt.png";
        blockFaceToImageName[$"{Blocks.DIRT.Id}_back"] = "dirt.png";
        blockFaceToImageName[$"{Blocks.DIRT.Id}_left"] = "dirt.png";
        blockFaceToImageName[$"{Blocks.DIRT.Id}_top"] = "dirt.png";
        blockFaceToImageName[$"{Blocks.DIRT.Id}_bottom"] = "dirt.png";

        blockFaceToImageName[$"{Blocks.STONE.Id}_front"] = "stone.png";
        blockFaceToImageName[$"{Blocks.STONE.Id}_right"] = "stone.png";
        blockFaceToImageName[$"{Blocks.STONE.Id}_back"] = "stone.png";
        blockFaceToImageName[$"{Blocks.STONE.Id}_left"] = "stone.png";
        blockFaceToImageName[$"{Blocks.STONE.Id}_top"] = "stone.png";
        blockFaceToImageName[$"{Blocks.STONE.Id}_bottom"] = "stone.png";

        var imageCount = new HashSet<string>(blockFaceToImageName.Values).Count;
        var imageNameToAtlasPos = new Dictionary<string, Rect2>();
        var blockFaceToAtlasPos = new Dictionary<string, Rect2>();
        var atlasImage = Image.Create(imageCount * 16, 16, true, Image.Format.Rgb8);
        int i = 0;

        foreach (var entry in blockFaceToImageName)
        {
            var blockFace = entry.Key;
            var imageName = entry.Value;
            if (!imageNameToAtlasPos.ContainsKey(imageName))
            {
                var image = ResourceLoader.Load<Image>($"textures/{imageName}");
                image.Convert(Image.Format.Rgb8);
                atlasImage.BlitRect(
                    image,
                    new Rect2I(Vector2I.Zero, Vector2I.One * 16),
                    new Vector2I(i * 16, 0)
                );
                imageNameToAtlasPos[imageName] = new Rect2(
                    new Vector2((i * 16) / (float)atlasImage.GetWidth(), 0),
                    new Vector2(
                        16 / (float)atlasImage.GetWidth(),
                        16 / (float)atlasImage.GetHeight()
                    )
                );
                i++;
            }
            blockFaceToAtlasPos[blockFace] = imageNameToAtlasPos[imageName];
        }

        var texture = new ImageTexture();
        texture.SetImage(atlasImage);
        this.texture = texture;
        this.uvLookup = blockFaceToAtlasPos;
    }

    public Rect2 GetBlockFrontUVs(Block block)
    {
        return this.uvLookup[$"{block.Id}_front"];
    }

    public Rect2 GetBlockRightUVs(Block block)
    {
        return this.uvLookup[$"{block.Id}_right"];
    }

    public Rect2 GetBlockBackUVs(Block block)
    {
        return this.uvLookup[$"{block.Id}_back"];
    }

    public Rect2 GetBlockLeftUVs(Block block)
    {
        return this.uvLookup[$"{block.Id}_left"];
    }

    public Rect2 GetBlockTopUVs(Block block)
    {
        return this.uvLookup[$"{block.Id}_top"];
    }

    public Rect2 GetBlockBottomUVs(Block block)
    {
        return this.uvLookup[$"{block.Id}_bottom"];
    }
}
