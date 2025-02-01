using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class Map : Node2D
{
    public const int HEIGHT = 8;
    public const int WIDTH = 16;

    public Cell[,] Cells { get; private set; } = new Cell[HEIGHT, WIDTH];

    public const int TEXTURE_SIZE = 32;
    public static TileSetAtlasSource CellAtlas { get; private set; }
    public static TileSetAtlasSource ItemAtlas { get; private set; }

    private static PackedScene cellScene;
    private static PackedScene itemScene;

    public static readonly Dictionary<CellType, Vector2I> CELL_TYPE_TO_ATLAS_COORDS = new()
    {
        { CellType.NONE,            new Vector2I(0, 0) },
        { CellType.NORMAL,          new Vector2I(1, 0) },
        { CellType.GENERATOR_TOP,   new Vector2I(2, 0) },
    };

    public static readonly Dictionary<ItemType, Vector2I> ITEM_TYPE_TO_ATLAS_COORDS = new()
    {
        { ItemType.NONE,            new Vector2I(0, 0) },
        { ItemType.COPPER,          new Vector2I(1, 0) },
        { ItemType.IRON,            new Vector2I(2, 0) },
        { ItemType.DIAMOND,         new Vector2I(3, 0) },
    };

    public void Init()
    {
        // @Temporary, in the future maps will be directely loaded into the Cells array
        TileSet cellTileSet = GD.Load<TileSet>("res://sprites/cell_tileset.tres");
        TileSet itemTileSet = GD.Load<TileSet>("res://sprites/item_tileset.tres");
        cellScene = GD.Load<PackedScene>("res://scenes/cell.tscn");
        itemScene = GD.Load<PackedScene>("res://scenes/item.tscn");

        if (cellScene == null) GD.PushError("Could not locate cell scene.");
        if (itemScene == null) GD.PushError("Could not locate item scene.");

        TileMapLayer cellLayer = GetNode<TileMapLayer>("CellLayer");
        TileMapLayer itemLayer = GetNode<TileMapLayer>("ItemLayer");

        CellAtlas = (TileSetAtlasSource)cellTileSet.GetSource(cellTileSet.GetSourceId(0));
        ItemAtlas = (TileSetAtlasSource)itemTileSet.GetSource(itemTileSet.GetSourceId(0));

        for (int y = 0; y < HEIGHT; y++)
        {
            for (int x = 0; x < WIDTH; x++)
            {
                Vector2I mapCoords = new(x, y);

                // Get the atlas coords for the cell and item
                // If the cell or item doesn't exist, set the atlas coords of type NONE
                Vector2I cellAtlasCoords = cellLayer.GetCellAtlasCoords(mapCoords);
                Vector2I itemAtlasCoords = itemLayer.GetCellAtlasCoords(mapCoords);

                if (cellAtlasCoords == -Vector2I.One)
                {
                    cellAtlasCoords = Vector2I.Zero;
                    itemAtlasCoords = Vector2I.Zero;
                }
                else if (itemAtlasCoords == -Vector2I.One)
                {
                    itemAtlasCoords = Vector2I.Zero;
                }

                CellType cellType = CELL_TYPE_TO_ATLAS_COORDS.FirstOrDefault(x => x.Value == cellAtlasCoords).Key;
                ItemType itemType = ITEM_TYPE_TO_ATLAS_COORDS.FirstOrDefault(x => x.Value == itemAtlasCoords).Key;
                SetCell(mapCoords, cellType);
                SetItem(mapCoords, itemType, false);
            }
        }

        cellLayer.QueueFree();
        itemLayer.QueueFree();
    }

    private static Vector2 MapToLocal(Vector2I mapCoords)
    {
        return TEXTURE_SIZE * ((Vector2)mapCoords + Vector2.One / 2);
    }

    // @Incomplete: Add boundary checks
    public void SetCell(Vector2I mapCoords, CellType cellType = CellType.NONE)
    {
        Cell cell = cellScene.Instantiate<Cell>();
        cell.Type = cellType;
        cell.MapCoords = mapCoords;
        cell.Position = MapToLocal(mapCoords);
        cell.Init();

        Cells[mapCoords.Y, mapCoords.X]?.QueueFree();
        Cells[mapCoords.Y, mapCoords.X] = cell;
        AddChild(cell);
    }

    // @Incomplete: Add boundary checks
    public void SetItem(Vector2I mapCoords, ItemType itemType = ItemType.NONE, bool visible = true)
    {
        Item item = itemScene.Instantiate<Item>();
        item.Visible = visible;
        item.Type = itemType;
        item.Position = MapToLocal(mapCoords);
        item.Init();

        Cells[mapCoords.Y, mapCoords.X].Item?.QueueFree();
        Cells[mapCoords.Y, mapCoords.X].Item = item;
        AddChild(item);
    }
}
