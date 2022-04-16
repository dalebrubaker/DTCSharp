using Xunit;

namespace TestsDTC
{
    [CollectionDefinition("Logging collection")]
    public class LoggingCollection: ICollectionFixture<TestFixture>
    {
        
    }
}