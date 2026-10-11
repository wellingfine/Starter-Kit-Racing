using Godot;

// 跟随车辆的相机节点：位置缓动跟随车辆，并按车速拉远镜头。
[GlobalClass]
public partial class View : Node3D
{
	// Vehicle 目前仍是 GDScript 脚本（class_name Vehicle），C# 无法在编译期引用它的类型，
	// 因此这里用 Get / Call 做跨语言访问。
	// 若之后把 vehicle.gd 也转成 C#，把 Target 改为 Vehicle 类型，
	// 并直接用 Target.GetVehiclePosition() 与 Target.LinearSpeed 即可。
	private const string MethodGetVehiclePosition = "get_vehicle_position";
	private const string PropertyLinearSpeed = "linear_speed";

	[ExportGroup("Properties")]
	[Export] public Node3D Target { get; set; } = null!;

	private Camera3D _camera = null!;

	// Functions

	public override void _Ready()
	{
		_camera = GetNode<Camera3D>("Camera");
	}

	public override void _PhysicsProcess(double delta)
	{
		// Ease position towards target vehicle position

		Position = Position.Lerp(GetVehiclePosition(), (float)delta * 4);

		// Zoom camera based on the speed of the vehicle

		float speed_factor = Mathf.Clamp(Mathf.Abs(GetLinearSpeed()), 0.0f, 1.0f);
		float target_z = Mathf.Remap(speed_factor, 0.0f, 1.0f, 10.0f, 20.0f);

		_camera.Position = new Vector3(
			_camera.Position.X,
			_camera.Position.Y,
			Mathf.Lerp(_camera.Position.Z, target_z, (float)delta * 0.5f)
		);
	}

	private Vector3 GetVehiclePosition()
	{
		return Target.Call(MethodGetVehiclePosition).AsVector3();
	}

	private float GetLinearSpeed()
	{
		return Target.Get(PropertyLinearSpeed).AsSingle();
	}
}
