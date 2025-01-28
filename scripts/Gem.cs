using System;
using Godot;

public partial class Gem : RigidBody2D
{
    [Export] public CompressedTexture2D[] SpriteTextures { get; set; } = Array.Empty<CompressedTexture2D>();

    public override void _Ready()
    {
        Image image;
        if (SpriteTextures.Length == 0)
        {
            image = Image.LoadFromFile("res://sprites/default.png");
        }
        else
        {
            image = SpriteTextures[GD.Randi() % SpriteTextures.Length].GetImage();
        }

        ImageTexture texture = ImageTexture.CreateFromImage(image);
        GetNode<Sprite2D>("Sprite2D").Texture = texture;
    }
}
