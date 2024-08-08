namespace Multithreading_Library.ThreadControl.States;

/// <summary>
/// defines the state of a process
/// </summary>
public enum ExecutionState
{
    /// <summary>
    /// the process is currently idle
    /// </summary>
    Idle = 0,
    /// <summary>
    /// the process is currently executing
    /// </summary>
    Executing = 1,
}