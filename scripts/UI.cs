using Godot;

public partial class UI : Control
{
    public Label MovesLabel { get; private set; }
    public Label ScoreLabel { get; private set; }

    public void Init()
    {
        MovesLabel = GetNode<Label>("MovesLabel");
        ScoreLabel = GetNode<Label>("ScoreLabel");
    }
}
