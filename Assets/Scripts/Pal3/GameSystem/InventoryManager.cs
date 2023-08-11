// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.GameSystem
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using Command;
    using Command.InternalCommands;
    using Command.SceCommands;
    using Core.DataReader.Gdb;
    using Core.Utils;
    using Data;
    using UnityEngine;

    public sealed class InventoryManager : IDisposable,
        ICommandExecutor<InventoryAddItemCommand>,
        ICommandExecutor<InventoryRemoveItemCommand>,
        ICommandExecutor<InventoryAddMoneyCommand>,
        ICommandExecutor<ResetGameStateCommand>
    {
        private const int MONEY_ITEM_ID = 0;

        private readonly Dictionary<int, GameItemInfo>  _gameItemInfos;
        private readonly Dictionary<int, int> _items = new ();

        public InventoryManager(GameResourceProvider resourceProvider)
        {
            Requires.IsNotNull(resourceProvider, nameof(resourceProvider));
            _gameItemInfos = resourceProvider.GetGameItemInfos();
            _items[MONEY_ITEM_ID] = 0; // init money
            CommandExecutorRegistry<ICommand>.Instance.Register(this);
        }

        public void Dispose()
        {
            CommandExecutorRegistry<ICommand>.Instance.UnRegister(this);
        }

        public override string ToString()
        {
            var sb = new StringBuilder();

            sb.Append("----- Inventory info -----\n");
            sb.Append($"Total money: {_items[MONEY_ITEM_ID]}\n");

            foreach (var (id, count) in _items.Where(_ => _.Key != MONEY_ITEM_ID))
            {
                sb.Append($"{_gameItemInfos[id].Name}({id}) x {count} [{_gameItemInfos[id].Type}]\n");
            }

            return sb.ToString();
        }

        public bool HaveItem(int itemId)
        {
            // TODO: Remove this
            {
                if (_gameItemInfos.ContainsKey(itemId) &&
                    _gameItemInfos[itemId].Type == ItemType.Plot)
                {
                    return true;
                }
            }

            return _items.ContainsKey(itemId);
        }

        public int GetTotalMoney()
        {
            return _items[MONEY_ITEM_ID];
        }

        public IEnumerable<KeyValuePair<int, int>> GetAllItems()
        {
            return _items.Where(_ => _.Key != MONEY_ITEM_ID);
        }

        public void Execute(InventoryAddItemCommand command)
        {
            if (!_gameItemInfos.ContainsKey(command.ItemId)) return;

            if (_items.ContainsKey(command.ItemId))
            {
                _items[command.ItemId] += command.Count;
            }
            else
            {
                _items[command.ItemId] = command.Count;
            }

            var itemName = _gameItemInfos[command.ItemId].Name;

            CommandDispatcher<ICommand>.Instance.Dispatch(new UIDisplayNoteCommand($"得到{itemName}"));

            Debug.LogWarning($"[{nameof(InventoryManager)}] Add item: {itemName}({command.ItemId}) count: {command.Count}");
        }

        public void Execute(InventoryRemoveItemCommand command)
        {
            if (!_gameItemInfos.ContainsKey(command.ItemId)) return;
            if (!_items.ContainsKey(command.ItemId)) return;

            _items[command.ItemId] -= 1;

            if (_items[command.ItemId] <= 0)
            {
                _items.Remove(command.ItemId);
            }

            var itemName = _gameItemInfos[command.ItemId].Name;

            CommandDispatcher<ICommand>.Instance.Dispatch(new UIDisplayNoteCommand($"失去{itemName}"));

            Debug.LogWarning($"[{nameof(InventoryManager)}] Remove item: {itemName}({command.ItemId})");
        }

        public void Execute(InventoryAddMoneyCommand command)
        {
            _items[MONEY_ITEM_ID] += command.ChangeAmount;

            if (_items[MONEY_ITEM_ID] < 0) _items[MONEY_ITEM_ID] = 0;

            if (_items[MONEY_ITEM_ID] == 0)
            {
                CommandDispatcher<ICommand>.Instance.Dispatch(new UIDisplayNoteCommand($"失去全部文钱"));
            }
            else if (command.ChangeAmount > 0)
            {
                CommandDispatcher<ICommand>.Instance.Dispatch(new UIDisplayNoteCommand($"得到{command.ChangeAmount}文钱"));
            }
            else if (command.ChangeAmount < 0)
            {
                CommandDispatcher<ICommand>.Instance.Dispatch(new UIDisplayNoteCommand($"失去{-command.ChangeAmount}文钱"));
            }

            Debug.LogWarning($"[{nameof(InventoryManager)}] Add money: {command.ChangeAmount} current total: {_items[MONEY_ITEM_ID]}");
        }

        public void Execute(ResetGameStateCommand command)
        {
            _items.Clear();
            _items[MONEY_ITEM_ID] = 0; // reset money
        }
    }
}