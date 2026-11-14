// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Pal3.Game.Tests
{
    using System;
    using System.IO;
    using System.IO.Compression;
    using System.Linq;
    using System.Reflection;
    using Constants;
    using NUnit.Framework;
    using UnityEditor;
    using UnityEngine.Assemblies;

    public sealed class ReleaseArchiveTests
    {
        [Test]
        public void ArchiveUsesTheCurrentVersionDirectoryAndOriginalFileLayout()
        {
            string directory = Path.Combine(Path.GetTempPath(), "Pal3ReleaseTest-" + Guid.NewGuid().ToString("N"));
            string output = Path.Combine(directory, PlayerSettings.bundleVersion);
            string playerPath = Path.Combine(output, GameConstants.AppName, "Windows_x64", GameConstants.AppName + ".exe");
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(playerPath));
                File.WriteAllBytes(playerPath, new byte[] { 1, 2, 3 });
                Type pipeline = CurrentAssemblies.GetLoadedAssemblies().Select(a => a.GetType("Editor.ReleaseBuildPipeline")).First(t => t != null);
                MethodInfo zip = pipeline.GetMethod("ZipReleaseFiles", BindingFlags.Static | BindingFlags.NonPublic, null, new[] { typeof(string) }, null);
                zip.Invoke(null, new object[] { output });
                string archive = Path.Combine(output, "Release", $"{GameConstants.AppName}_v{PlayerSettings.bundleVersion}_Windows_x64.zip");
                Assert.That(File.Exists(archive), Is.True);
                using var contents = ZipFile.OpenRead(archive);
                Assert.That(contents.Entries.Select(e => e.FullName), Is.EqualTo(new[] { GameConstants.AppName + ".exe" }));
            }
            finally
            {
                if (Directory.Exists(directory))
                    Directory.Delete(directory, true);
            }
        }
    }
}
