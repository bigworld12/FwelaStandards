using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using Xunit;

namespace FwelaStandards.ProjectComposition.Tests
{
    public class TestDepNotification
    {
        [Fact]
        public void Test1()
        {
            var root = new MockRootPart();
            bool doesNotify = false;
            root.PropertyChanged += (_, args) =>
            {
                if (args.PropertyName == nameof(root.ValueSum))
                {
                    doesNotify = true;
                }
            };

            var rootNode = root.InitFromParent(null);
            Assert.NotNull(root.PartA);
            Assert.Equal(0, root.ValueSum);
            var pANode = rootNode.GetChild(nameof(root.PartA));
            var pA = pANode.GetPartAs<PartAType>();
            var actualPartB = new ListPartB();
            pA.ListOfB.Add(actualPartB);
            Assert.Single(pA.ListOfB);
            Assert.Single(pANode.AsList);
            Assert.Same(pANode.AsList[0], actualPartB.NodeInfo);
            Assert.Same(pA.ListOfB[0], actualPartB);
            var actualC = new ListPartC();
            actualPartB.ListOfC.Add(actualC);
            actualC.ListOfD.Add(new ListPartD() { Value = 10 });
            actualC.ListOfD.Add(new ListPartD() { Value = 20 });
            Assert.Equal(30, root.ValueSum);

            //#1 test
            actualC.ListOfD.Add(new ListPartD());
            doesNotify = false;
            actualC.ListOfD[actualC.ListOfD.Count - 1].Value = 30;
            Assert.True(doesNotify);

            //#2 test
            var actualC2 = new ListPartC();
            doesNotify = false;
            actualPartB.ListOfC.Add(actualC);
            Assert.True(doesNotify);

            actualC2.ListOfD.Add(new ListPartD() { Value = 100 });
            doesNotify = false;
            actualPartB.ListOfC.Add(actualC2);
            Assert.True(doesNotify);
            //Assert.False(true);
        }
    }
}
