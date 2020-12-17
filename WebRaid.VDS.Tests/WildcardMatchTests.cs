using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Test.Extensions.Logging;
using NSubstitute;
using FluentAssertions;
using NUnit.Framework;

namespace WebRaid.VDS.Tests
{
    public class WildcardMatchTests : BasisTestKlasse
    {
        public WildcardMatchTests() : base(LogLevel.Trace)
        {
            WildcardMatch.Logger = Lf.Logger<WildcardMatchTests>();
        }

        [TestCaseSource(nameof(TestCases))]
        public void EqualsWildcard(string text, string pattern, bool ignoreCase, bool erwartet)
        {
            text.EqualsWildcard(pattern, ignoreCase).Should().Be(erwartet);
        }

        [TestCaseSource(nameof(TestCases))]
        public void EqualsWildcardRegular(string text, string pattern, bool ignoreCase, bool erwartet)
        {
            text.EqualsWildcardRegular(pattern, ignoreCase).Should().Be(erwartet);
        }


        public static IEnumerable<TestCaseData> TestCases()
        {
            yield return new TestCaseData("123", "12", true, false);
            yield return new TestCaseData("123", "12N", true, false);
            yield return new TestCaseData("1234", "12?N", true, false);
            yield return new TestCaseData("1234", "1N*4", true, false);
            yield return new TestCaseData("123", "12*N", true, false);
            yield return new TestCaseData("123", "*12", true, false);
            yield return new TestCaseData("123", "12*", true, true);
            yield return new TestCaseData("123", "123", true, true);
            yield return new TestCaseData("123", "1?3", true, true);
            yield return new TestCaseData("123", "1*3", true, true);
            yield return new TestCaseData("123", "*23", true, true);
            yield return new TestCaseData("1xx23", "*23", true, true);
        }
    }
}