using Godot;

public partial class Cell : Node2D
{
    [Signal] public delegate void SelectedEventHandler(Cell cell);

    public CellType Type;
    public Item Item;
    public Vector2I MapCoords;

    public void Init()
    {
        Map map = GetParent<Map>();
        Image atlasImage = map.CellAtlas.Texture.GetImage();
        Rect2I textureRegion = new(Map.TEXTURE_SIZE * map.CELL_TYPE_TO_ATLAS_COORDS[Type], Map.TEXTURE_SIZE * Vector2I.One);
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
    GENERATOR_TOP,
    GENERATOR_LEFT,
    GENERATOR_BOTTOM,
    GENERATOR_RIGHT,
    GENERATOR_CENTER,
}
