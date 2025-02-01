using Godot;

public partial class BackgroundArea : Area2D
{
    [Signal] public delegate void SelectedEventHandler();

    private void OnInputEvent(Node viewport, InputEvent inputEvent, int shapeIdx)
    {
        if (!inputEvent.IsActionReleased("left_click")) return;
        EmitSignal(SignalName.Selected);
    }
}
