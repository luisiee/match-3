using Godot;

public partial class Cell : Node2D
{
    [Signal] public delegate void SelectedEventHandler(Cell cell);

    public CellType Type;
    public Item Item;
    public Vector2I MapCoords;

    public const int TYPE_GEN_MIN = 2;
    public const int TYPE_GEN_MAX = 10;

    public void Init()
    {
        Map map = GetParent<Map>();
        Image atlasImage = map.CellAtlas.Texture.GetImage();
        Rect2I textureRegion = new(Map.TEXTURE_SIZE * map.GetCellAtlasCoords(Type), Map.TEXTURE_SIZE * Vector2I.One);
        ImageTexture texture = ImageTexture.CreateFromImage(atlasImage.GetRegion(textureRegion));
        GetNode<Sprite2D>("Sprite2D").Texture = texture;
    }

    public void OnInputEvent(Node viewport, InputEvent inputEvent, int shapeIdx)
    {
        if (!inputEvent.IsActionReleased("left_click")) return;
        EmitSignal(SignalName.Selected, this);
    }
}

public enum CellType
{
    NONE,
    NORMAL,

    GENERATOR_CENTER = 2,
    GENERATOR_TOP,
    GENERATOR_LEFT,
    GENERATOR_BOTTOM,
    GENERATOR_RIGHT,
    GENERATOR_TOP_RIGHT,
    GENERATOR_TOP_LEFT,
    GENERATOR_BOTTOM_LEFT,
    GENERATOR_BOTTOM_RIGHT = 10,
}
