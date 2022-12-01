// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2022, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Player
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using Command;
    using Command.InternalCommands;
    using Command.SceCommands;
    using Core.DataReader.Gdb;
    using Data;
    using UnityEngine;

    public sealed class InventoryManager : IDisposable,
        ICommandExecutor<InventoryAddItemCommand>,
        ICommandExecutor<InventoryRemoveItemCommand>,
        ICommandExecutor<InventoryAddMoneyCommand>,
        ICommandExecutor<ResetGameStateCommand>
    {
        private const int MoneyItemID = 0;

        private readonly Dictionary<int, GameItem>  _gameItemsInfo;
        private readonly Dictionary<int, int> _items = new ();

        public InventoryManager(GameResourceProvider resourceProvider)
        {
            _ = resourceProvider ?? throw new ArgumentNullException(nameof(resourceProvider));
            _gameItemsInfo = resourceProvider.GetGameItems();
            _items[MoneyItemID] = 0; // init money
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
            sb.Append($"Total money: {_items[MoneyItemID]}\n");

            foreach (var (id, count) in _items.Where(_ => _.Key != MoneyItemID))
            {
                sb.Append($"{_gameItemsInfo[id].Name}({id}) x {count} [{_gameItemsInfo[id].Type}]\n");
            }

            return sb.ToString();
        }

        public bool HaveItem(int itemId)
        {
            // TODO: Remove this
            {
                if (_gameItemsInfo.ContainsKey(itemId) &&
                    _gameItemsInfo[itemId].Type == ItemType.Plot)
                {
                    return true;
                }
            }

            return _items.ContainsKey(itemId);
        }

        public int GetTotalMoney()
        {
            return _items[MoneyItemID];
        }

        public IEnumerable<KeyValuePair<int, int>> GetAllItems()
        {
            return _items.Where(_ => _.Key != MoneyItemID);
        }

        public void Execute(InventoryAddItemCommand command)
        {
            if (!_gameItemsInfo.ContainsKey(command.ItemId)) return;

            if (_items.ContainsKey(command.ItemId))
            {
                _items[command.ItemId] += command.Count;
            }
            else
            {
                _items[command.ItemId] = command.Count;
            }

            var itemName = _gameItemsInfo[command.ItemId].Name;

            CommandDispatcher<ICommand>.Instance.Dispatch(new UIDisplayNoteCommand($"得到{itemName}"));

            Debug.LogWarning($"Add item: {itemName}({command.ItemId}) count: {command.Count}");
        }

        public void Execute(InventoryRemoveItemCommand command)
        {
            if (!_gameItemsInfo.ContainsKey(command.ItemId)) return;
            if (!_items.ContainsKey(command.ItemId)) return;

            _items[command.ItemId] -= 1;

            if (_items[command.ItemId] <= 0)
            {
                _items.Remove(command.ItemId);
            }

            var itemName = _gameItemsInfo[command.ItemId].Name;

            CommandDispatcher<ICommand>.Instance.Dispatch(new UIDisplayNoteCommand($"失去{itemName}"));

            Debug.LogWarning($"Remove item: {itemName}({command.ItemId})");
        }

        public void Execute(InventoryAddMoneyCommand command)
        {
            _items[MoneyItemID] += command.ChangeAmount;

            if (_items[MoneyItemID] < 0) _items[MoneyItemID] = 0;

            if (_items[MoneyItemID] == 0)
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

            Debug.LogWarning($"Add money: {command.ChangeAmount} current total: {_items[MoneyItemID]}");
        }

        public void Execute(ResetGameStateCommand command)
        {
            _items.Clear();
            _items[MoneyItemID] = 0; // reset money
        }
    }
}