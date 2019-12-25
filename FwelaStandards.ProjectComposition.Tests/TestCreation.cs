using System;
using Xunit;

namespace FwelaStandards.ProjectComposition.Tests
{
    public class TestCreation
    {
        [Fact]
        public void Test1()
        {
            var root = new MockRootPart();
            root.InitFromParent(null,null);
        }
    }
}
