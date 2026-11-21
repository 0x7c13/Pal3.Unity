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

    public sealed class CombatCameraConfigFileReaderTests
    {
        private const string Camera = "[DefaultCam0]\nx=-70.413155\ny=.125\nz=140.5\nyaw=-3.171740\npitch=-.500003\nroll=1.25e-2\n";

        private static CombatCameraConfig Read(string text) => new CombatCameraConfigFileReader().Read(Encoding.ASCII.GetBytes(text), 936).DefaultCamConfigs[0];

        [TestCase("en-US")]
        [TestCase("fr-FR")]
        [TestCase("de-DE")]
        public void NativeCameraCoordinatesAndAnglesIgnoreTheEditorsCulture(string culture)
        {
            CultureInfo previous = CultureInfo.CurrentCulture;
            try
            {
                CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(culture);
                CombatCameraConfig camera = Read(Camera);
                Assert.That(camera.GameBoxPositionX, Is.EqualTo(-70.413155f));
                Assert.That(camera.GameBoxPositionY, Is.EqualTo(.125f));
                Assert.That(camera.GameBoxPositionZ, Is.EqualTo(140.5f));
                Assert.That(camera.Yaw, Is.EqualTo(-3.171740f));
                Assert.That(camera.Pitch, Is.EqualTo(-.500003f));
                Assert.That(camera.Roll, Is.EqualTo(.0125f));
            }
            finally
            {
                CultureInfo.CurrentCulture = previous;
            }
        }

        [TestCase("en-US")]
        [TestCase("fr-FR")]
        [TestCase("de-DE")]
        public void NativeCameraNumbersNeverInterpretCommasAsThousandsSeparators(string culture)
        {
            CultureInfo previous = CultureInfo.CurrentCulture;
            try
            {
                CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(culture);
                const string grouped = "[DefaultCam0]\nx=-70,413155\ny=1\nz=2\nyaw=-3\npitch=-1\nroll=0\n";
                Assert.Throws<FormatException>(() => Read(grouped));
            }
            finally
            {
                CultureInfo.CurrentCulture = previous;
            }
        }

        [Test]
        public void CameraValuesAllowNativeInlineCommentsWithoutChangingTheirValue()
        {
            CombatCameraConfig camera = Read(Camera.Replace("-70.413155", "-70.413155 ; position").Replace("-3.171740", "-3.171740 ; angle"));
            Assert.That(camera.GameBoxPositionX, Is.EqualTo(-70.413155f));
            Assert.That(camera.Yaw, Is.EqualTo(-3.171740f));
        }
    }
}
