using Xunit;

namespace Tests;

[CollectionDefinition("Logging collection", DisableParallelization = true)]
public class LoggingCollection: ICollectionFixture<TestFixture>
{
    
}