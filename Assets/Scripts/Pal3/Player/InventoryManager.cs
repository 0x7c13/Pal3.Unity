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
    using State;
    using UnityEngine;

    public sealed class InventoryManager : IDisposable,
        ICommandExecutor<InventoryAddItemCommand>,
        ICommandExecutor<InventoryRemoveItemCommand>,
        ICommandExecutor<InventoryAddMoneyCommand>,
        ICommandExecutor<ResetGameStateCommand>
    {
        private const int MoneyID = 0;
        private const int InitialMoney = 7777; // 启动资金 :)

        private readonly GameStateManager _gameStateManager;
        
        private readonly Dictionary<int, GameItem>  _gameItemsInfo;
        private readonly Dictionary<int, int> _items = new ();

        public InventoryManager(GameResourceProvider resourceProvider,
            GameStateManager gameStateManager)
        {
            _gameStateManager = gameStateManager ?? throw new ArgumentNullException(nameof(gameStateManager));
            _ = resourceProvider ?? throw new ArgumentNullException(nameof(resourceProvider));
            _gameItemsInfo = resourceProvider.GetGameItems();
            _items[MoneyID] = InitialMoney;
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
            sb.Append($"Total money: {_items[MoneyID]}\n");
            
            foreach (var (id, count) in _items.Where(_ => _.Key != MoneyID))
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
            return _items[MoneyID];
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
            
            if (_gameStateManager.GetCurrentState() == GameState.Gameplay)
            {
                CommandDispatcher<ICommand>.Instance.Dispatch(new PlaySfxCommand("wa006", 1));
                CommandDispatcher<ICommand>.Instance.Dispatch(new UIDisplayNoteCommand($"获得{itemName}"));   
            }
            
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

            if (_gameStateManager.GetCurrentState() == GameState.Gameplay)
            {
                CommandDispatcher<ICommand>.Instance.Dispatch(new PlaySfxCommand("wa007", 1));
                CommandDispatcher<ICommand>.Instance.Dispatch(new UIDisplayNoteCommand($"失去{itemName}"));
            }
            
            Debug.LogWarning($"Remove item: {itemName}({command.ItemId})");
        }

        public void Execute(InventoryAddMoneyCommand command)
        {
            _items[MoneyID] += command.ChangeAmount;

            if (_items[MoneyID] < 0) _items[MoneyID] = 0;

            if (_gameStateManager.GetCurrentState() == GameState.Gameplay)
            {
                if (_items[MoneyID] == 0)
                {
                    CommandDispatcher<ICommand>.Instance.Dispatch(new PlaySfxCommand("wa007", 1));
                    CommandDispatcher<ICommand>.Instance.Dispatch(new UIDisplayNoteCommand($"失去全部文钱"));
                }
                else if (command.ChangeAmount > 0)
                {
                    CommandDispatcher<ICommand>.Instance.Dispatch(new PlaySfxCommand("wa006", 1));
                    CommandDispatcher<ICommand>.Instance.Dispatch(new UIDisplayNoteCommand($"获得{command.ChangeAmount}文钱"));
                }
                else if (command.ChangeAmount < 0)
                {
                    CommandDispatcher<ICommand>.Instance.Dispatch(new PlaySfxCommand("wa007", 1));
                    CommandDispatcher<ICommand>.Instance.Dispatch(new UIDisplayNoteCommand($"失去{-command.ChangeAmount}文钱"));
                }
            }
            
            Debug.LogWarning($"Add money: {command.ChangeAmount} current total: {_items[MoneyID]}");
        }

        public void Execute(ResetGameStateCommand command)
        {
            _items.Clear();
            _items[MoneyID] = InitialMoney;
        }
    }
}