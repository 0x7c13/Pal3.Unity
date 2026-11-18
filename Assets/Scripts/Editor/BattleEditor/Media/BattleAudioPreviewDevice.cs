// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Pal3.BattleEditor
{
    using System;
    using System.Reflection;
    using UnityEditor;
    using UnityEngine;

    // AudioUtil has one global preview transport, without per-clip stop or pitch.
    // Movies reserve it until release; pending SFX decoders cannot replace their
    // soundtrack, and a former owner cannot stop the new owner's audio.
    internal static class BattleAudioPreviewDevice
    {
        private const BindingFlags Static = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
        private static readonly Type Utility = typeof(AudioImporter).Assembly.GetType("UnityEditor.AudioUtil");
        private static readonly MethodInfo PlayClip = Find("PlayPreviewClip", typeof(AudioClip), typeof(int), typeof(bool));
        private static readonly MethodInfo PauseClip = Find("PausePreviewClip");
        private static readonly MethodInfo ResumeClip = Find("ResumePreviewClip");
        private static readonly MethodInfo StopClips = Find("StopAllPreviewClips");
        private static readonly MethodInfo SetPosition = Find("SetPreviewClipSamplePosition", typeof(AudioClip), typeof(int));
        private static readonly MethodInfo GetPosition = Find("GetPreviewClipSamplePosition");
        private static object _owner;
        private static bool _exclusive;

        public static bool IsAvailable => PlayClip != null && PauseClip != null && ResumeClip != null &&
            StopClips != null && SetPosition != null && GetPosition != null;

        private static MethodInfo Find(string name, params Type[] parameters) =>
            Utility?.GetMethod(name, Static, null, parameters, null);

        [InitializeOnLoadMethod]
        private static void Initialize()
        {
            AssemblyReloadEvents.beforeAssemblyReload += Reset;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            // These events also run when Enter Play Mode skips domain reload.
            if (state is PlayModeStateChange.ExitingEditMode or PlayModeStateChange.ExitingPlayMode)
                Reset();
        }

        private static void Reset()
        {
            try
            {
                Stop(_owner);
            }
            catch (Exception error) when (IsUnavailable(error))
            {
                // Stop releases the lease before calling the optional editor backend.
            }
        }

        internal static bool IsUnavailable(Exception error) =>
            error is NotSupportedException or TypeInitializationException ||
            error is TargetInvocationException invocation && IsUnavailable(invocation.InnerException);

        public static bool Owns(object owner) => owner != null && ReferenceEquals(_owner, owner);

        public static bool Play(object owner, AudioClip clip, int sample = 0, bool exclusive = false)
        {
            if (_exclusive && !Owns(owner))
                return false;
            if (!IsAvailable)
                throw new NotSupportedException("This editor does not expose the battle audio preview controls.");
            if (exclusive && _owner != null && !Owns(owner))
                StopClips?.Invoke(null, null);
            PlayClip.Invoke(null, new object[] { clip, sample, false });
            _owner = owner;
            _exclusive = exclusive;
            return true;
        }

        public static void Pause(object owner)
        {
            if (Owns(owner))
                PauseClip?.Invoke(null, null);
        }

        public static void Resume(object owner, AudioClip clip, int sample)
        {
            if (!Owns(owner))
                return;
            Seek(owner, clip, sample);
            ResumeClip?.Invoke(null, null);
        }

        public static int Position(object owner) => Owns(owner) && GetPosition != null ? (int)GetPosition.Invoke(null, null) : -1;

        public static void Seek(object owner, AudioClip clip, int sample)
        {
            if (Owns(owner))
                SetPosition?.Invoke(null, new object[] { clip, sample });
        }

        public static void Stop(object owner)
        {
            if (!Owns(owner))
                return;
            _owner = null;
            _exclusive = false;
            StopClips?.Invoke(null, null);
        }
    }
}
