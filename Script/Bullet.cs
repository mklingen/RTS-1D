using Godot;
using System;

public partial class Bullet : Node3D
{
    [Export] public WeaponStats WeaponStats;
    // Maximum time the bullet will remain alive before being destroyed.
    [Export] public float MaxLifeTime = 1.5f;
    // Initial speed of the bullet.
    [Export] public float BulletSpeed = 1.0f;
    // Size of the bullet for calculating collisions.
    [Export] public float Radius = 0.01f;
    // Maximum distance to the target in which we will still damage it.
    [Export] public float MaxDistanceForDamage = 0.1f;
    // When the bullet dies, it creates this explosion.
    [Export] private PackedScene ExplosionPrefab;
    [Export] private BulletStats stats;
    [ExportGroup("Animation")]
    [Export] private Curve heightCurve;
    [Export] private Curve distCurve;
    [Export] private float arcAmount = 1.0f;

    private struct AnimationState
    {
        public Vector3 startPos;
        public Vector3 endPos;
        public Planet planet;
        private Vector3 delta;
        public float t0;
        public float totalTime;
        public float totalArcHeight;

        public AnimationState(Vector3 start, Vector3 end, Planet planet, float totalT, float height)
        {
            startPos = start;
            endPos = end;
            this.planet = planet;
            totalTime = totalT;
            t0 = Game.GetTime();
            delta = (endPos - startPos);
            totalArcHeight = height;
        }
        
        public Vector3 GetPosition(Curve heightCurve, Curve distCurve, float t)
        {
            float alpha = (t - t0) / totalTime;
            float height = heightCurve.SampleBaked(alpha);
            float dist = distCurve.SampleBaked(alpha);
            Vector3 along = dist * delta + startPos;
            Planet.TangentFrame tangent = planet.GetTangentFrame(along);
            Vector3 above = tangent.Up * height * totalArcHeight;
            return along + above;
        }

        public Vector3 GetVelocity(Curve heightCurve, Curve distCurve, float t, float speed)
        {
            Vector3 p0 = GetPosition(heightCurve, distCurve, t);
            Vector3 p1 = GetPosition(heightCurve, distCurve, t + (float)1e-1 * totalTime);
            return (p1 - p0).Normalized() * speed;
        }

        public bool IsDone(float t)
        {
            return t - t0 > totalTime;
        }
    }
    private AnimationState animationState;
    private float timeLaunched = 0;
    private Planet planet;

    private Vector3 target;
    private Node3D targetNode;

    public void Launch(Vector3 from, Vector3 to, Node3D targetNode = null)
    {
        this.targetNode = targetNode;
        target = to;
        GlobalPosition = from;
        float len = (to - from).Length();
        if (planet == null) {
            planet = Game.Get().GetPlanet();
        }
        Vector3 halfPoint = (from + to) * 0.5f;
        Vector3 projHalfPoint = planet.ProjectToCylinder(halfPoint, 0.0f, 0.0f);
        float heightDiff = (halfPoint - projHalfPoint).Length();
        if (len < 0.1) {
            heightDiff = 0;
        }
        animationState = new AnimationState(from, to, planet, len / BulletSpeed, arcAmount * heightDiff);
        // Set the launch time
        timeLaunched = Game.GetTime();
        LookAt(animationState.GetPosition(heightCurve, distCurve, 0.5f * animationState.totalTime) + Vector3.Up * 0.01f, (GlobalPosition - planet.GlobalPosition).Normalized());
    }

    public override void _Process(double delta)
    {
        base._Process(delta);
        float t = Game.GetTime();
        if (Game.GetTime() > timeLaunched + MaxLifeTime) {
            Explode();
        }
        GlobalPosition = animationState.GetPosition(heightCurve, distCurve, t);
        Vector3 velocity = animationState.GetVelocity(heightCurve, distCurve, t, BulletSpeed);
        if (planet == null) {
            planet = Game.Get().GetPlanet();
        } else {
            if (planet.Collides(GlobalPosition)) {
                Explode();
            }
        }

        if ((GlobalPosition - target).Length() < Radius || animationState.IsDone(t)) {
            Explode();
        }
        if (velocity.LengthSquared() > 1e-2) {
            LookAt(GlobalPosition + velocity.Normalized() * 10 + Vector3.Up * 0.01f, (GlobalPosition - planet.GlobalPosition).Normalized());
        }
    }

    public void Explode()
    {
        if (ExplosionPrefab != null) {
            Node explosion = ExplosionPrefab.Instantiate();
            Node3D explosion3D = explosion as Node3D;
            if (explosion3D != null) {
                explosion3D.GlobalTransform = GlobalTransform;
            }
            Game.Get().AddChild(explosion);
        }
        if (targetNode != null && (targetNode.NativeInstance.ToInt64() != 0x0) &&
            (targetNode.GlobalPosition - GlobalPosition).Length() < MaxDistanceForDamage) {

            foreach (Game.IDamageable damageables in Game.FindInterfacesRecursive<Game.IDamageable>(targetNode)) {
                damageables.DoDamage(WeaponStats.DamagePerShot, WeaponStats.ArmorPiercing);
            }
        }
        QueueFree();

    }

}
