using Godot;
using System;

public partial class Player : Node3D
{
    private const float MOVE_SPEED = 7;
    private const float SPRINT_FACTOR = 7f;
    private const float REACH_DISTANCE = 20f;
    private const float SENSITIVITY = 0.002f;

    [Export]
    private Node3D camera = null;

    [Export]
    private World world = null;

    [Export]
    private Node3D cube = null;

    private bool mouseLocked = false;
    private Vector2 cameraRotation = Vector2.Zero;

    public override void _Process(double delta)
    {
        if (Input.IsActionJustPressed("lock_mouse"))
        {
            this.mouseLocked = !this.mouseLocked;
            Input.MouseMode = this.mouseLocked
                ? Input.MouseModeEnum.Captured
                : Input.MouseModeEnum.Visible;
        }

        this.cube.Position = this.world.Raytrace(
            this.camera.GlobalPosition,
            -this.camera.Basis.Z,
            REACH_DISTANCE
        );

        // TODO world raytrace returns a result a Vector3 in the middle of the block, not a Vector3I of the block pos.
        // Update it to return a block pos so we don't have to floor the values.
        if (Input.IsActionJustPressed("break_block"))
        {
            world.SetBlockAt(
                (int)Mathf.Floor(this.cube.Position.X),
                (int)Mathf.Floor(this.cube.Position.Y),
                (int)Mathf.Floor(this.cube.Position.Z),
                Blocks.AIR
            );
        }
    }

    public override void _Input(InputEvent @event)
    {
        if (this.mouseLocked && @event is InputEventMouseMotion)
        {
            var mouseEvent = (InputEventMouseMotion)@event;
            this.cameraRotation -= mouseEvent.Relative * SENSITIVITY;
            this.cameraRotation = new Vector2(
                this.cameraRotation.X,
                Mathf.Clamp(this.cameraRotation.Y, Mathf.DegToRad(-90), Mathf.DegToRad(90))
            );
            this.camera.Rotation = new Vector3(this.cameraRotation.Y, this.cameraRotation.X, 0);
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        // TODO breaking and placing blocks! player movement!



        this.world.Raytrace(this.camera.Position, this.camera.Basis.Z, 10);

        var moveDirection = Vector3.Zero;

        if (Input.IsActionPressed("move_forward"))
        {
            moveDirection += Vector3.Forward.Rotated(Vector3.Up, this.cameraRotation.X);
        }

        if (Input.IsActionPressed("move_backward"))
        {
            moveDirection += Vector3.Back.Rotated(Vector3.Up, this.cameraRotation.X);
        }

        if (Input.IsActionPressed("move_left"))
        {
            moveDirection += Vector3.Left.Rotated(Vector3.Up, this.cameraRotation.X);
        }

        if (Input.IsActionPressed("move_right"))
        {
            moveDirection += Vector3.Right.Rotated(Vector3.Up, this.cameraRotation.X);
        }

        if (Input.IsActionPressed("jump"))
        {
            moveDirection += Vector3.Up;
        }

        if (Input.IsActionPressed("crouch"))
        {
            moveDirection += Vector3.Down;
        }

        moveDirection = moveDirection.Normalized();
        var sprinting = Input.IsActionPressed("sprint");
        var moveSpeed = MOVE_SPEED * (sprinting ? SPRINT_FACTOR : 1);

        this.Translate(moveDirection * moveSpeed * (float)delta);
    }
}
