using System.Threading.Tasks;
using Godot;

public partial class Level : Node
{
    private Map map;
    private BackgroundArea backgroundArea;

    private bool interactable;
    private Cell selectedCell;

    public override void _Ready()
    {
        map = GetNode<Map>("Map");
        backgroundArea = GetNode<BackgroundArea>("BackgroundArea");

        map.Init();
        LinkSignals();
        Start();
    }

    private void LinkSignals()
    {
        foreach (Cell cell in map.Cells)
        {
            cell.Selected += OnMapCellSelected;
        }
        backgroundArea.Selected += OnBackgroundAreaSelected;
    }

    private async void Start()
    {
        foreach (Cell cell in map.Cells)
        {
            if (cell.Item.Type == ItemType.NONE) continue;

            cell.Item.Sprite.Scale = Vector2.Zero;
            cell.Item.Visible = true;
            await PlayItemAnimation(cell.Item, "item/spawn", 2);
        }
        interactable = true;
    }

    public async void OnMapCellSelected(Cell cell)
    {
        if (!interactable) return;
        interactable = false;
        GD.Print("Selected cell!");

        if (selectedCell != null)
        {
            selectedCell.Item.Highlighted = false;

            if (cell.Type == CellType.NONE || (cell.MapCoords - selectedCell.MapCoords).Length() != 1)
            {
                if (cell.Type != CellType.NONE)
                {
                    PlayItemAnimation(cell.Item, "item/invalid_move");
                }
                await PlayItemAnimation(selectedCell.Item, "item/invalid_move");
            }
            else
            {
                Vector2I direction = cell.MapCoords - selectedCell.MapCoords;
                if (direction.Y == -1)
                {
                    PlayItemAnimation(selectedCell.Item, "item/move_up");
                    await PlayItemAnimation(cell.Item, "item/move_down");
                }
                else if (direction.Y == 1)
                {
                    PlayItemAnimation(selectedCell.Item, "item/move_down");
                    await PlayItemAnimation(cell.Item, "item/move_up");
                }
                else if (direction.X == 1)
                {
                    PlayItemAnimation(selectedCell.Item, "item/move_right");
                    await PlayItemAnimation(cell.Item, "item/move_left");
                }
                else if (direction.X == -1)
                {
                    PlayItemAnimation(selectedCell.Item, "item/move_left");
                    await PlayItemAnimation(cell.Item, "item/move_right");
                }

                Vector2I selectedCellMapCoords = selectedCell.MapCoords;
                ItemType selectedCellItemType = selectedCell.Item.Type;
                Vector2I cellMapCoords = cell.MapCoords;
                ItemType cellItemType = cell.Item.Type;

                map.SetItem(selectedCellMapCoords, cellItemType);
                map.SetItem(cellMapCoords, selectedCellItemType);
            }

            selectedCell = null;
            interactable = true;
            return;
        }

        if (cell.Type == CellType.NONE)
        {
            selectedCell = null;
        }
        else
        {
            selectedCell = cell;
            cell.Item.Highlighted = true;
            await PlayItemAnimation(cell.Item, "item/grow");
            await PlayItemAnimation(cell.Item, "item/shrink");
        }

        interactable = true;
    }

    public async void OnBackgroundAreaSelected()
    {
        if (!interactable) return;
        interactable = false;
        GD.Print("Selected background!");

        if (selectedCell != null)
        {
            selectedCell.Item.Highlighted = false;
            await PlayItemAnimation(selectedCell.Item, "item/invalid_move");
            selectedCell = null;
        }

        interactable = true;
    }

    private async Task PlayItemAnimation(Item item, string animationName, float speed = 1)
    {
        AnimationPlayer animationPlayer = item.AnimationPlayer;
        float animationLength = animationPlayer.GetAnimation(animationName).Length / speed;
        animationPlayer.Play(animationName, customSpeed: speed);
        await ToSignal(GetTree().CreateTimer(animationLength), SceneTreeTimer.SignalName.Timeout);
    }
}
