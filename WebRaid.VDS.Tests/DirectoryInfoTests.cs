using System.IO;
using Microsoft.Extensions.Logging;
using NUnit.Framework;
using Test.Extensions.Logging;
using NSubstitute;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using WebRaid.Abstraction.Speicher;
using WebRaid.VDS.JsonConverter;

namespace WebRaid.VDS.Tests
{
    public class DirectoryInfoTests : BasisTestKlasse
    {
        public DirectoryInfoTests() : base(LogLevel.Trace)
        {
            JsonConverterFileSystemInfoReadDto.Logger = Lf.Logger<JsonConverterFileSystemInfoReadDto>();
            JsonConverterFileSystemInfo.Logger = Lf.Logger<JsonConverterFileSystemInfo>();
        }

        [Test]
        public void GetRoot_verwendetAdressenGenerator()
        {
            var node = Substitute.For<INode>();
            var adressenGenerator = Substitute.For<IFileAdressenGenerator>();
            adressenGenerator.GetNew().Returns("Test/Dir1");

            var rootDir = DirectoryInfo.GetRoot(node, "/Test", adressenGenerator, Lf.Logger<DirectoryInfo>());

            adressenGenerator.Received(1).GetNew();
            rootDir.Adresse.Should().Be("Test/Dir1");
        }

        [Test]
        public void GetRoot_SchreibtSichAufNode()
        {
            var configuration = Substitute.For<IConfiguration>();
            configuration["Name"].Returns("TestMemoryNode1");

            var node = new Node.Memory.Node(configuration, Lf.Logger<Node.Memory.Node>(LogLevel.Warning));
            var adressenGenerator = Substitute.For<IFileAdressenGenerator>();
            adressenGenerator.GetNew().Returns("Test/Dir1");

            _ = DirectoryInfo.GetRoot(node, "/Test", adressenGenerator, Lf.Logger<DirectoryInfo>());

            var stream = node.Get("Test/Dir1");

            var streamReader = new StreamReader(stream.Result);
            streamReader.ReadToEnd().Should().Be("{\"type\":\"DirectoryInfo\",\"FullName\":\"/Test\",\"Adresse\":\"Test/Dir1\",\"Properties\":[],\"Inhalt\":{}}");
        }

        [Test]
        public void GetRoot_KonstruktorLiestVomNode()
        {
            var configuration = Substitute.For<IConfiguration>();
            configuration["Name"].Returns("TestMemoryNode1");

            var node = new Node.Memory.Node(configuration, Lf.Logger<Node.Memory.Node>(LogLevel.Warning));
            var adressenGenerator = Substitute.For<IFileAdressenGenerator>();
            adressenGenerator.GetNew().Returns("Test/Dir1");

            _ = DirectoryInfo.GetRoot(node, "/Test", adressenGenerator, Lf.Logger<DirectoryInfo>());

            TesteNodeInhalt(node, "Test/Dir1", "{\"type\":\"DirectoryInfo\",\"FullName\":\"/Test\",\"Adresse\":\"Test/Dir1\",\"Properties\":[],\"Inhalt\":{}}");

            var dir = new DirectoryInfo(node, "Test/Dir1", adressenGenerator, Lf.Logger<DirectoryInfo>());

            dir.FullName.Should().Be("/Test");
        }

        [Test]
        public void GetRoot_CreateSubdirectorySchreibtSichAufNode()
        {
            var configuration = Substitute.For<IConfiguration>();
            configuration["Name"].Returns("TestMemoryNode1");

            var node = new Node.Memory.Node(configuration, Lf.Logger<Node.Memory.Node>(LogLevel.Warning));
            var adressenGenerator = Substitute.For<IFileAdressenGenerator>();
            var adressNr = 1;
            adressenGenerator.GetNew().Returns($"Test/Dir{adressNr++}",$"Test/Dir{adressNr++}",$"Test/Dir{adressNr}");

            var root = DirectoryInfo.GetRoot(node, "/Test", adressenGenerator, Lf.Logger<DirectoryInfo>());

            TesteNodeInhalt(node, "Test/Dir1", "{\"type\":\"DirectoryInfo\",\"FullName\":\"/Test\",\"Adresse\":\"Test/Dir1\",\"Properties\":[],\"Inhalt\":{}}");

            root.CreateSubdirectory("Udir1");
            TesteNodeInhalt(node, "Test/Dir1", "{\"type\":\"DirectoryInfo\",\"FullName\":\"/Test\",\"Adresse\":\"Test/Dir1\",\"Properties\":[],\"Inhalt\":{\"Udir1\":{\"type\":\"DirectoryInfo\",\"FullName\":\"/Test/Udir1\",\"Adresse\":\"Test/Dir2\",\"Properties\":[]}}}");
        }

        
        [Test]
        public void GetRoot_KonstruktorLiestVomNodeInclusiveSubDir()
        {
            var configuration = Substitute.For<IConfiguration>();
            configuration["Name"].Returns("TestMemoryNode1");

            var node = new Node.Memory.Node(configuration, Lf.Logger<Node.Memory.Node>(LogLevel.Warning));
            var adressenGenerator = Substitute.For<IFileAdressenGenerator>();
            adressenGenerator.GetNew().Returns("Test/Dir1","Test/Dir2");

            var root = DirectoryInfo.GetRoot(node, "/Test", adressenGenerator, Lf.Logger<DirectoryInfo>());

            TesteNodeInhalt(node, "Test/Dir1", "{\"type\":\"DirectoryInfo\",\"FullName\":\"/Test\",\"Adresse\":\"Test/Dir1\",\"Properties\":[],\"Inhalt\":{}}");

            root.CreateSubdirectory("Udir1");

            var dir = new DirectoryInfo(node, "Test/Dir1", adressenGenerator, Lf.Logger<DirectoryInfo>());
            dir.FullName.Should().Be("/Test");

            dir.Inhalt.Should().ContainKey("Udir1").WhichValue.FullName.Should().Be("/Test/Udir1");
        }

        private void TesteNodeInhalt(INode node, string adresse, string inhalt)
        {
            var stream = node.Get(adresse);
            var streamReader = new StreamReader(stream.Result);
            streamReader.ReadToEnd().Should().Be(inhalt);

        }
    }
}