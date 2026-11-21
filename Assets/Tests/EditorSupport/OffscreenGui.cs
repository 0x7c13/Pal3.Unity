// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Pal3.Tests.EditorSupport
{
    using System;
    using System.Reflection;
    using System.Runtime.ExceptionServices;
    using UnityEditor;
    using UnityEngine;
    using UnityEngine.Rendering;
    using UnityEngine.TestTools;
    using UnityEngine.UIElements;
    using Object = UnityEngine.Object;

    /// <summary>Runs editor IMGUI on a detached panel with persistent layout and control state.</summary>
    public sealed class OffscreenGui : IDisposable
    {
        private sealed class Owner : ScriptableObject
        {
        }

        private const BindingFlags Instance = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        private const BindingFlags Static = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
        private static readonly Type PanelType = typeof(VisualElement).Assembly.GetType("UnityEngine.UIElements.Panel", true);
        private static readonly MethodInfo CreatePanel = PanelType.GetMethod("CreateEditorPanel", Static, null, new[] { typeof(ScriptableObject) }, null);
        private static readonly PropertyInfo VisualTree = PanelType.GetProperty("visualTree", Instance);
        private static readonly MethodInfo DoOnGui = typeof(IMGUIContainer).GetMethod("DoOnGUI", Instance, null,
            new[] { typeof(Event), typeof(Matrix4x4), typeof(Rect), typeof(bool), typeof(Rect), typeof(Action), typeof(bool) }, null);

        private static readonly Type ContainerWindow = typeof(EditorWindow).Assembly.GetType("UnityEditor.ContainerWindow", true);

        private readonly ScriptableObject _owner;
        private readonly IDisposable _panel;
        private readonly VisualElement _tree;
        private readonly IMGUIContainer _container;
        private readonly Action _draw;
        private readonly int _windowCount;
        private Event _input;
        private bool _editingTextField, _disposed;
        private int _keyboardControl, _hotControl;

        public RenderTexture RenderTarget
        {
            get; private set;
        }

        public OffscreenGui(Action draw)
        {
            _windowCount = Resources.FindObjectsOfTypeAll(ContainerWindow).Length;
            _owner = ScriptableObject.CreateInstance<Owner>();
            _panel = (IDisposable)CreatePanel.Invoke(null, new object[] { _owner });
            _tree = (VisualElement)VisualTree.GetValue(_panel);
            _draw = () =>
            {
                GUI.skin = EditorGUIUtility.GetBuiltinSkin(EditorSkin.Inspector);
                Event.current.mousePosition = _input.mousePosition;
                Event.current.delta = _input.delta;
                Event.current.button = _input.button;
                draw();
            };
            _container = new IMGUIContainer(_draw) { contextType = ContextType.Editor, cullingEnabled = false };
            _tree.Add(_container);
        }

        public void Paint(Vector2 size)
        {
            Run(new Event { type = EventType.Layout }, size);
            Run(new Event { type = EventType.Repaint }, size);
        }

        public void SendEvent(Event input, Vector2 size)
        {
            if (input.type is not (EventType.Layout or EventType.Repaint))
                Run(new Event { type = EventType.Layout, mousePosition = input.mousePosition }, size);
            Run(input, size);
        }

        private static bool IsPointer(Event input) => input.isMouse || input.type == EventType.ScrollWheel;

        private void Run(Event input, Vector2 size)
        {
            _tree.style.width = _container.style.width = size.x;
            _tree.style.height = _container.style.height = size.y;
            bool graphics = SystemInfo.graphicsDeviceType != GraphicsDeviceType.Null;
            if (graphics)
                EnsureTarget(size);
            var layout = new Rect(Vector2.zero, size);
            Event previousEvent = Event.current;
            RenderTexture previousTarget = graphics ? RenderTexture.active : null;
            Application.logMessageReceived += AllowCursorRectWithoutWindow;
            try
            {
                _input = new Event(input);
                Event.current = input;
                if (graphics)
                {
                    RenderTexture.active = RenderTarget;
                    GL.PushMatrix();
                }
                try
                {
                    if (graphics)
                    {
                        GL.Viewport(layout);
                        GL.LoadPixelMatrix(0, size.x, size.y, 0);
                        if (input.type == EventType.Repaint)
                            GL.Clear(true, true, Color.black);
                    }
                    // Translate the detached panel's pointer origin for native controls in nested clips.
                    Vector2 offset = IsPointer(_input) ? -_input.mousePosition : Vector2.zero;
                    // The text editor flag is global even though IMGUIContainer saves its own control state.
                    bool previousEditing = EditorGUIUtility.editingTextField;
                    int previousKeyboardControl = GUIUtility.keyboardControl;
                    int previousHotControl = GUIUtility.hotControl;
                    try
                    {
                        EditorGUIUtility.editingTextField = _editingTextField;
                        GUIUtility.keyboardControl = _keyboardControl;
                        GUIUtility.hotControl = _hotControl;
                        DoOnGui.Invoke(_container, new object[] { input, Matrix4x4.Translate(offset), new Rect(offset, size), false, layout, _draw, true });
                    }
                    finally
                    {
                        _editingTextField = EditorGUIUtility.editingTextField;
                        _keyboardControl = GUIUtility.keyboardControl;
                        _hotControl = input.rawType == EventType.MouseUp ? 0 : GUIUtility.hotControl;
                        EditorGUIUtility.editingTextField = previousEditing;
                        GUIUtility.keyboardControl = previousKeyboardControl;
                        GUIUtility.hotControl = previousHotControl;
                    }
                }
                finally
                {
                    if (graphics)
                        GL.PopMatrix();
                }
            }
            catch (TargetInvocationException exception) when (exception.InnerException is ExitGUIException)
            {
                // Standard IMGUI control flow. A detached panel has no host view to catch it.
            }
            catch (TargetInvocationException exception) when (exception.InnerException != null)
            {
                ExceptionDispatchInfo.Capture(exception.InnerException).Throw();
            }
            finally
            {
                Application.logMessageReceived -= AllowCursorRectWithoutWindow;
                Event.current = previousEvent;
                if (graphics)
                    RenderTexture.active = previousTarget;
            }

            if (Resources.FindObjectsOfTypeAll(ContainerWindow).Length > _windowCount)
                throw new InvalidOperationException("An offscreen GUI test opened a native Unity window.");
        }

        private void EnsureTarget(Vector2 size)
        {
            int width = Mathf.Max(1, Mathf.CeilToInt(size.x));
            int height = Mathf.Max(1, Mathf.CeilToInt(size.y));
            if (RenderTarget != null && RenderTarget.width == width && RenderTarget.height == height)
                return;
            ReleaseTarget();
            RenderTarget = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
            RenderTarget.Create();
        }

        private static void AllowCursorRectWithoutWindow(string message, string stackTrace, LogType type)
        {
            // Editor controls register resize cursors with GUIView, which is deliberately absent.
            if (type == LogType.Error && message == "EditorGUIUtility.AddCursorRect called outside an editor OnGUI")
                LogAssert.Expect(type, message);
        }

        private void ReleaseTarget()
        {
            if (RenderTarget == null)
                return;
            RenderTarget.Release();
            Object.DestroyImmediate(RenderTarget);
            RenderTarget = null;
        }

        public void Dispose()
        {
            if (_disposed)
                return;
            _disposed = true;
            _container.RemoveFromHierarchy();
            _container.Dispose();
            _panel.Dispose();
            Object.DestroyImmediate(_owner);
            ReleaseTarget();
        }
    }
}
