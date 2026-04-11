using System.Numerics;

namespace YachtDiceMaui.Physics;

/// <summary>
/// Simple custom dice physics: ballistic motion with bounce off floor and walls.
/// Designed to be easily replaceable with a real physics engine.
/// </summary>
public class SimpleDicePhysics : IDicePhysics
{
    private const int DiceCount = 5;
    private const float DieHalfSize = 0.5f;
    private const float Gravity = -18f;
    private const float Restitution = 0.35f;
    private const float Friction = 0.92f;
    private const float AngularDamping = 0.90f;
    private const float SettleThreshold = 0.15f;
    private const float SnapToFaceThreshold = 0.3f;

    private float _trayWidth, _trayDepth, _wallHeight;
    private readonly DieBody[] _dice = new DieBody[DiceCount];
    private readonly Random _rng = new();

    // Standard d6: face normals (local space) → face values
    // Opposite faces sum to 7. Using same layout as CubeDie.cs:
    // Front(-Z)=1, Right(+X)=2, Bottom(-Y)=3, Top(+Y)=4, Left(-X)=5, Back(+Z)=6
    private static readonly (Vector3 Normal, int Value)[] FaceMap =
    {
        (new Vector3( 0,  0, -1), 1), // Front
        (new Vector3( 1,  0,  0), 2), // Right
        (new Vector3( 0, -1,  0), 3), // Bottom
        (new Vector3( 0,  1,  0), 4), // Top
        (new Vector3(-1,  0,  0), 5), // Left
        (new Vector3( 0,  0,  1), 6), // Back
    };

    public SimpleDicePhysics()
    {
        for (int i = 0; i < DiceCount; i++)
            _dice[i] = new DieBody();
    }

    public void Initialize(float trayWidth, float trayDepth, float wallHeight)
    {
        _trayWidth = trayWidth;
        _trayDepth = trayDepth;
        _wallHeight = wallHeight;
    }

    public void Roll(IReadOnlyList<int> heldIndices)
    {
        var held = new HashSet<int>(heldIndices);
        for (int i = 0; i < DiceCount; i++)
        {
            if (held.Contains(i)) continue;

            var d = _dice[i];
            d.IsHeld = false;
            d.Settled = false;

            // Spawn higher, spread out horizontally
            float maxSpreadX = _trayWidth / 2f - DieHalfSize - 0.5f;
            float spreadX = (i - 2) * 1.2f + (_rng.NextSingle() - 0.5f) * 0.6f;
            spreadX = MathF.Max(-maxSpreadX, MathF.Min(maxSpreadX, spreadX));
            float spreadZ = (_rng.NextSingle() - 0.5f) * 1.5f;
            d.Position = new Vector3(spreadX, _wallHeight + 3f + _rng.NextSingle() * 3f + i * 0.5f, spreadZ);

            // Faster downward + stronger lateral velocity
            d.Velocity = new Vector3(
                (_rng.NextSingle() - 0.5f) * 10f,
                -8f - _rng.NextSingle() * 7f,
                (_rng.NextSingle() - 0.5f) * 10f
            );

            // Much more spin for varied results
            d.AngularVelocity = new Vector3(
                (_rng.NextSingle() - 0.5f) * 35f,
                (_rng.NextSingle() - 0.5f) * 35f,
                (_rng.NextSingle() - 0.5f) * 35f
            );

            // Random initial orientation
            d.Rotation = Quaternion.CreateFromYawPitchRoll(
                _rng.NextSingle() * MathF.Tau,
                _rng.NextSingle() * MathF.Tau,
                _rng.NextSingle() * MathF.Tau
            );
        }
    }

    public void Step(float dt)
    {

        // Phase 1: Integrate motion for active dice
        for (int i = 0; i < DiceCount; i++)
        {
            var d = _dice[i];
            if (d.IsHeld || d.Settled) continue;

            // Gravity
            d.Velocity += new Vector3(0, Gravity * dt, 0);

            // Integrate position
            d.Position += d.Velocity * dt;

            // Integrate rotation (approximate: small angle)
            float angSpeed = d.AngularVelocity.Length();
            if (angSpeed > 0.001f)
            {
                var axis = Vector3.Normalize(d.AngularVelocity);
                var dq = Quaternion.CreateFromAxisAngle(axis, angSpeed * dt);
                d.Rotation = Quaternion.Normalize(dq * d.Rotation);
            }

            // Collide with floor (Y = 0)
            if (d.Position.Y - DieHalfSize < 0)
            {
                d.Position = new Vector3(d.Position.X, DieHalfSize, d.Position.Z);
                d.Velocity = new Vector3(
                    d.Velocity.X * Friction,
                    -d.Velocity.Y * Restitution,
                    d.Velocity.Z * Friction
                );
                d.AngularVelocity *= AngularDamping * 0.85f;
            }

            // Collide with walls
            float halfW = _trayWidth / 2f - DieHalfSize;
            float halfD = _trayDepth / 2f - DieHalfSize;

            if (d.Position.X < -halfW)
            {
                d.Position = new Vector3(-halfW, d.Position.Y, d.Position.Z);
                d.Velocity = new Vector3(-d.Velocity.X * Restitution, d.Velocity.Y, d.Velocity.Z * Friction);
                d.AngularVelocity *= AngularDamping;
            }
            else if (d.Position.X > halfW)
            {
                d.Position = new Vector3(halfW, d.Position.Y, d.Position.Z);
                d.Velocity = new Vector3(-d.Velocity.X * Restitution, d.Velocity.Y, d.Velocity.Z * Friction);
                d.AngularVelocity *= AngularDamping;
            }

            if (d.Position.Z < -halfD)
            {
                d.Position = new Vector3(d.Position.X, d.Position.Y, -halfD);
                d.Velocity = new Vector3(d.Velocity.X * Friction, d.Velocity.Y, -d.Velocity.Z * Restitution);
                d.AngularVelocity *= AngularDamping;
            }
            else if (d.Position.Z > halfD)
            {
                d.Position = new Vector3(d.Position.X, d.Position.Y, halfD);
                d.Velocity = new Vector3(d.Velocity.X * Friction, d.Velocity.Y, -d.Velocity.Z * Restitution);
                d.AngularVelocity *= AngularDamping;
            }
        }

        // Phase 2: Die-die collisions — purely lateral separation
        // We ONLY push dice apart in XZ. Never push upward. This prevents stacking entirely.
        float dieFull = DieHalfSize * 2f; // 1.0 — the full die width
        float minSep = dieFull + 0.05f;   // minimum XZ center-to-center distance
        
        for (int pass = 0; pass < 8; pass++)
        {
            for (int i = 0; i < DiceCount; i++)
            {
                var a = _dice[i];
                if (a.IsHeld) continue;

                for (int j = i + 1; j < DiceCount; j++)
                {
                    var b = _dice[j];
                    if (b.IsHeld) continue;

                    // Check XZ distance (ignore Y entirely for overlap detection to catch stacking)
                    float dx = a.Position.X - b.Position.X;
                    float dz = a.Position.Z - b.Position.Z;
                    float dxzSq = dx * dx + dz * dz;

                    // Also check if they're vertically close enough to interact
                    float dy = MathF.Abs(a.Position.Y - b.Position.Y);
                    if (dy > dieFull * 1.5f) continue; // too far apart vertically

                    if (dxzSq < minSep * minSep)
                    {
                        float dxz = MathF.Sqrt(dxzSq);
                        float overlap = minSep - dxz;

                        float nx, nz;
                        if (dxz > 0.01f)
                        {
                            nx = dx / dxz;
                            nz = dz / dxz;
                        }
                        else
                        {
                            // Exactly on top — deterministic push direction
                            float angle = ((i + 1) * 137.5f + (j + 1) * 79.3f + pass * 60f) % 360f * MathF.PI / 180f;
                            nx = MathF.Cos(angle);
                            nz = MathF.Sin(angle);
                            overlap = minSep;
                        }

                        float push = overlap * 0.55f;
                        bool aSettledBefore = a.Settled;
                        bool bSettledBefore = b.Settled;

                        if (a.Settled && !b.Settled)
                        {
                            b.Position -= new Vector3(nx * push * 2f, 0, nz * push * 2f);
                        }
                        else if (b.Settled && !a.Settled)
                        {
                            a.Position += new Vector3(nx * push * 2f, 0, nz * push * 2f);
                        }
                        else
                        {
                            a.Position += new Vector3(nx * push, 0, nz * push);
                            b.Position -= new Vector3(nx * push, 0, nz * push);
                        }

                        // Wake settled dice that were pushed
                        if (a.Settled) a.Settled = false;
                        if (b.Settled) b.Settled = false;

                        // Velocity impulse (lateral only)
                        float relVel = (a.Velocity.X - b.Velocity.X) * nx + (a.Velocity.Z - b.Velocity.Z) * nz;
                        if (relVel < 0)
                        {
                            float imp = relVel * (1f + Restitution) * 0.5f;
                            a.Velocity -= new Vector3(nx * imp, 0, nz * imp);
                            b.Velocity += new Vector3(nx * imp, 0, nz * imp);
                        }

                        a.AngularVelocity *= 0.85f;
                        b.AngularVelocity *= 0.85f;
                    }
                }
            }
        }

        // Clamp all dice to tray bounds after collision pushes (with velocity bounce)
        {
            float halfW = _trayWidth / 2f - DieHalfSize;
            float halfD = _trayDepth / 2f - DieHalfSize;
            for (int i = 0; i < DiceCount; i++)
            {
                var d = _dice[i];
                if (d.IsHeld) continue;
                if (d.Position.X < -halfW)
                {
                    d.Position = new Vector3(-halfW, d.Position.Y, d.Position.Z);
                    if (d.Velocity.X < 0) d.Velocity = new Vector3(-d.Velocity.X * Restitution, d.Velocity.Y, d.Velocity.Z);
                }
                else if (d.Position.X > halfW)
                {
                    d.Position = new Vector3(halfW, d.Position.Y, d.Position.Z);
                    if (d.Velocity.X > 0) d.Velocity = new Vector3(-d.Velocity.X * Restitution, d.Velocity.Y, d.Velocity.Z);
                }
                if (d.Position.Z < -halfD)
                {
                    d.Position = new Vector3(d.Position.X, d.Position.Y, -halfD);
                    if (d.Velocity.Z < 0) d.Velocity = new Vector3(d.Velocity.X, d.Velocity.Y, -d.Velocity.Z * Restitution);
                }
                else if (d.Position.Z > halfD)
                {
                    d.Position = new Vector3(d.Position.X, d.Position.Y, halfD);
                    if (d.Velocity.Z > 0) d.Velocity = new Vector3(d.Velocity.X, d.Velocity.Y, -d.Velocity.Z * Restitution);
                }
                if (d.Position.Y < DieHalfSize)
                    d.Position = new Vector3(d.Position.X, DieHalfSize, d.Position.Z);
            }
        }

        // Phase 3: Settle check
        for (int i = 0; i < DiceCount; i++)
        {
            var d = _dice[i];
            if (d.IsHeld || d.Settled) continue;

            // Never settle if still elevated (Phase 3 should have fixed it, but be safe)
            if (d.Position.Y > DieHalfSize + 0.1f)
                continue;

            if (d.Velocity.Length() < SettleThreshold &&
                d.AngularVelocity.Length() < SettleThreshold)
            {
                d.Velocity = Vector3.Zero;
                d.AngularVelocity = Vector3.Zero;
                d.Position = new Vector3(d.Position.X, DieHalfSize, d.Position.Z);
                SnapToNearestFace(d);
                d.Settled = true;
            }

        }
    }

    public bool AreSettled
    {
        get
        {
            for (int i = 0; i < DiceCount; i++)
            {
                if (_dice[i].IsHeld) continue;
                if (!_dice[i].Settled) return false;
            }
            return true;
        }
    }

    public DieState GetDieState(int index) =>
        new(_dice[index].Position, _dice[index].Rotation, _dice[index].IsHeld);

    public int GetFaceValue(int index)
    {
        var d = _dice[index];
        var up = Vector3.UnitY;
        float bestDot = -2f;
        int bestValue = 1;

        foreach (var (normal, value) in FaceMap)
        {
            var worldNormal = Vector3.Transform(normal, d.Rotation);
            float dot = Vector3.Dot(worldNormal, up);
            if (dot > bestDot)
            {
                bestDot = dot;
                bestValue = value;
            }
        }
        return bestValue;
    }

    public void SetHeld(int index, Vector3 trayPosition)
    {
        var d = _dice[index];
        d.IsHeld = true;
        d.Settled = true;
        d.Position = trayPosition;
        d.Velocity = Vector3.Zero;
        d.AngularVelocity = Vector3.Zero;
        SnapToAxisAligned(d); // Fully straighten for tidy tray appearance
    }

    public void SetUnheld(int index)
    {
        _dice[index].IsHeld = false;
        _dice[index].Settled = true; // Keep it where it is until next roll
    }

    public void NudgeDie(int index, Vector3 delta)
    {
        var d = _dice[index];
        if (d.IsHeld) return;
        d.Position += delta;
    }

    public void ForceValue(int index, int value)
    {
        var d = _dice[index];
        if (d.IsHeld) return;
        // Find the local-space normal for the desired face value
        Vector3 targetNormal = Vector3.UnitY;
        foreach (var (normal, v) in FaceMap)
        {
            if (v == value) { targetNormal = normal; break; }
        }
        // Build a rotation that maps targetNormal -> world +Y (up)
        Vector3 localRight;
        if (MathF.Abs(Vector3.Dot(targetNormal, Vector3.UnitZ)) < 0.99f)
            localRight = Vector3.Normalize(Vector3.Cross(targetNormal, Vector3.UnitZ));
        else
            localRight = Vector3.Normalize(Vector3.Cross(targetNormal, Vector3.UnitX));
        var localForward = Vector3.Cross(localRight, targetNormal);
        var mat = new Matrix4x4(
            localRight.X, targetNormal.X, localForward.X, 0,
            localRight.Y, targetNormal.Y, localForward.Y, 0,
            localRight.Z, targetNormal.Z, localForward.Z, 0,
            0, 0, 0, 1);
        d.Rotation = Quaternion.Normalize(Quaternion.CreateFromRotationMatrix(mat));
    }

    /// <summary>
    /// Snap rotation to the nearest axis-aligned face (so dice land flat).
    /// </summary>
    private static void SnapToNearestFace(DieBody d)
    {
        // Find which face normal is most pointing up
        float bestDot = -2f;
        Vector3 bestNormal = Vector3.UnitY;

        foreach (var (normal, _) in FaceMap)
        {
            var worldNormal = Vector3.Transform(normal, d.Rotation);
            float dot = Vector3.Dot(worldNormal, Vector3.UnitY);
            if (dot > bestDot)
            {
                bestDot = dot;
                bestNormal = normal;
            }
        }

        // Compute rotation that takes bestNormal to UnitY
        var currentUp = Vector3.Transform(bestNormal, d.Rotation);
        if (Vector3.Distance(currentUp, Vector3.UnitY) < 0.01f) return;

        var axis = Vector3.Cross(currentUp, Vector3.UnitY);
        if (axis.Length() < 0.001f) return;
        axis = Vector3.Normalize(axis);
        float angle = MathF.Acos(MathF.Min(1f, Vector3.Dot(currentUp, Vector3.UnitY)));
        var correction = Quaternion.CreateFromAxisAngle(axis, angle);
        d.Rotation = Quaternion.Normalize(correction * d.Rotation);
    }

    /// <summary>
    /// Snap to a fully axis-aligned rotation (no residual yaw).
    /// Used for held dice so they sit perfectly upright in the tray.
    /// </summary>
    private static void SnapToAxisAligned(DieBody d)
    {
        // Find which face normal is most pointing up
        float bestDot = -2f;
        Vector3 bestLocalUp = Vector3.UnitY;

        foreach (var (normal, _) in FaceMap)
        {
            var worldNormal = Vector3.Transform(normal, d.Rotation);
            float dot = Vector3.Dot(worldNormal, Vector3.UnitY);
            if (dot > bestDot)
            {
                bestDot = dot;
                bestLocalUp = normal;
            }
        }

        // Find which face normal is most pointing toward camera (toward +Z)
        float bestFront = -2f;
        Vector3 bestLocalFront = -Vector3.UnitZ;

        foreach (var (normal, _) in FaceMap)
        {
            // Skip the up/down axis we just chose
            if (normal == bestLocalUp || normal == -bestLocalUp) continue;
            var worldNormal = Vector3.Transform(normal, d.Rotation);
            float dot = Vector3.Dot(worldNormal, Vector3.UnitZ);
            if (dot > bestFront)
            {
                bestFront = dot;
                bestLocalFront = normal;
            }
        }

        // Build a rotation that maps bestLocalUp -> WorldY and bestLocalFront -> WorldZ
        // Third axis is the cross product
        var worldRight = Vector3.Normalize(Vector3.Cross(Vector3.UnitY, Vector3.UnitZ));
        var targetMatrix = new Matrix4x4(
            worldRight.X, worldRight.Y, worldRight.Z, 0,
            Vector3.UnitY.X, Vector3.UnitY.Y, Vector3.UnitY.Z, 0,
            Vector3.UnitZ.X, Vector3.UnitZ.Y, Vector3.UnitZ.Z, 0,
            0, 0, 0, 1);

        var localRight = Vector3.Normalize(Vector3.Cross(bestLocalUp, bestLocalFront));
        var localMatrix = new Matrix4x4(
            localRight.X, localRight.Y, localRight.Z, 0,
            bestLocalUp.X, bestLocalUp.Y, bestLocalUp.Z, 0,
            bestLocalFront.X, bestLocalFront.Y, bestLocalFront.Z, 0,
            0, 0, 0, 1);

        // Rotation = target * inverse(local)
        if (Matrix4x4.Invert(localMatrix, out var localInv))
        {
            var combined = localInv * targetMatrix;
            d.Rotation = Quaternion.Normalize(Quaternion.CreateFromRotationMatrix(combined));
        }
        else
        {
            // Fallback: just snap up-face
            SnapToNearestFace(d);
        }
    }

    private class DieBody
    {
        public Vector3 Position;
        public Vector3 Velocity;
        public Quaternion Rotation = Quaternion.Identity;
        public Vector3 AngularVelocity;
        public bool IsHeld;
        public bool Settled;
    }
}
