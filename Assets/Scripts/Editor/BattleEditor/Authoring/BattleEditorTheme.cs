// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Pal3.BattleEditor
{
    using UnityEditor;
    using UnityEngine;

    // Local style copies preserve the host Editor skin and never mutate its
    // shared styles. All measurements are logical points; IMGUI handles Retina.
    internal sealed class BattleEditorTheme
    {
        public const float ControlHeight = 24;
        public const float CanvasTop = 32;
        public const float GroupSpacing = 8;

        public bool ProSkin { get; } = EditorGUIUtility.isProSkin;

        public GUIStyle Button
        {
            get;
        }

        public GUIStyle PrimaryButton
        {
            get;
        }

        public GUIStyle ToolbarButton
        {
            get;
        }

        public GUIStyle Popup
        {
            get;
        }

        public GUIStyle Tabs
        {
            get;
        }

        public GUIStyle TabLeft
        {
            get;
        }

        public GUIStyle TabMiddle
        {
            get;
        }

        public GUIStyle TabRight
        {
            get;
        }

        public GUIStyle Section
        {
            get;
        }

        public GUIStyle Title
        {
            get;
        }

        public GUIStyle Metadata
        {
            get;
        }

        public GUIStyle WelcomeTitle
        {
            get;
        }

        public GUIStyle WelcomeText
        {
            get;
        }

        public GUIStyle Roster
        {
            get;
        }

        public Color Header => ProSkin ? new Color(.18f, .18f, .18f) : new Color(.78f, .78f, .78f);
        public Color Surface => ProSkin ? new Color(.235f, .235f, .235f) : new Color(.82f, .82f, .82f);
        public Color Border => ProSkin ? new Color(.12f, .12f, .12f) : new Color(.57f, .57f, .57f);
        public Color Accent => ProSkin ? new Color(.5f, .75f, 1) : new Color(.64f, .81f, 1);

        public BattleEditorTheme()
        {
            Button = Control(EditorStyles.miniButton);
            PrimaryButton = new GUIStyle(Button) { fontStyle = FontStyle.Bold };
            ToolbarButton = Control(EditorStyles.toolbarButton);
            Popup = Control(EditorStyles.popup);
            Popup.alignment = TextAnchor.MiddleLeft;
            Popup.padding = new RectOffset(6, 18, 0, 0);
            Tabs = Control(EditorStyles.miniButton);
            Tabs.margin = new RectOffset(0, 0, 0, 0);
            TabLeft = Control(EditorStyles.miniButtonLeft);
            TabMiddle = Control(EditorStyles.miniButtonMid);
            TabRight = Control(EditorStyles.miniButtonRight);
            Section = new GUIStyle(EditorStyles.foldoutHeader)
            {
                fixedHeight = ControlHeight,
                margin = new RectOffset(0, 0, 0, 0),
                fontSize = 11
            };
            Title = new GUIStyle(EditorStyles.boldLabel) { alignment = TextAnchor.MiddleLeft, fontSize = 12 };
            Metadata = new GUIStyle(EditorStyles.miniLabel)
            {
                alignment = TextAnchor.MiddleLeft,
                normal = { textColor = ProSkin ? new Color(.65f, .65f, .65f) : new Color(.3f, .3f, .3f) }
            };
            WelcomeTitle = new GUIStyle(EditorStyles.boldLabel) { alignment = TextAnchor.MiddleCenter, fontSize = 20, wordWrap = true };
            WelcomeText = new GUIStyle(EditorStyles.wordWrappedLabel) { alignment = TextAnchor.MiddleCenter };
            // The canvas stays dark in both Editor skins.
            WelcomeTitle.normal.textColor = new Color(.88f, .88f, .88f);
            WelcomeText.normal.textColor = new Color(.76f, .76f, .76f);
            Roster = new GUIStyle(Button) { alignment = TextAnchor.MiddleLeft, padding = new RectOffset(8, 8, 0, 0) };
        }

        private static GUIStyle Control(GUIStyle source) => new(source)
        {
            fixedHeight = ControlHeight,
            fixedWidth = 0,
            fontSize = 11,
            alignment = TextAnchor.MiddleCenter,
            margin = new RectOffset(2, 2, 2, 2),
            padding = new RectOffset(8, 8, 0, 0)
        };
    }
}
