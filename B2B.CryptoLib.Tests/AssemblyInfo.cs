using Xunit.Sdk;
using Xunit.v3;

// Tests mutate process-wide crypto configuration and use shared key-set fixtures;
// this is the xUnit v3 equivalent of the former v2 DisableTestParallelization setting.
[assembly: Parallelization(Mode = ParallelMode.None)]
