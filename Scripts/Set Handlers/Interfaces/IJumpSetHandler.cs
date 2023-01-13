public interface IJumpSetHandler
{
    IJumpInputter JumpInputter { get; }
    void InitialiseJumpController();
    void ApplyGravitationalForces();
}