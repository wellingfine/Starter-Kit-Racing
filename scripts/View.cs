#nullable enable
using Godot;

// 跟随车辆的相机节点：位置缓动跟随车辆，并按车速拉远镜头。
[GlobalClass]
public partial class View : Node3D
{
	[ExportGroup("Properties")]
	[Export] public Vehicle Target { get; set; } = null!;

	private Camera3D _camera = null!;

	// Functions

	public override void _Ready()
	{
		_camera = GetNode<Camera3D>("Camera");
	}

	public override void _PhysicsProcess(double delta)
	{
		// Ease position towards target vehicle position

		Position = Position.Lerp(Target.GetVehiclePosition(), (float)delta * 4);

		// Zoom camera based on the speed of the vehicle

		float speed_factor = Mathf.Clamp(Mathf.Abs(Target.LinearSpeed), 0.0f, 1.0f);
		float target_z = Mathf.Remap(speed_factor, 0.0f, 1.0f, 10.0f, 20.0f);

		_camera.Position = new Vector3(
			_camera.Position.X,
			_camera.Position.Y,
			Mathf.Lerp(_camera.Position.Z, target_z, (float)delta * 0.5f)
		);
	}
}
