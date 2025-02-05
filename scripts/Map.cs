using Godot;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public partial class Map : Node2D
{
    public const int HEIGHT = 10;
    public const int WIDTH = 12;

    public Cell[,] Cells { get; private set; } = new Cell[HEIGHT, WIDTH];

    public List<Cell> CenterGenerators { get; private set; } = new();
    public List<GeneratorVector> VerticalVectors { get; private set; } = new();
    public List<GeneratorVector> HorizontalVectors { get; private set; } = new();

    public MatchType[,] Matches { get; private set; } = new MatchType[HEIGHT, WIDTH];

    public const int TEXTURE_SIZE = 32;
    public const int CELL_ATLAS_SIZE = 8;
    public const int ITEM_ATLAS_SIZE = 8;

    public TileSetAtlasSource CellAtlas { get; private set; }
    public TileSetAtlasSource ItemAtlas { get; private set; }

    private PackedScene cellScene;
    private PackedScene itemScene;

    private Level level;

    // @Incomplete: migrate to function and arrays
    public readonly Dictionary<CellType, Vector2I> CELL_TYPE_TO_ATLAS_COORDS = new()
    {
        { CellType.NONE,                new Vector2I(0, 0) },
        { CellType.NORMAL,              new Vector2I(1, 0) },
        { CellType.GENERATOR_CENTER,    new Vector2I(2, 0) },
        { CellType.GENERATOR_TOP,       new Vector2I(3, 0) },
        { CellType.GENERATOR_LEFT,      new Vector2I(4, 0) },
        { CellType.GENERATOR_BOTTOM,    new Vector2I(5, 0) },
        { CellType.GENERATOR_RIGHT,     new Vector2I(6, 0) },
    };

    public readonly Dictionary<ItemType, Vector2I> ITEM_TYPE_TO_ATLAS_COORDS = new()
    {
        { ItemType.NONE,        new Vector2I(0, 0) },
        { ItemType.COPPER,      new Vector2I(1, 0) },
        { ItemType.IRON,        new Vector2I(2, 0) },
        { ItemType.DIAMOND,     new Vector2I(3, 0) },
        { ItemType.AMETHYST,    new Vector2I(4, 0) },
    };

    private readonly CellType[] CELL_ATLAS_INFO =
    {
        CellType.NONE,
        CellType.NORMAL,

        CellType.GENERATOR_CENTER,
        CellType.GENERATOR_TOP,
        CellType.GENERATOR_LEFT,
        CellType.GENERATOR_BOTTOM,
        CellType.GENERATOR_RIGHT,
        CellType.GENERATOR_TOP_RIGHT,
        CellType.GENERATOR_TOP_LEFT,
        CellType.GENERATOR_BOTTOM_LEFT,
        CellType.GENERATOR_BOTTOM_RIGHT,
    };

    private readonly ItemType[] ITEM_ATLAS_INFO =
    {
        ItemType.NONE,
        ItemType.COPPER,
        ItemType.IRON,
        ItemType.DIAMOND,
        ItemType.AMETHYST,
    };

    public void Init()
    {
        level = GetParent<Level>();

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

        LoadGenerators();
    }

    private static Vector2 MapToLocal(Vector2I mapCoords)
    {
        return TEXTURE_SIZE * ((Vector2)mapCoords + Vector2.One / 2);
    }

    public Vector2I GetCellAtlasCoords(CellType type)
    {
        int row = -1;
        for (int col = 0; col < CELL_ATLAS_INFO.Length; col++)
        {
            if (col % CELL_ATLAS_SIZE == 0) row++;
            if (CELL_ATLAS_INFO[col] == type)
            {
                return new(col % CELL_ATLAS_SIZE, row);
            }
        }
        return -Vector2I.One;
    }

    public Vector2I GetItemAtlasCoords(ItemType type)
    {
        int row = -1;
        for (int col = 0; col < ITEM_ATLAS_INFO.Length; col++)
        {
            if (col % ITEM_ATLAS_SIZE == 0) row++;
            if (ITEM_ATLAS_INFO[col] == type)
            {
                return new(col % ITEM_ATLAS_SIZE, row);
            }
        }
        return -Vector2I.One;
    }

    public static bool InBounds(Vector2I mapCoords)
    {
        return mapCoords.X >= 0 || mapCoords.X < WIDTH || mapCoords.Y >= 0 || mapCoords.Y < HEIGHT;
    }

    public void SetCell(Vector2I mapCoords, CellType type = CellType.NONE)
    {
        if (mapCoords.X < 0 || mapCoords.X >= WIDTH || mapCoords.Y < 0 || mapCoords.Y >= HEIGHT)
        {
            GD.PrintErr("[Map]: Tried to set cell at invalid coords.");
            return;
        }

        Cell cell = cellScene.Instantiate<Cell>();
        cell.Type = type;
        cell.MapCoords = mapCoords;
        cell.Position = MapToLocal(mapCoords);

        Cells[mapCoords.Y, mapCoords.X]?.QueueFree();
        Cells[mapCoords.Y, mapCoords.X] = cell;
        AddChild(cell);
        cell.Init();

        cell.Selected += level.OnMapCellSelected;
    }

    // WARNING: Does not auto recalculate the matches
    public void SetItem(Vector2I mapCoords, ItemType type = ItemType.NONE, bool visible = true)
    {
        if (mapCoords.X < 0 || mapCoords.X >= WIDTH || mapCoords.Y < 0 || mapCoords.Y >= HEIGHT)
        {
            GD.PrintErr("[Map]: Tried to set item at invalid coords.");
            return;
        }

        if (Cells[mapCoords.Y, mapCoords.X].Item is not null && Cells[mapCoords.Y, mapCoords.X].Item.Type == type) return;

        if (Cells[mapCoords.Y, mapCoords.X].Type == CellType.NONE && type != ItemType.NONE)
        {
            GD.PrintErr("[Map]: Tried to set item at cell of type NONE.");
            return;
        }

        Item item = itemScene.Instantiate<Item>();
        item.Visible = visible;
        item.Type = type;
        item.Position = MapToLocal(mapCoords);

        Cells[mapCoords.Y, mapCoords.X].Item?.QueueFree();
        Cells[mapCoords.Y, mapCoords.X].Item = item;
        AddChild(item);
        item.Init();
    }

    // WARNING: Does not auto recalculate the matches
    public void SetItem(Cell cell, ItemType type = ItemType.NONE, bool visible = true)
    {
        if (cell.Item is not null && cell.Item.Type == type) return;

        if (cell.MapCoords.X < 0 || cell.MapCoords.X >= WIDTH || cell.MapCoords.Y < 0 || cell.MapCoords.Y >= HEIGHT)
        {
            GD.PrintErr("[Map]: Tried to set item at invalid coords.");
            return;
        }

        Item item = itemScene.Instantiate<Item>();
        item.Visible = visible;
        item.Type = type;
        item.Position = MapToLocal(cell.MapCoords);

        cell.Item?.QueueFree();
        cell.Item = item;
        AddChild(item);
        item.Init();
    }

    // @Incomplete: Add support for corner generators
    private void LoadGenerators()
    {
        foreach (Cell cell in Cells)
        {
            if ((int)cell.Type < Cell.TYPE_GEN_MIN || (int)cell.Type > Cell.TYPE_GEN_MAX)
            {
                continue;
            }

            if (cell.Type == CellType.GENERATOR_CENTER)
            {
                CenterGenerators.Add(cell);
                continue;
            }

            // Assign default values so that the compiler doesn't complain
            Vector2I direction = new();
            CellType complementType = CellType.NONE;

            switch (cell.Type)
            {
                case CellType.GENERATOR_TOP:
                    direction = Vector2I.Down;
                    complementType = CellType.GENERATOR_BOTTOM;
                    break;
                case CellType.GENERATOR_BOTTOM:
                    direction = Vector2I.Up;
                    complementType = CellType.GENERATOR_TOP;
                    break;
                case CellType.GENERATOR_LEFT:
                    direction = Vector2I.Right;
                    complementType = CellType.GENERATOR_RIGHT;
                    break;
                case CellType.GENERATOR_RIGHT:
                    direction = Vector2I.Left;
                    complementType = CellType.GENERATOR_LEFT;
                    break;
            }

            Vector2I start = cell.MapCoords;
            Vector2I tip = cell.MapCoords + direction;
            while (InBounds(tip) && Cells[tip.Y, tip.X].Type != CellType.NONE && Cells[tip.Y, tip.X].Type != complementType)
            {
                tip += direction;
            }

            bool priority = true;
            if (InBounds(tip) && (Cells[tip.Y, tip.X].Type == CellType.GENERATOR_TOP || Cells[tip.Y, tip.X].Type == CellType.GENERATOR_LEFT))
            {
                priority = false;
            }

            if (cell.Type == CellType.GENERATOR_TOP || cell.Type == CellType.GENERATOR_BOTTOM)
            {
                VerticalVectors.Add(new(start, tip - direction, priority));
            }
            else
            {
                HorizontalVectors.Add(new(start, tip - direction, priority));
            }
        }
    }

    // NOTE: We calculate the matches just before checking
    public void CalculateMatches()
    {
        for (int y = 0; y < HEIGHT; y++)
        {
            for (int x = 0; x < WIDTH; x++)
            {
                Matches[y, x] = MatchType.NONE;

                // Cell type NONE => item type NONE
                if (Cells[y, x].Item.Type == ItemType.NONE) continue;

                if (x < WIDTH - 2 && Cells[y, x].Item.Type == Cells[y, x + 1].Item.Type && Cells[y, x].Item.Type == Cells[y, x + 2].Item.Type)
                {
                    Matches[y, x] |= MatchType.RIGHT;
                }
                if (y < HEIGHT - 2 && Cells[y, x].Item.Type == Cells[y + 1, x].Item.Type && Cells[y, x].Item.Type == Cells[y + 2, x].Item.Type)
                {
                    Matches[y, x] |= MatchType.DOWN;
                }
            }
        }
    }
}

public readonly struct GeneratorVector : IEnumerable
{
    public readonly Vector2I start;
    public readonly Vector2I end;
    public readonly Vector2I direction;     // Easier to store explicitely
    public readonly bool priority;          // When the vector is shared

    // Used for refactoring
    public enum Type
    {
        VERTICAL,
        HORIZONTAL,
    }

    public GeneratorVector(Vector2I start, Vector2I end, bool priority = true)
    {
        if (!Map.InBounds(start))
        {
            GD.PrintErr("[Map]: Tried to create generator vector with invalid starting point.");
        }
        if (!Map.InBounds(end))
        {
            GD.PrintErr("[Map]: Tried to create generator vector with invalid ending point.");
        }

        this.start = start;
        this.end = end;
        this.direction = (Vector2I)((Vector2)(end - start)).Normalized();
        this.priority = priority;
    }

    public IEnumerator<Vector2I> GetEnumerator()
    {
        Vector2I current = start;
        yield return current;
        while (current != end)
        {
            current += direction;
            yield return current;
        }
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    // @Unused
    public GeneratorVector Reversed()
    {
        return new(end, start, priority);
    }
}

[Flags]
public enum MatchType
{
    NONE = 0,
    RIGHT = 1 << 0,
    DOWN = 1 << 1,
}
