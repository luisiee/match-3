using Godot;

public partial class BackgroundArea : Area2D
{
    [Signal] public delegate void SelectedEventHandler();

    public void Init()
    {
        Selected += GetParent<Level>().OnBackgroundAreaSelected;
    }

    private void OnInputEvent(Node viewport, InputEvent inputEvent, int shapeIdx)
    {
        if (!inputEvent.IsActionReleased("left_click")) return;
        EmitSignal(SignalName.Selected);
    }
}
