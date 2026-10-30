using System;
using System.Linq;
using FluentAssertions;
using NUnit.Framework;

namespace WebRaid.Node.Raid.Tests
{
    public class ReedSolomonTests
    {
        [TestCase(1, 0)]
        [TestCase(1, 2)]
        [TestCase(2, 1)]
        [TestCase(3, 2)]
        [TestCase(4, 4)]
        [TestCase(10, 4)]
        public void Wiederherstellen_JedeAuswahlBisMAusfaelle_ErgibtDieDaten(int k, int m)
        {
            var code = new ReedSolomon(k, m);
            var zufall = new Random(k * 100 + m);
            var original = Enumerable.Range(0, k + m).Select(_ => new byte[37]).ToArray();
            for (var i = 0; i < k; i++) zufall.NextBytes(original[i]);
            code.Kodieren(original);

            foreach (var ausfaelle in Teilmengen(k + m, m))
            {
                var bloecke = original.Select(b => (byte[])b.Clone()).ToArray();
                var vorhanden = Enumerable.Repeat(true, k + m).ToArray();
                foreach (var a in ausfaelle)
                {
                    vorhanden[a] = false;
                    zufall.NextBytes(bloecke[a]);
                }

                code.Wiederherstellen(bloecke, vorhanden);

                for (var i = 0; i < k + m; i++) bloecke[i].Should().Equal(original[i], $"Block {i} bei Ausfall von {string.Join(",", ausfaelle)}");
            }
        }

        [Test]
        public void Kodieren_LaesstDatenBloeckeUnveraendert()
        {
            var code = new ReedSolomon(3, 2);
            var bloecke = Enumerable.Range(0, 5).Select(i => Enumerable.Repeat((byte)(i + 1), 8).ToArray()).ToArray();

            code.Kodieren(bloecke);

            bloecke[0].Should().OnlyContain(b => b == 1);
            bloecke[1].Should().OnlyContain(b => b == 2);
            bloecke[2].Should().OnlyContain(b => b == 3);
        }

        [Test]
        public void Wiederherstellen_ZuVieleAusfaelle_WirftAusnahme()
        {
            var code = new ReedSolomon(3, 2);
            var bloecke = Enumerable.Range(0, 5).Select(_ => new byte[4]).ToArray();

            Action aktion = () => code.Wiederherstellen(bloecke, new[] { true, false, true, false, false });

            aktion.Should().Throw<InvalidOperationException>();
        }

        [TestCase(0, 1)]
        [TestCase(1, -1)]
        [TestCase(200, 56)]
        public void Konstruktor_UngueltigeParameter_WirftAusnahme(int k, int m)
        {
            Action aktion = () => new ReedSolomon(k, m);

            aktion.Should().Throw<ArgumentOutOfRangeException>();
        }

        /// <summary>Alle Teilmengen von {0..n-1} mit genau <paramref name="groesse"/> Elementen</summary>
        private static int[][] Teilmengen(int n, int groesse)
        {
            if (groesse == 0) return new[] { Array.Empty<int>() };
            return Enumerable.Range(0, n)
                .SelectMany(erstes => Teilmengen(n, groesse - 1).Where(rest => rest.All(r => r > erstes)).Select(rest => new[] { erstes }.Concat(rest).ToArray()))
                .ToArray();
        }
    }
}
