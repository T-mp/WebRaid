using FluentAssertions;
using NUnit.Framework;
using WebRaid.Abstraction.VDS;

namespace WebRaid.Abstraction.Tests
{
    public class PfadTests
    {
        [Test]
        public void DirectorySeparatorChar()
        {
            Pfad.DirectorySeparatorChar.Should().Be('/');
        }

        [Test]
        public void GetDirectoryNameEmpty()
        {
            var result = new Pfad("").GetDirectoryName();
            result.Should().BeNull();
        }
        [Test]
        public void GetDirectoryNameWhiteSpaces()
        {
            var result = new Pfad("  \t").GetDirectoryName();
            result.Should().BeNull();
        }
        [Test]
        public void GetDirectoryNameNull()
        {
            var result = new Pfad(null).GetDirectoryName();
            result.Should().BeNull();
        }
        [Test]
        public void GetDirectoryNameRootOrdner()
        {
            var result = new Pfad("/").GetDirectoryName();
            result.Should().Be("/");
        }
        [Test]
        public void GetDirectoryNameRootDatei()
        {
            var result = new Pfad("/Test").GetDirectoryName();
            result.Should().Be("/");
        }
        [Test]
        public void GetDirectoryNameOrdner1()
        {
            var result = new Pfad("/Test/").GetDirectoryName();
            result.Should().Be("/Test");
        }
        [Test]
        public void GetDirectoryNameOrdner1Datei()
        {
            var result = new Pfad("/Test/Datei").GetDirectoryName();
            result.Should().Be("/Test");
        }

        [Test]
        public void GetFileNameEmpty()
        {
            var result = new Pfad("").GetFileName();
            result.Should().BeNull();
        }
        [Test]
        public void GetFileNameWhiteSpaces()
        {
            var result = new Pfad("  \t").GetFileName();
            result.Should().BeNull();
        }
        [Test]
        public void GetFileNameNull()
        {
            var result = new Pfad(null).GetFileName();
            result.Should().BeNull();
        }
        [Test]
        public void GetFileNameRootOrdner()
        {
            var result = new Pfad("/").GetFileName();
            result.Should().Be("");
        }
        [Test]
        public void GetFileNameRootDatei()
        {
            var result = new Pfad("/Datei").GetFileName();
            result.Should().Be("Datei");
        }
        [Test]
        public void GetFileNameRootDateiMitExtension()
        {
            var result = new Pfad("/Datei.Extension").GetFileName();
            result.Should().Be("Datei.Extension");
        }
        [Test]
        public void GetFileNameOrdner1()
        {
            var result = new Pfad("/Test/").GetFileName();
            result.Should().Be("");
        }
        [Test]
        public void GetFileNameOrdner1Datei()
        {
            var result = new Pfad("/Test/Datei").GetFileName();
            result.Should().Be("Datei");
        }

        [Test]
        public void GetFileNameWithoutExtensionEmpty()
        {
            var result = new Pfad("").GetFileNameWithoutExtension();
            result.Should().BeNull();
        }
        [Test]
        public void GetFileNameWithoutExtensionWhiteSpaces()
        {
            var result = new Pfad("  \t").GetFileNameWithoutExtension();
            result.Should().BeNull();
        }
        [Test]
        public void GetFileNameWithoutExtensionNull()
        {
            var result = new Pfad(null).GetFileNameWithoutExtension();
            result.Should().BeNull();
        }
        [Test]
        public void GetFileNameWithoutExtensionRootOrdner()
        {
            var result = new Pfad("/").GetFileNameWithoutExtension();
            result.Should().Be("");
        }
        [Test]
        public void GetFileNameWithoutExtensionRootDatei()
        {
            var result = new Pfad("/Datei").GetFileNameWithoutExtension();
            result.Should().Be("Datei");
        }
        [Test]
        public void GetFileNameWithoutExtensionRootDateiMitExtension()
        {
            var result = new Pfad("/Datei.Extension").GetFileNameWithoutExtension();
            result.Should().Be("Datei");
        }
        [Test]
        public void GetFileNameWithoutExtensionOrdner1()
        {
            var result = new Pfad("/Test/").GetFileNameWithoutExtension();
            result.Should().Be("");
        }
        [Test]
        public void GetFileNameWithoutExtensionOrdner1Datei()
        {
            var result = new Pfad("/Test/Datei").GetFileNameWithoutExtension();
            result.Should().Be("Datei");
        }
        [Test]
        public void GetFileNameWithoutExtensionOrdner1DateiMitExtension()
        {
            var result = new Pfad("/Test/Datei.Extension").GetFileNameWithoutExtension();
            result.Should().Be("Datei");
        }

        [Test]
        public void GetExtensionEmpty()
        {
            var result = new Pfad("").GetExtension();
            result.Should().BeNull();
        }
        [Test]
        public void GetExtensionWhiteSpaces()
        {
            var result = new Pfad("  \t").GetExtension();
            result.Should().BeNull();
        }
        [Test]
        public void GetExtensionNull()
        {
            var result = new Pfad(null).GetExtension();
            result.Should().BeNull();
        }
        [Test]
        public void GetExtensionRootOrdner()
        {
            var result = new Pfad("/").GetExtension();
            result.Should().Be("");
        }
        [Test]
        public void GetExtensionRootDatei()
        {
            var result = new Pfad("/Test.test").GetExtension();
            result.Should().Be(".test");
        }
        [Test]
        public void GetExtensionRootDateiOhneExtension()
        {
            var result = new Pfad("/Test").GetExtension();
            result.Should().Be("");
        }
        [Test]
        public void GetExtensionOrdner1()
        {
            var result = new Pfad("/Test/").GetExtension();
            result.Should().Be("");
        }
        [Test]
        public void GetExtensionOrdner1Datei()
        {
            var result = new Pfad("/Test/Datei.test").GetExtension();
            result.Should().Be(".test");
        }
    }
}