using Dto;
using Xunit;

namespace FreeAIr.Mcp.Tests
{
    /// <summary>
    /// Covers <see cref="McpServerName"/>, the rule about what an MCP server may be called.
    ///
    /// It matters because the name is the prefix of every tool the server publishes, and the whole
    /// of issue #74 is the name the `add server` button used to fill in: the local date and time,
    /// which holds spaces, which providers refuse in a function name. The user is then told that
    /// some function is wrong, having never typed a function name in their life.
    /// </summary>
    public class McpServerNameTests
    {
        [Fact]
        public void TheDefaultNameCanPrefixAToolName()
        {
            //this is the bug: `DateTime.Now.ToString()` is "09.09.2026 23:45:18" or
            //"9/9/2026 11:45:18 PM" depending on the machine, and both are refused
            var name = McpServerName.CreateDefault(new DateTime(2026, 9, 9, 23, 45, 18));

            Assert.Equal("McpServer_20260909_234518", name);
            Assert.True(McpServerName.IsValid(name));
        }

        [Fact]
        public void TheDefaultNameDoesNotDependOnTheMachinesCulture()
        {
            //the name travels in a settings file which is committed to the repository, and the
            //local format is what put the spaces in it in the first place
            var moment = new DateTime(2026, 1, 2, 3, 4, 5);

            var name = RunUnderCulture("de-DE", () => McpServerName.CreateDefault(moment));

            Assert.Equal("McpServer_20260102_030405", name);
            Assert.Equal(McpServerName.CreateDefault(moment), name);
        }

        [Fact]
        public void ASecondServerAddedInTheSameSecondGetsANameOfItsOwn()
        {
            //the servers are persisted as a dictionary keyed by name, so a duplicate is not a
            //configuration which merely looks confusing
            var moment = new DateTime(2026, 9, 9, 23, 45, 18);
            var first = McpServerName.CreateDefault(moment);

            var second = McpServerName.CreateDefault(moment, new[] { first });
            var third = McpServerName.CreateDefault(moment, new[] { first, second });

            Assert.Equal("McpServer_20260909_234518_2", second);
            Assert.Equal("McpServer_20260909_234518_3", third);
        }

        [Fact]
        public void AnExistingNameIsMatchedWhateverItsCase()
        {
            var moment = new DateTime(2026, 9, 9, 23, 45, 18);

            var name = McpServerName.CreateDefault(moment, new[] { "MCPSERVER_20260909_234518" });

            Assert.Equal("McpServer_20260909_234518_2", name);
        }

        [Theory]
        [InlineData("")]
        [InlineData(null)]
        [InlineData("09.09.2026 23:45:18")]
        [InlineData("9/9/2026 11:45:18 PM")]
        [InlineData("my server")]
        [InlineData("tab\tseparated")]
        [InlineData(" leading")]
        [InlineData("trailing ")]
        public void ANameWhichCannotPrefixAToolIsRefusedAndSaysWhy(
            string? name
            )
        {
            Assert.False(McpServerName.IsValid(name));

            var problem = McpServerName.DescribeProblem(name);

            Assert.NotNull(problem);
            Assert.NotEmpty(problem);
        }

        [Theory]
        [InlineData("VS")]
        [InlineData("Github")]
        [InlineData("McpServer_20260909_234518")]
        [InlineData("my-server")]
        [InlineData("my_server")]
        //the rule stays the narrow one on purpose: the providers document [A-Za-z0-9_-], but a
        //configuration which works today must not start failing because of an upgrade
        [InlineData("my.server")]
        public void ANameWhichWorksTodayIsStillAccepted(
            string name
            )
        {
            Assert.True(McpServerName.IsValid(name));
            Assert.Null(McpServerName.DescribeProblem(name));
        }

        /// <summary>Runs the callback with the given culture installed, and puts the old one back whatever happens.</summary>
        private static T RunUnderCulture<T>(
            string culture,
            Func<T> action
            )
        {
            var previous = System.Globalization.CultureInfo.CurrentCulture;
            try
            {
                System.Globalization.CultureInfo.CurrentCulture = new System.Globalization.CultureInfo(culture);
                return action();
            }
            finally
            {
                System.Globalization.CultureInfo.CurrentCulture = previous;
            }
        }
    }
}
