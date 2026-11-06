using Xunit;

// The harness owns one in-memory SQLite database per factory instance and the repository layer serializes writes through a single tenant queue,
// so tests must not run in parallel against it.
[assembly: CollectionBehavior(DisableTestParallelization = true)]
