// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2024, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Game.UI
{
    using System;
    using Command;
    using Command.Extensions;
    using Core.Command;
    using Core.Utilities;
    using Data;
    using Engine.Core.Abstraction;
    using UnityEngine;
    using UnityEngine.InputSystem;
    using UnityEngine.InputSystem.DualShock;

    public sealed class CursorManager : IDisposable,
        ICommandExecutor<ActiveInputDeviceChangedNotification>
    {
        private readonly ITexture2D _cursorTextureNormal;

        public CursorManager(GameResourceProvider gameResourceProvider)
        {
            Requires.IsNotNull(gameResourceProvider, nameof(gameResourceProvider));
            _cursorTextureNormal = gameResourceProvider.GetCursorTexture();
            Cursor.SetCursor(_cursorTextureNormal.NativeObject as Texture2D, Vector2.zero, CursorMode.Auto);

            CommandExecutorRegistry<ICommand>.Instance.Register(this);
        }

        public void Dispose()
        {
            CommandExecutorRegistry<ICommand>.Instance.UnRegister(this);
            Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
            Cursor.visible = true;
        }

        public void Execute(ActiveInputDeviceChangedNotification command)
        {
            if (command.Device == Gamepad.current ||
                command.Device == Joystick.current ||
                command.Device == Touchscreen.current ||
                command.Device == DualShockGamepad.current)
            {
                Cursor.visible = false;
            }
            else if (command.Device == Mouse.current ||
                     command.Device == Pointer.current)
            {
                Cursor.visible = true;
            }
        }
    }
}