using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Sentry.Serilog.Tests
{
    [CollectionDefinition(nameof(AspNetSentrySdkTestFixture))]
    public sealed class SentrySdkCollection : ICollectionFixture<AspNetSentrySdkTestFixture>
    {
        // This class has no code, and is never instantiated. Its purpose is simply
        // to be the place to apply [CollectionDefinition] and all the
        // ICollectionFixture<> interfaces.
        // See: https://xunit.net/docs/shared-context#collection-fixture
    }
}