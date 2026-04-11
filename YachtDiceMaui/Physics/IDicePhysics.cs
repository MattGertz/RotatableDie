using System.Numerics;

namespace YachtDiceMaui.Physics;

/// <summary>
/// Abstraction over dice physics. Implement this interface to swap physics engines.
/// </summary>
public interface IDicePhysics
{
    /// <summary>
    /// Initialize with the given tray dimensions (width, depth) and wall height.
    /// </summary>
    void Initialize(float trayWidth, float trayDepth, float wallHeight);

    /// <summary>
    /// Spawn dice at starting positions, assign random velocities and angular velocities.
    /// heldIndices contains indices of dice that should NOT be rolled.
    /// </summary>
    void Roll(IReadOnlyList<int> heldIndices);

    /// <summary>
    /// Advance the simulation by dt seconds. Call this at ~60fps.
    /// </summary>
    void Step(float dt);

    /// <summary>
    /// True when all non-held dice have settled (velocity and angular velocity near zero).
    /// </summary>
    bool AreSettled { get; }

    /// <summary>
    /// Get the position and rotation of die at the given index.
    /// </summary>
    DieState GetDieState(int index);

    /// <summary>
    /// Determine which face value is showing (face pointing most upward).
    /// </summary>
    int GetFaceValue(int index);

    /// <summary>
    /// Set a die to "held" position (in the tray) at the given slot.
    /// </summary>
    void SetHeld(int index, Vector3 trayPosition);

    /// <summary>
    /// Return a die from held to the table.
    /// </summary>
    void SetUnheld(int index);

    /// <summary>
    /// Nudge a die's position by the given delta (for visibility corrections).
    /// </summary>
    void NudgeDie(int index, Vector3 delta);
}

public record struct DieState(Vector3 Position, Quaternion Rotation, bool IsHeld);
