using Xunit;

// This project validates package integration, not parallel native safety.
// The main test suite exercises the library concurrently. Serializing this
// small consumer harness removes a pre-existing Enter-key flake that produced
// 0x01 instead of 0x0D on some fresh restores.
[assembly: CollectionBehavior(DisableTestParallelization = true)]
