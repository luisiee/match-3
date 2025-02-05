using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;

public partial class Level : Node
{
    private Map map;
    private BackgroundArea backgroundArea;
    private UI ui;

    private bool isInteractable;
    private Cell selectedCell;

    private int _moves;
    private int _score;

    private int Moves
    {
        get { return _moves; }
        set
        {
            _moves = value;
            ui.MovesLabel.Text = _moves.ToString();
        }
    }

    private int Score
    {
        get { return _score; }
        set
        {
            _score = value;
            ui.ScoreLabel.Text = _score.ToString();
        }
    }

    private const int SCORE_INC = 100;

    // @Incomplete: Hardcoded for now
    private ItemType[] spawnableItemTypes = { ItemType.COPPER, ItemType.IRON, ItemType.DIAMOND, ItemType.AMETHYST };
    private Objective[] objectives = { new ScoreObjective(500000) };

    private readonly struct AnimationInfo
    {
        public readonly AnimationPlayer player;
        public readonly string name;
        public readonly float speed;

        public AnimationInfo(AnimationPlayer player, string name, float speed = 1)
        {
            this.player = player;
            this.name = name;
            this.speed = speed;
        }
    }

    private List<AnimationInfo> animationBuffer = new();

    // Pause times waited before animation
    private const float MATCH_PAUSE_TIME = 0.1f;
    private const float GENERATE_TO_MATCH_PAUSE_TIME = 0.25f;
    private const float GENERATE_PAUSE_TIME = 0.25f;

    public override void _Ready()
    {
        map = GetNode<Map>("Map");
        backgroundArea = GetNode<BackgroundArea>("BackgroundArea");
        ui = GetNode<UI>("UI");

        map.Init();
        backgroundArea.Init();
        ui.Init();

        Start();
    }

    private async void Start()
    {
        // @Incomplete: Hardcoded for now
        Moves = 15;
        Score = 0;

        foreach (Cell cell in map.Cells)
        {
            if (cell.Item.Type == ItemType.NONE) continue;

            cell.Item.Sprite.Scale = Vector2.Zero;
            cell.Item.Visible = true;
            animationBuffer.Add(new(cell.Item.AnimationPlayer, "item/spawn", speed: 2));
            await PlayBufferAnimations();
        }

        await TryMatchGenerateCycle();
        isInteractable = true;
    }

    #region SIGNALS

    public async void OnMapCellSelected(Cell cell)
    {
        if (!isInteractable) return;
        isInteractable = false;

        // Try to select the cell
        // If succesful, we can return
        // Otherwise, we might need to perform a swap operation
        bool isSuccessfulSelect = await TrySelect(cell);
        if (isSuccessfulSelect)
        {
            isInteractable = true;
            return;
        }

        // Store the selected cell before deselecting
        Cell cellToSwapWith = selectedCell;

        // Try to deselect the selected cell
        // If no cell was selected we don't need to swap anything
        bool isSuccesfulDeselect = await TryDeselect();
        if (!isSuccesfulDeselect)
        {
            isInteractable = true;
            return;
        }

        // Try to write swap animation to buffer
        // When the items are unable to swap, write invalid move animations to buffer
        // Animations must be played before performing the swap!
        bool isSuccesfulSwap = await TrySwap(cell, cellToSwapWith);
        if (!isSuccesfulSwap)
        {
            isInteractable = true;
            return;
        }

        Moves--;
        await TryMatchGenerateCycle();

        if (Moves > 0)
        {
            isInteractable = true;
        }
    }

    public async void OnBackgroundAreaSelected()
    {
        if (!isInteractable) return;
        isInteractable = false;

        await TryDeselect(false);
        await PlayBufferAnimations();
        isInteractable = true;
    }

    #endregion SIGNALS
    #region PRIVATE FUNCTIONS

    private async Task<bool> TrySelect(Cell cell)
    {
        // Cell type NONE => item type NONE
        if (selectedCell is not null || cell.Item.Type == ItemType.NONE) return false;

        selectedCell = cell;
        selectedCell.Item.Highlighted = true;
        animationBuffer.Add(new(cell.Item.AnimationPlayer, "item/select"));
        await PlayBufferAnimations();
        return true;
    }

    private async Task<bool> TryDeselect(bool isValid = true)
    {
        if (selectedCell is null) return false;

        selectedCell.Item.Highlighted = false;
        if (!isValid)
        {
            animationBuffer.Add(new(selectedCell.Item.AnimationPlayer, "item/invalid_move"));
            await PlayBufferAnimations();
        }
        selectedCell = null;
        return true;
    }

    private async Task<bool> TrySwap(Cell a, Cell b, bool technical = true)
    {
        if (a == b)
        {
            animationBuffer.Add(new(a.Item.AnimationPlayer, "item/invalid_move"));
            await PlayBufferAnimations();
            return false;
        }

        if (a is null || a.Type == CellType.NONE || b is null || b.Type == CellType.NONE || a.Item.Type == b.Item.Type)
        {
            animationBuffer.Add(new(a.Item.AnimationPlayer, "item/invalid_move"));
            animationBuffer.Add(new(b.Item.AnimationPlayer, "item/invalid_move"));
            await PlayBufferAnimations();
            return false;
        }

        Vector2I direction = b.MapCoords - a.MapCoords;
        if (direction.Length() != 1)
        {
            animationBuffer.Add(new(a.Item.AnimationPlayer, "item/invalid_move"));
            animationBuffer.Add(new(b.Item.AnimationPlayer, "item/invalid_move"));
            await PlayBufferAnimations();
            return false;
        }

        if (direction.Y == -1)
        {
            animationBuffer.Add(new(a.Item.AnimationPlayer, "item/move_up"));
            animationBuffer.Add(new(b.Item.AnimationPlayer, "item/move_down"));
        }
        else if (direction.Y == 1)
        {
            animationBuffer.Add(new(a.Item.AnimationPlayer, "item/move_down"));
            animationBuffer.Add(new(b.Item.AnimationPlayer, "item/move_up"));
        }
        else if (direction.X == 1)
        {
            animationBuffer.Add(new(a.Item.AnimationPlayer, "item/move_right"));
            animationBuffer.Add(new(b.Item.AnimationPlayer, "item/move_left"));
        }
        else if (direction.X == -1)
        {
            animationBuffer.Add(new(a.Item.AnimationPlayer, "item/move_left"));
            animationBuffer.Add(new(b.Item.AnimationPlayer, "item/move_right"));
        }

        if (technical)
        {
            await PlayBufferAnimations();
            TechnicalSwap(a.MapCoords, b.MapCoords);
        }
        return true;
    }

    // NOTE: Cells must not necessarily be adjacent
    private void TechnicalSwap(Vector2I coords1, Vector2I coords2)
    {
        if (coords1 == coords2) return;
        ItemType itemType1 = map.Cells[coords1.Y, coords1.X].Item.Type;
        ItemType itemType2 = map.Cells[coords2.Y, coords2.X].Item.Type;
        map.SetItem(coords1, itemType2);
        map.SetItem(coords2, itemType1);
    }

    // NOTE: Item won't be visible
    // The animations in the buffer must be played
    private void TechnicalSpawn(Cell cell)
    {
        if ((int)cell.Type < Cell.TYPE_GEN_MIN || (int)cell.Type > Cell.TYPE_GEN_MAX)
        {
            GD.PushError("[Level]: Tried to spawn item in invalid cell. Mapcoords: ", cell.MapCoords);
            return;
        }

        map.SetItem(cell, spawnableItemTypes[GD.Randi() % spawnableItemTypes.Length], false);
        cell.Item.Sprite.Scale = Vector2.Zero;
        cell.Item.Visible = true;
        animationBuffer.Add(new(cell.Item.AnimationPlayer, "item/spawn"));
    }

    private async Task<bool> TryGenerateCenter()
    {
        bool success = false;

        foreach (Cell cell in map.CenterGenerators)
        {
            if (cell.Item.Type != ItemType.NONE) continue;

            success = true;
            TechnicalSpawn(cell);
        }

        if (success)
        {
            await Pause(GENERATE_PAUSE_TIME);
            await PlayBufferAnimations();
        }
        return success;
    }

    private List<GeneratorVector> GetEffectiveGeneratorVectors(List<GeneratorVector> vectors)
    {
        List<GeneratorVector> effectiveVectors = new();

        foreach (GeneratorVector vector in vectors)
        {
            int nEmpty = 0;
            Vector2I closestEmptyCoords = -Vector2I.One;
            foreach (Vector2I coords in vector)
            {
                if (map.Cells[coords.Y, coords.X].Item.Type == ItemType.NONE)
                {
                    nEmpty++;
                    if (closestEmptyCoords == -Vector2I.One)
                    {
                        closestEmptyCoords = coords;
                    }
                    if (vector.priority || nEmpty > 1)
                    {
                        break;
                    }
                }
            }

            // No empty cell found, or vector doesn't have priority
            if (closestEmptyCoords == -Vector2I.One || (!vector.priority && nEmpty == 1))
            {
                continue;
            }

            effectiveVectors.Add(new(vector.start, closestEmptyCoords));
        }

        return effectiveVectors;
    }

    private async Task<bool> TryGenerateVector(GeneratorVector.Type type)
    {
        bool success = false;
        List<GeneratorVector> vectors = (type == GeneratorVector.Type.VERTICAL) ? map.VerticalVectors : map.HorizontalVectors;

        // @Incomplete: finite loop?
        while (true)
        {
            List<GeneratorVector> effectiveVectors = GetEffectiveGeneratorVectors(vectors);
            if (effectiveVectors.Count > 0)
            {
                success = true;
            }
            else break;

            // MOVE ANIMATIONS
            foreach (GeneratorVector vector in effectiveVectors)
            {
                if (vector.direction != Vector2I.Zero)
                {
                    string animationName;
                    if (type == GeneratorVector.Type.VERTICAL)
                    {
                        animationName = (vector.direction.Y == 1) ? "item/move_down" : "item/move_up";
                    }
                    else
                    {
                        animationName = (vector.direction.X == 1) ? "item/move_right" : "item/move_left";
                    }

                    foreach (Vector2I coords in new GeneratorVector(vector.start, vector.end - vector.direction))
                    {
                        animationBuffer.Add(new(map.Cells[coords.Y, coords.X].Item.AnimationPlayer, animationName));
                    }
                }
            }

            await Pause(GENERATE_PAUSE_TIME);
            await PlayBufferAnimations();

            // TECHNICAL SWAPS AND SPAWNS
            foreach (GeneratorVector vector in effectiveVectors)
            {
                if (vector.direction != Vector2I.Zero)
                {
                    foreach (Vector2I coords in new GeneratorVector(vector.end, vector.start + vector.direction))
                    {
                        TechnicalSwap(coords, coords - vector.direction);
                    }
                }

                TechnicalSpawn(map.Cells[vector.start.Y, vector.start.X]);
                animationBuffer.Add(new(map.Cells[vector.start.Y, vector.start.X].Item.AnimationPlayer, "item/spawn"));
            }

            await PlayBufferAnimations();
        }

        return success;
    }

    private async Task<bool> TryGenerate()
    {
        bool isSuccesfulGenerateCenter = await TryGenerateCenter();
        bool isSuccesfulGenerateVertical = await TryGenerateVector(GeneratorVector.Type.VERTICAL);
        bool isSuccesfulGenerateHorizontal = await TryGenerateVector(GeneratorVector.Type.HORIZONTAL);

        return isSuccesfulGenerateCenter || isSuccesfulGenerateVertical || isSuccesfulGenerateHorizontal;
    }

    private async Task<bool> TryMatch(float pauseBeforeAnimation = MATCH_PAUSE_TIME)
    {
        bool success = false;
        map.CalculateMatches();

        for (int y = 0; y < Map.HEIGHT; y++)
        {
            for (int x = 0; x < Map.WIDTH; x++)
            {
                if (map.Matches[y, x] == MatchType.NONE) continue;

                if (!success)
                {
                    await Pause(pauseBeforeAnimation);
                }

                success = true;

                // @Temporary
                // Write animations to buffer
                // await PlayBufferAnimations();
                // Different scores
                // Special items

                if ((map.Matches[y, x] & MatchType.RIGHT) == MatchType.RIGHT)
                {
                    Score += SCORE_INC;
                    map.SetItem(new Vector2I(x + 0, y), ItemType.NONE);
                    map.SetItem(new Vector2I(x + 1, y), ItemType.NONE);
                    map.SetItem(new Vector2I(x + 2, y), ItemType.NONE);
                }
                if ((map.Matches[y, x] & MatchType.DOWN) == MatchType.DOWN)
                {
                    Score += SCORE_INC;
                    map.SetItem(new Vector2I(x, y + 0), ItemType.NONE);
                    map.SetItem(new Vector2I(x, y + 1), ItemType.NONE);
                    map.SetItem(new Vector2I(x, y + 2), ItemType.NONE);
                }
            }
        }
        return success;
    }

    private async Task<bool> TryMatchGenerateCycle()
    {
        await TryGenerate();

        bool isSuccesfulMatch = await TryMatch();
        while (isSuccesfulMatch)
        {
            bool isSuccesfulGenerate = await TryGenerate();
            if (!isSuccesfulGenerate) break;

            isSuccesfulMatch = await TryMatch(pauseBeforeAnimation: GENERATE_TO_MATCH_PAUSE_TIME);
        }

        return isSuccesfulMatch;
    }

    private async Task PlayBufferAnimations()
    {
        if (animationBuffer.Count == 0) return;

        float maxAnimationLength = 0;
        foreach (AnimationInfo animationInfo in animationBuffer)
        {
            float animationLength = animationInfo.player.GetAnimation(animationInfo.name).Length / animationInfo.speed;
            if (animationLength > maxAnimationLength)
            {
                maxAnimationLength = animationLength;
            }
            animationInfo.player.Play(animationInfo.name, customSpeed: animationInfo.speed);
        }
        await Pause(maxAnimationLength);
        animationBuffer.Clear();
    }

    private async Task Pause(float secs)
    {
        await ToSignal(GetTree().CreateTimer(secs), SceneTreeTimer.SignalName.Timeout);
    }

    #endregion
}
