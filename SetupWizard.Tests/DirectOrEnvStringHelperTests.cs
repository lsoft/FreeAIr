using System;
using FreeAIr.SetupWizard.Helper;
using Xunit;

namespace FreeAIr.SetupWizard.Tests
{
    public class DirectOrEnvStringHelperTests
    {
        [Fact]
        public void GetValue_Literal_ReturnsItUnchanged()
        {
            Assert.Equal("plain-token", DirectOrEnvStringHelper.GetValue("plain-token"));
        }

        [Fact]
        public void GetValue_EnvReference_ReadsTheVariable()
        {
            var varName = "FREEAIR_SETUPWIZARD_TEST_" + Guid.NewGuid().ToString("N");
            Environment.SetEnvironmentVariable(varName, "the-value");
            try
            {
                Assert.Equal("the-value", DirectOrEnvStringHelper.GetValue(DirectOrEnvStringHelper.MakeEnvReference(varName)));
            }
            finally
            {
                Environment.SetEnvironmentVariable(varName, null);
            }
        }

        [Theory]
        [InlineData("{$MY_VAR}", true)]
        [InlineData("plain", false)]
        [InlineData("", false)]
        [InlineData("{$MY_VAR", false)]
        [InlineData("MY_VAR}", false)]
        public void IsEnvReference_RecognizesTheForm(string value, bool expected)
        {
            Assert.Equal(expected, DirectOrEnvStringHelper.IsEnvReference(value));
        }

        [Fact]
        public void MakeEnvReference_ThenTryGetVarName_RoundTrips()
        {
            var reference = DirectOrEnvStringHelper.MakeEnvReference("MY_TOKEN");

            Assert.True(DirectOrEnvStringHelper.TryGetVarName(reference, out var varName));
            Assert.Equal("MY_TOKEN", varName);
        }

        [Fact]
        public void TryGetVarName_NonReference_ReturnsFalse()
        {
            Assert.False(DirectOrEnvStringHelper.TryGetVarName("plain-token", out var varName));
            Assert.Equal(string.Empty, varName);
        }
    }
}
