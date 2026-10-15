#nullable enable
using Godot;

// 跟随车辆的相机节点：位置缓动跟随车辆，朝向跟进车头方向，并按车速拉远镜头。
[GlobalClass]
public partial class View : Node3D
{
	[ExportGroup("Properties")]
	[Export] public Vehicle Target { get; set; } = null!;

	[ExportGroup("Follow")]
	[Export] public float FollowSpeed { get; set; } = 4.0f; // 位置跟进速度
	[Export] public float TurnSpeed { get; set; } = 3.0f;   // 朝向跟进速度

	private Camera3D _camera = null!;
	private bool _aligned; // 首帧是否已对齐车头

	// Functions
	public override void _Ready()
	{
		_camera = GetNode<Camera3D>("Camera");
	}

	public override void _PhysicsProcess(double delta)
	{
		float step = (float)delta;

		// Ease position towards target vehicle position
		Position = Position.Lerp(Target.GetVehiclePosition(), step * FollowSpeed);

		// 朝向跟进车头方向
		// 相机挂在 View 的 +Z 上，所以让 View 的 +Z 指向车尾，相机才会待在车后面看向车头。
		// 只改 Y 轴（水平朝向），X 的俯角保持在场景里调好的角度。
		Vector3 forward = Target.GetVehicleForward();

		if (forward.X * forward.X + forward.Z * forward.Z > 0.001f) // 车头朝天/朝地（翻车）时算不出朝向，保持上一帧
		{
			float targetYaw = Mathf.Atan2(-forward.X, -forward.Z);

			// 首帧直接对齐，避免开局镜头自己转一大圈
			float yaw = _aligned ? Mathf.LerpAngle(Rotation.Y, targetYaw, step * TurnSpeed) : targetYaw;
			Rotation = new Vector3(Rotation.X, yaw, Rotation.Z);

			_aligned = true;
		}

		// Zoom camera based on the speed of the vehicle
		float speed_factor = Mathf.Clamp(Mathf.Abs(Target.LinearSpeed), 0.0f, 1.0f);
		float target_z = Mathf.Remap(speed_factor, 0.0f, 1.0f, 10.0f, 20.0f);

		_camera.Position = new Vector3(
			_camera.Position.X,
			_camera.Position.Y,
			Mathf.Lerp(_camera.Position.Z, target_z, step * 0.5f)
		);
	}
}
