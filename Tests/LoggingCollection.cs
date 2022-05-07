using Xunit;

namespace Tests;

/// <summary>
/// Used to make tests not-parallel and use only 1 static logger
/// </summary>
[CollectionDefinition("Logging collection", DisableParallelization = true)]
public class LoggingCollection: ICollectionFixture<TestFixture>
{
    
}