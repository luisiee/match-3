using Godot;

public partial class Item : Node2D
{
    public ItemType Type;

    public Sprite2D Sprite { get; private set; }
    private ImageTexture texture;
    private ImageTexture highlightedTexture;
    public AnimationPlayer AnimationPlayer { get; private set; }

    private bool highlighted;
    public bool Highlighted
    {
        get { return highlighted; }
        set
        {
            if (value)
                Sprite.Texture = highlightedTexture;
            else
                Sprite.Texture = texture;

            highlighted = value;
        }
    }

    public void Init()
    {
        Map map = GetParent<Map>();
        Image atlasImage = map.ItemAtlas.Texture.GetImage();

        Rect2I textureRegion = new(Map.TEXTURE_SIZE * map.ITEM_TYPE_TO_ATLAS_COORDS[Type], Map.TEXTURE_SIZE * Vector2I.One);
        Rect2I highlightedTextureRegion = new(textureRegion.Position + 128 * Vector2I.Down, textureRegion.Size);

        texture = ImageTexture.CreateFromImage(atlasImage.GetRegion(textureRegion));
        highlightedTexture = ImageTexture.CreateFromImage(atlasImage.GetRegion(highlightedTextureRegion));

        Sprite = GetNode<Sprite2D>("Sprite2D");
        Sprite.Texture = texture;
        AnimationPlayer = Sprite.GetNode<AnimationPlayer>("AnimationPlayer");
    }
}

public enum ItemType
{
    NONE,
    COPPER,
    IRON,
    DIAMOND,
    AMETHYST,
}
