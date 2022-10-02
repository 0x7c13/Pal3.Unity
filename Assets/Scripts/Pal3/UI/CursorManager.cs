// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2022, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.UI
{
    using Command;
    using Command.InternalCommands;
    using Data;
    using UnityEngine;
    using UnityEngine.InputSystem;
    using UnityEngine.InputSystem.DualShock;

    public class CursorManager : MonoBehaviour,
        ICommandExecutor<ActiveInputDeviceChangedNotification>
    {
        private Texture2D _cursorTextureNormal;
        private readonly CursorMode _cursorMode = CursorMode.Auto;

        public void Init(GameResourceProvider gameResourceProvider)
        {
            _cursorTextureNormal = gameResourceProvider.GetCursorTexture();
            Cursor.SetCursor(_cursorTextureNormal, Vector2.zero, _cursorMode);
        }

        private void OnEnable()
        {
            CommandExecutorRegistry<ICommand>.Instance.Register(this);
        }

        private void OnDisable()
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