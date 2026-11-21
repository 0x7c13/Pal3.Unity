// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Pal3.Core.Tests
{
    using System;
    using System.Globalization;
    using System.Text;
    using Core.DataReader.Ini;
    using NUnit.Framework;

    public sealed class CombatConfigFileReaderTests
    {
        [Test]
        public void OptionalEnemyCenterRequiresBothAxesWhenPresent()
        {
            var text = new StringBuilder("[RolePos]\n");
            for (int actor = 0; actor < 10; actor++)
                text.AppendFormat(CultureInfo.InvariantCulture, "Role{0}-x=1\nRole{0}-z=2\n", actor);
            var reader = new CombatConfigFileReader();
            Assert.That(reader.Read(Encoding.UTF8.GetBytes(text.ToString()), 65001).ActorGameBoxPositions, Has.Length.EqualTo(11));
            text.Append("Role10-x=3\n");
            var error = Assert.Throws<FormatException>(() => reader.Read(Encoding.UTF8.GetBytes(text.ToString()), 65001));
            Assert.That(error.Message, Does.Contain("Role10-z"));
        }

        [Test]
        public void MissingFormationRollDefaultsToZeroWhileAnInvalidValueStillFails()
        {
            const string ini = "[AllyFiveLineup]\nx=1\ny=2\nz=3\nradius=4\n";
            var reader = new CombatConfigFileReader();
            CombatConfigFile layout = reader.Read(Encoding.UTF8.GetBytes(ini), 65001);
            Assert.That(layout.AllyFormationConfig.GameBoxRadius, Is.EqualTo(4));
            Assert.That(layout.AllyFormationConfig.RollRadians, Is.Zero);
            Assert.Throws<FormatException>(() => reader.Read(Encoding.UTF8.GetBytes(ini + "roll=invalid\n"), 65001));
        }

        [TestCase("en-US")]
        [TestCase("de-DE")]
        [TestCase("fr-FR")]
        public void NativeDecimalCoordinatesAndRollIgnoreTheEditorsCulture(string culture)
        {
            CultureInfo previous = CultureInfo.CurrentCulture;
            try
            {
                CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(culture);
                var text = new StringBuilder("[RolePos]\n");
                for (int actor = 0; actor < 11; actor++)
                    text.AppendFormat(CultureInfo.InvariantCulture, "Role{0}-x=-1.25 ; position\nRole{0}-z=2.5\n", actor);
                text.Append("[AllyFiveLineup]\nx=1.5 ; center\ny=-2.25\nz=3.125\nradius=4.5 ; radius\nroll=-.500003 ; angle\n");
                text.Append("[EnemyFiveLineup]\nx=-1.5\ny=2.25\nz=-3.125\nradius=4.5\nroll=.156633\n");
                CombatConfigFile layout = new CombatConfigFileReader().Read(Encoding.UTF8.GetBytes(text.ToString()), 65001);
                Assert.That(layout.ActorGameBoxPositions[0].X, Is.EqualTo(-1.25f));
                Assert.That(layout.ActorGameBoxPositions[10].Z, Is.EqualTo(2.5f));
                Assert.That(layout.AllyFormationConfig.CenterGameBoxPosition.X, Is.EqualTo(1.5f));
                Assert.That(layout.AllyFormationConfig.GameBoxRadius, Is.EqualTo(4.5f));
                Assert.That(layout.AllyFormationConfig.RollRadians, Is.EqualTo(-.500003f));
                Assert.That(layout.EnemyFormationConfig.RollRadians, Is.EqualTo(.156633f));
            }
            finally
            {
                CultureInfo.CurrentCulture = previous;
            }
        }
    }
}
