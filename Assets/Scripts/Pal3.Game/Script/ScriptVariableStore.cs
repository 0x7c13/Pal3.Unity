// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Pal3.Game.Script
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using Core.Contract.Constants;
    using Core.DataReader.Sce;

    // Local variables belong to a script instance, not to its script ID or scene.
    internal sealed class ScriptVariableStore : IUserVariableStore<ushort, int>
    {
        private readonly IUserVariableStore<ushort, int> _globals;
        private readonly int[] _locals;

        public ScriptVariableStore(IUserVariableStore<ushort, int> globals, SceUserVarInfo[] definitions)
        {
            _globals = globals;
            _locals = new int[definitions?.Length ?? 0];
            for (int i = 0; i < _locals.Length; i++)
            {
                byte[] data = definitions[i].InitData;
                if (data == null || data.Length == 0) continue;
                if (data.Length != sizeof(int))
                    throw new NotSupportedException($"Unsupported local variable initializer at index {i}");
                _locals[i] = data[0] | data[1] << 8 | data[2] << 16 | data[3] << 24;
            }
        }

        public int Get(ushort variable) => (variable & ScriptConstants.GlobalVariableFlag) != 0
            ? _globals.Get(variable) : _locals[variable];

        public void Set(ushort variable, int value)
        {
            if ((variable & ScriptConstants.GlobalVariableFlag) != 0) _globals.Set(variable, value);
            else _locals[variable] = value;
        }

        public IEnumerator<KeyValuePair<ushort, int>> GetEnumerator()
        {
            foreach (var variable in _globals) yield return variable;
            for (int i = 0; i < _locals.Length; i++)
                yield return new KeyValuePair<ushort, int>((ushort)i, _locals[i]);
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
