#nullable enable
using Godot;

// scripts/vehicle-motorcycle.gd 的 C# 版本（原 GDScript 文件保留）。
// 继承 C# 版 Vehicle，只重写「车身姿态」和「车轮」两个表现方法，
// 物理驱动、输入、音效全部沿用基类。
public partial class VehicleMotorcycle : Vehicle
{
	private Node3D? _motorcycle;
	private Node3D? _fork;

	private Node3D? _wheelFront;
	private Node3D? _wheelBack;

	// 生命周期

	public override void _Ready()
	{
		base._Ready(); // 先让基类取好 Sphere / Container / 音效等引用

		_motorcycle = GetNodeOrNull<Node3D>("Container/Model/motorcycle");
		_fork = GetNodeOrNull<Node3D>("Container/Model/motorcycle/body/fork");

		_wheelFront = GetNodeOrNull<Node3D>("Container/Model/motorcycle/wheel-front");
		_wheelBack = GetNodeOrNull<Node3D>("Container/Model/motorcycle/wheel-back");

		// 摩托车模型层级更深，车身节点要重新指定路径
		VehicleBody = GetNode<Node3D>("Container/Model/motorcycle/body");
	}

	// 压弯：整车绕 Z 轴倾斜（汽车只是车身侧倾，这是两者观感上最大的区别）

	protected override void EffectBody(float delta)
	{
		float targetLean = -_input.X / 5 * LinearSpeed;
		CalculatedLean = Mathf.LerpAngle(CalculatedLean, targetLean, delta * 5);

		if (_motorcycle == null || VehicleBody == null) return;

		_motorcycle.Rotation = new Vector3(
			_motorcycle.Rotation.X,
			_motorcycle.Rotation.Y,
			Mathf.LerpAngle(_motorcycle.Rotation.Z, _input.X * LinearSpeed, delta * 3)
		);

		VehicleBody.Rotation = new Vector3(
			Mathf.LerpAngle(VehicleBody.Rotation.X, -(LinearSpeed - Acceleration) / 6, delta * 10),
			VehicleBody.Rotation.Y,
			VehicleBody.Rotation.Z
		);
	}

	// 只有前后两个轮子，且转向时前叉（fork）要跟着一起转

	protected override void EffectWheels(float delta)
	{
		// 根据加速度转动车轮

		Node3D?[] wheels = { _wheelFront, _wheelBack };
		foreach (Node3D? wheel in wheels)
		{
			if (wheel == null) continue;
			wheel.Rotation = new Vector3(wheel.Rotation.X + Acceleration, wheel.Rotation.Y, wheel.Rotation.Z);
		}

		// 处理转向

		if (_wheelFront == null) return;

		if (_fork != null)
			_fork.Rotation = new Vector3(_fork.Rotation.X, Mathf.LerpAngle(_fork.Rotation.Y, -_input.X / 1.5f, delta * 5), _fork.Rotation.Z);

		_wheelFront.Rotation = new Vector3(
			_wheelFront.Rotation.X,
			Mathf.LerpAngle(_wheelFront.Rotation.Y, -_input.X / 1.5f, delta * 10),
			_wheelFront.Rotation.Z
		);
	}
}
