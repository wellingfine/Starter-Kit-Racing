#nullable enable
using Godot;

// 车辆基类：球体代理物理、姿态表现、特效与音效。
// 原 GDScript 版 scripts/vehicle.gd 已移除，这是唯一的 Vehicle 实现。
// 摩托车（VehicleMotorcycle）继承本类，物理驱动、输入、音效全部沿用这里。
[GlobalClass]
public partial class Vehicle : Node3D
{
	// 节点

	private RigidBody3D _sphere = null!;
	private RayCast3D _raycast = null!;

	// 车辆部件

	private Node3D _vehicleModel = null!;
	protected Node3D? VehicleBody;

	// 车轮（可选）

	protected Node3D? WheelFl;
	protected Node3D? WheelFr;
	protected Node3D? WheelBl;
	protected Node3D? WheelBr;

	// 特效

	protected GpuParticles3D? TrailLeft;
	protected GpuParticles3D? TrailRight;

	// 音效

	private AudioStreamPlayer3D _screechSound = null!;
	private AudioStreamPlayer3D _engineSound = null!;
	private AudioStreamPlayer3D _impactSound = null!;

	// 车辆状态：输入、地面法线、速度与姿态等

	protected Vector3 _input;
	protected Vector3 Normal;

	public float LinearSpeed;
	protected float Acceleration;
	protected float AngularSpeed;
	protected bool Colliding;
	protected Vector3 LinearVelocity;
	protected Vector3 PrevPosition;
	protected float CalculatedLean;

	// 生命周期

	public override void _Ready()
	{
		_sphere = GetNode<RigidBody3D>("Sphere");
		_raycast = GetNode<RayCast3D>("Ground");

		_vehicleModel = GetNode<Node3D>("Container");
		VehicleBody = GetNodeOrNull<Node3D>("Container/Model/body");

		WheelFl = GetNodeOrNull<Node3D>("Container/Model/wheel-front-left");
		WheelFr = GetNodeOrNull<Node3D>("Container/Model/wheel-front-right");
		WheelBl = GetNodeOrNull<Node3D>("Container/Model/wheel-back-left");
		WheelBr = GetNodeOrNull<Node3D>("Container/Model/wheel-back-right");

		TrailLeft = GetNodeOrNull<GpuParticles3D>("Container/TrailLeft");
		TrailRight = GetNodeOrNull<GpuParticles3D>("Container/TrailRight");

		_screechSound = GetNode<AudioStreamPlayer3D>("Container/ScreechSound");
		_engineSound = GetNode<AudioStreamPlayer3D>("Container/EngineSound");
		_impactSound = GetNode<AudioStreamPlayer3D>("Container/ImpactSound");
	}

	// 公开方法

	public Vector3 GetVehiclePosition() => _vehicleModel.GlobalPosition;

	public override void _PhysicsProcess(double delta)
	{
		float step = (float)delta;

		HandleInput(step);

		float direction = Mathf.Sign(LinearSpeed);
		if (direction == 0) direction = Mathf.Abs(_input.Z) > 0.1f ? Mathf.Sign(_input.Z) : 1;

		float steeringGrip = Mathf.Clamp(Mathf.Abs(LinearSpeed), 0.2f, 1.0f);

		float targetAngular = -_input.X * steeringGrip * 4 * direction;
		AngularSpeed = Mathf.Lerp(AngularSpeed, targetAngular, step * 4);

		_vehicleModel.RotateY(AngularSpeed * step);

		// 与地面法线对齐

		if (_raycast.IsColliding())
		{
			if (!Colliding)
			{
				if (VehicleBody != null) VehicleBody.Position = new Vector3(0, 0.1f, 0); // 落地回弹
				_input.Z = 0;
			}

			Normal = _raycast.GetCollisionNormal();

			// 让车身朝向贴合碰撞法线

			if (Normal.Dot(_vehicleModel.GlobalBasis.Y) > 0.5f)
			{
				Transform3D xform = AlignWithY(_vehicleModel.GlobalTransform, Normal);
				_vehicleModel.GlobalTransform = _vehicleModel.GlobalTransform.InterpolateWith(xform, 0.2f).Orthonormalized();
			}
		}

		Colliding = _raycast.IsColliding();

		float targetSpeed = _input.Z;

		if (targetSpeed < 0 && LinearSpeed > 0.01f)
			LinearSpeed = Mathf.Lerp(LinearSpeed, 0.0f, step * 8);
		else if (targetSpeed < 0)
			LinearSpeed = Mathf.Lerp(LinearSpeed, targetSpeed / 2, step * 2);
		else
			LinearSpeed = Mathf.Lerp(LinearSpeed, targetSpeed, step * 6);

		Acceleration = Mathf.Lerp(Acceleration, LinearSpeed + (Mathf.Abs(_sphere.AngularVelocity.Length() * LinearSpeed) / 100), step);

		// 让车辆模型跟随物理球体

		_vehicleModel.Position = _sphere.Position - new Vector3(0, 0.65f, 0);
		_raycast.Position = _sphere.Position;

		// 计算车辆模型的线速度

		LinearVelocity = (_vehicleModel.Position - PrevPosition) / step;
		PrevPosition = _vehicleModel.Position;

		// 视觉与音效表现

		EffectEngine(step);
		EffectBody(step);
		EffectWheels(step);
		EffectTrails();
	}

	// 车辆着地时才处理输入

	protected virtual void HandleInput(float delta)
	{

		if (_raycast.IsColliding())
		{
			_input.X = Input.GetAxis("left", "right");
			_input.Z = Input.GetAxis("back", "forward");
		}

		_sphere.AngularVelocity += _vehicleModel.GlobalTransform.Basis.X * (LinearSpeed * 100) * delta;
	}

	protected virtual void EffectBody(float delta)
	{
		CalculatedLean = Mathf.LerpAngle(CalculatedLean, -_input.X / 5 * LinearSpeed, delta * 5);

		// 根据加速度和转向，让车身轻微倾斜（并位移）

		if (VehicleBody != null)
		{
			VehicleBody.Rotation = new Vector3(
				Mathf.LerpAngle(VehicleBody.Rotation.X, -(LinearSpeed - Acceleration) / 6, delta * 10),
				VehicleBody.Rotation.Y,
				CalculatedLean
			);

			VehicleBody.Position = VehicleBody.Position.Lerp(new Vector3(0, 0.2f, 0), delta * 5);
		}
	}

	protected virtual void EffectWheels(float delta)
	{
		// 根据加速度转动车轮

		Node3D?[] wheels = { WheelFl, WheelFr, WheelBl, WheelBr };
		foreach (Node3D? wheel in wheels)
		{
			if (wheel == null) continue;
			wheel.Rotation = new Vector3(wheel.Rotation.X + Acceleration, wheel.Rotation.Y, wheel.Rotation.Z);
		}

		// 根据转向方向转动前轮

		if (WheelFl != null)
			WheelFl.Rotation = new Vector3(WheelFl.Rotation.X, Mathf.LerpAngle(WheelFl.Rotation.Y, -_input.X / 1.5f, delta * 10), WheelFl.Rotation.Z);

		if (WheelFr != null)
			WheelFr.Rotation = new Vector3(WheelFr.Rotation.X, Mathf.LerpAngle(WheelFr.Rotation.Y, -_input.X / 1.5f, delta * 10), WheelFr.Rotation.Z);
	}

	// 引擎音效

	protected virtual void EffectEngine(float delta)
	{
		float speedFactor = Mathf.Clamp(Mathf.Abs(LinearSpeed), 0.0f, 1.0f);
		float throttleFactor = Mathf.Clamp(Mathf.Abs(_input.Z), 0.0f, 1.0f);

		float targetVolume = Mathf.Remap(speedFactor + (throttleFactor * 0.5f), 0.0f, 1.5f, -15.0f, -5.0f);
		_engineSound.VolumeDb = Mathf.Lerp(_engineSound.VolumeDb, targetVolume, delta * 5.0f);

		float targetPitch = Mathf.Remap(speedFactor, 0.0f, 1.0f, 0.5f, 3.0f);
		if (throttleFactor > 0.1f) targetPitch += 0.2f;

		_engineSound.PitchScale = Mathf.Lerp(_engineSound.PitchScale, targetPitch, delta * 2.0f);
	}

	// 显示漂移拖痕（并播放打滑音效）

	protected virtual void EffectTrails()
	{
		float driftIntensity = Mathf.Abs(LinearSpeed - Acceleration) + (Mathf.Abs(CalculatedLean) * 2.0f);
		bool shouldEmit = driftIntensity > 0.25f;

		if (TrailLeft != null) TrailLeft.Emitting = shouldEmit;
		if (TrailRight != null) TrailRight.Emitting = shouldEmit;

		float targetVolume = -80.0f;
		if (shouldEmit) targetVolume = Mathf.Remap(Mathf.Clamp(driftIntensity, 0.25f, 2.0f), 0.25f, 2.0f, -10.0f, 0.0f);

		_screechSound.PitchScale = Mathf.Lerp(_screechSound.PitchScale, Mathf.Clamp(Mathf.Abs(LinearSpeed), 1.0f, 3.0f), 0.1f);
		_screechSound.VolumeDb = Mathf.Lerp(_screechSound.VolumeDb, targetVolume, 10.0f * (float)GetPhysicsProcessDeltaTime());
	}

	// 让车辆的 Y 轴对齐指定法线

	protected static Transform3D AlignWithY(Transform3D xform, Vector3 newY)
	{
		Basis basis = xform.Basis;
		basis.Row1 = newY;
		basis.Row0 = -basis.Row2.Cross(newY);
		basis = basis.Orthonormalized();
		xform.Basis = basis;

		return xform;
	}

	// 检测碰撞并播放撞击音效
	// 方法名保持与场景里的信号连接一致：_on_sphere_body_entered

	public void _on_sphere_body_entered(Node body)
	{
		if (VehicleBody == null) return;

		if (!_impactSound.Playing)
		{
			float impactVelocity = Mathf.Abs(LinearVelocity.Dot(VehicleBody.GlobalBasis.Z));
			_impactSound.VolumeDb = Mathf.Clamp(Mathf.Remap(impactVelocity, 0.0f, 6.0f, -20.0f, 0.0f), -20.0f, 0.0f);
			_impactSound.Play();
		}
	}
}
