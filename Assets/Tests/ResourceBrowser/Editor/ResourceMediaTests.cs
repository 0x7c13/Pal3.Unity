// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Pal3.ResourceBrowser.Tests
{
    using System;
    using System.Collections;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using NUnit.Framework;
    using UnityEditor;
    using UnityEngine;
    using UnityEngine.SceneManagement;
    using UnityEngine.TestTools;

    [TestFixture, Category("ResourceBrowserLocalData")]
    public sealed class ResourceMediaTests
    {
        private ResourceCatalog _catalog;

        [OneTimeSetUp]
        public void Load()
        {
            string root = Path.GetFullPath(Path.Combine(Application.dataPath, "../../Game Data", ResourceBrowserWindow.Variant));
            if (!Directory.Exists(root))
                Assert.Ignore("Local game data is unavailable.");
            _catalog = ResourceCatalog.Load(root, 936, ResourceBrowserWindow.Variant);
        }

        [OneTimeTearDown]
        public void Cleanup() => _catalog?.Dispose();

        [UnityTest]
        public IEnumerator RealAudioLoadsAndReleasesArchiveTemporaryFiles()
        {
            foreach (string extension in new[] { ".wav", ".mp3" })
            {
                ResourceEntry entry = _catalog.Entries.First(e => e.Extension == extension);
                using var preview = new ResourceMediaPreview(_catalog, entry);
                double deadline = EditorApplication.timeSinceStartup + 35;
                while (preview.Loading && EditorApplication.timeSinceStartup < deadline)
                {
                    preview.Tick();
                    yield return null;
                }
                Assert.That(preview.Error, Is.Null, entry.Path);
                Assert.That(preview.Ready, Is.True, entry.Path);
                AudioClip clip = preview.Audio;
                Assert.That(clip.length, Is.GreaterThan(0));
                preview.Tick();
                Assert.That(preview.Tick(), Is.False, "Idle audio must not continuously repaint the browser.");
                string directory = preview.TemporaryDirectory;
                ResourceMediaPreview.CleanupStaleTemporaryFiles();
                if (directory != null)
                    Assert.That(Directory.Exists(directory), Is.True, "Cleanup must preserve active previews.");
                preview.Dispose();
                Assert.That(clip == null, Is.True);
                if (directory != null)
                    Assert.That(Directory.Exists(directory), Is.False);
            }
        }

        [UnityTest]
        public IEnumerator RealVideoPlaysInPreviewSceneWithoutDirtyingCurrentScene()
        {
            Scene original = SceneManager.GetActiveScene();
            bool dirty = original.isDirty;
            ResourceEntry entry = _catalog.Entries.First(e => e.Extension == ".webm");
            using var preview = new ResourceMediaPreview(_catalog, entry);
            double deadline = EditorApplication.timeSinceStartup + 35;
            while (preview.Loading && EditorApplication.timeSinceStartup < deadline)
            {
                preview.Tick();
                yield return null;
            }
            Assert.That(preview.Error, Is.Null, entry.Path);
            Assert.That(preview.Ready, Is.True);
            var video = preview.Video;
            video.Play();
            Assert.That(preview.Tick(), Is.True, "Playing video must repaint.");
            deadline = EditorApplication.timeSinceStartup + 10;
            while (video.frame < 2 && EditorApplication.timeSinceStartup < deadline)
            {
                preview.Tick();
                yield return null;
            }
            Assert.That(video.frame, Is.GreaterThanOrEqualTo(2));
            Assert.That(video.texture, Is.Not.Null);
            video.Pause();
            foreach (double requestedTime in new[] { video.length * 0.5, 0, video.length })
            {
                bool seeked = false;
                UnityEngine.Video.VideoPlayer.EventHandler handler = _ => seeked = true;
                video.seekCompleted += handler;
                double seekTime = Math.Min(requestedTime, video.length - 1 / video.frameRate);
                preview.SeekVideo(requestedTime);
                Assert.That(preview.Tick(), Is.True, "Seeking video must repaint.");
                deadline = EditorApplication.timeSinceStartup + 10;
                while ((!seeked || Math.Abs(video.frame / video.frameRate - seekTime) > 0.2) && EditorApplication.timeSinceStartup < deadline)
                {
                    preview.Tick();
                    yield return null;
                }
                video.seekCompleted -= handler;
                Assert.That(preview.Error, Is.Null);
                Assert.That(seeked, Is.True, "Paused seek did not complete.");
                Assert.That(video.frame / video.frameRate, Is.EqualTo(seekTime).Within(0.2), "Paused seek did not update the frame.");
                Assert.That(video.isPlaying, Is.False);
                preview.Tick();
                Assert.That(preview.Tick(), Is.False, "Paused video must stop requesting repaints after the final frame.");
            }
            preview.SeekVideo(video.length * 0.25);
            preview.SeekVideo(video.length * 0.5);
            preview.SeekVideo(video.length * 0.75);
            deadline = EditorApplication.timeSinceStartup + 10;
            while (Math.Abs(video.frame / video.frameRate - video.length * 0.75) > 0.2 && EditorApplication.timeSinceStartup < deadline)
            {
                preview.Tick();
                yield return null;
            }
            Assert.That(preview.Error, Is.Null);
            Assert.That(video.frame / video.frameRate, Is.EqualTo(video.length * 0.75).Within(0.2), "Scrubbing must retain the latest requested time.");
            Assert.That(video.isPlaying, Is.False);
            preview.Tick();
            Assert.That(preview.Tick(), Is.False);
            double asynchronousSeek = video.length * 0.25;
            preview.SeekVideo(asynchronousSeek);
            deadline = EditorApplication.timeSinceStartup + 10;
            while (Math.Abs(video.frame / video.frameRate - asynchronousSeek) > 0.2 && EditorApplication.timeSinceStartup < deadline)
            {
                // Let decoder callbacks finish between browser ticks.
                EditorApplication.QueuePlayerLoopUpdate();
                yield return null;
            }
            Assert.That(video.frame / video.frameRate, Is.EqualTo(asynchronousSeek).Within(0.2));
            Assert.That(video.isPlaying, Is.False);
            Assert.That(preview.Tick(), Is.True, "A completed asynchronous seek must repaint its final frame.");
            Assert.That(preview.Tick(), Is.False);
            typeof(ResourceMediaPreview).GetMethod("OnVideoError", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(preview, new object[] { video, "Simulated decoder error" });
            Assert.That(preview.Tick(), Is.True, "An error arriving while idle must repaint once.");
            Assert.That(preview.Error, Is.EqualTo("Simulated decoder error"));
            Assert.That(preview.Tick(), Is.False);
            Assert.That(SceneManager.GetActiveScene(), Is.EqualTo(original));
            Assert.That(original.isDirty, Is.EqualTo(dirty));
            preview.Dispose();
            Assert.That(video == null, Is.True);
        }

        [UnityTest]
        public IEnumerator DisposingDuringExtractionCannotCreateMediaOrLeaveFiles()
        {
            ResourceEntry entry = _catalog.Entries.First(e => e.IsArchived && e.Extension == ".mp3");
            var preview = new ResourceMediaPreview(_catalog, entry);
            string directory = preview.TemporaryDirectory;
            preview.Dispose();
            double deadline = EditorApplication.timeSinceStartup + 2;
            while (EditorApplication.timeSinceStartup < deadline)
            {
                preview.Tick();
                yield return null;
            }
            Assert.That(preview.Audio, Is.Null);
            Assert.That(preview.Video, Is.Null);
            Assert.That(Directory.Exists(directory), Is.False);
        }

        [Test]
        public void CleanupPreservesLiveAndUnrecognizedDirectories()
        {
            string root = Path.GetFullPath(Path.Combine(Application.dataPath, "../../workspace/ResourceBrowserMedia"));
            using var process = Process.GetCurrentProcess();
            string live = Path.Combine(root, process.Id + "-" + Guid.NewGuid().ToString("N"));
            string stale = Path.Combine(root, int.MaxValue + "-" + Guid.NewGuid().ToString("N"));
            string legacy = Path.Combine(root, Guid.NewGuid().ToString("N"));
            string link = Path.Combine(root, int.MaxValue + "-" + Guid.NewGuid().ToString("N"));
            string[] directories = { live, stale, legacy };
            try
            {
                Assert.Throws<ArgumentException>(() => Process.GetProcessById(int.MaxValue));
                foreach (string directory in directories)
                {
                    Directory.CreateDirectory(directory);
                    File.WriteAllText(Path.Combine(directory, "preview.mp3"), "temporary media");
                }
                bool testLink = Application.platform is RuntimePlatform.OSXEditor or RuntimePlatform.LinuxEditor;
                if (testLink)
                {
                    var start = new ProcessStartInfo("/bin/ln") { UseShellExecute = false };
                    start.ArgumentList.Add("-s");
                    start.ArgumentList.Add(legacy);
                    start.ArgumentList.Add(link);
                    using var ln = Process.Start(start);
                    ln.WaitForExit();
                    Assert.That(ln.ExitCode, Is.Zero);
                }
                ResourceMediaPreview.CleanupStaleTemporaryFiles();
                Assert.That(Directory.Exists(live), Is.True);
                Assert.That(Directory.Exists(legacy), Is.True);
                Assert.That(Directory.Exists(stale), Is.False);
                Assert.That(File.ReadAllText(Path.Combine(legacy, "preview.mp3")), Is.EqualTo("temporary media"));
                if (testLink)
                    Assert.That(Directory.Exists(link), Is.True, "Cleanup must not follow or remove symbolic links.");
            }
            finally
            {
                if (Directory.Exists(link))
                    Directory.Delete(link);
                foreach (string directory in directories)
                    if (Directory.Exists(directory))
                        Directory.Delete(directory, true);
            }
        }

        [Test]
        public void BatchConversionsProduceDistinctScriptDialogueAndDatabaseFiles()
        {
            string output = Path.GetFullPath(Path.Combine(Application.dataPath, "../../workspace/ResourceBrowserQA/test-output", Guid.NewGuid().ToString("N")));
            try
            {
                foreach (var mode in new[] { ResourceExportMode.Script, ResourceExportMode.Dialogue, ResourceExportMode.Json })
                {
                    ResourceEntry entry = _catalog.Entries.First(e => e.Extension == (mode == ResourceExportMode.Json ? ".gdb" : ".sce"));
                    string destination = ResourceCatalog.ExportPath(output, entry.Path + ResourceExport.Suffix(mode));
                    ResourceExport.Write(_catalog, entry, mode, destination);
                    Assert.That(new FileInfo(destination).Length, Is.GreaterThan(0));
                    Assert.Throws<IOException>(() => ResourceExport.Write(_catalog, entry, mode, destination));
                }
            }
            finally
            {
                if (Directory.Exists(output))
                    Directory.Delete(output, true);
            }
        }
    }
}
