using Godot;

public partial class Cell : Node2D
{
    [Signal] public delegate void SelectedEventHandler(Cell cell);

    private Map map;

    private CellType _type;
    public CellType Type
    {
        get { return _type; }
        set
        {
            _type = value;
            Image atlasImage = map.CellAtlas.Texture.GetImage();
            Rect2I textureRegion = new(Map.TEXTURE_SIZE * map.GetCellAtlasCoords(Type), Map.TEXTURE_SIZE * Vector2I.One);
            ImageTexture texture = ImageTexture.CreateFromImage(atlasImage.GetRegion(textureRegion));
            GetNode<Sprite2D>("Sprite2D").Texture = texture;
        }
    }
    public Item Item;
    public Vector2I MapCoords;

    public const int TYPE_GENERATOR_MIN = 2;
    public const int TYPE_GENERATOR_MAX = 10;
    public const int TYPE_COUNTER_MIN = 11;
    public const int TYPE_COUNTER_MAX = 14;

    public void Init()
    {
        map = GetParent<Map>();
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

    COUNTER_1 = 11,
    COUNTER_2,
    COUNTER_3,
    COUNTER_4 = 14,
}
