using System.Collections.Generic;
using AtMycelia.Hyphlow;
using NUnit.Framework;
using UnityEngine;
using UnityObj = UnityEngine.Object;

namespace VScriptingTests.FlowchartStructure
{
    /// <summary>
    /// Covers adding, finding and removing Commands on a Block (and through the Flowchart's
    /// BlockManager), including Commands the editor has already destroyed.
    /// </summary>
    public class BlockCommandListTests
    {
        private GameObject _fcHolder;
        private Flowchart _flowchart;
        private Block _block;

        [SetUp]
        public void SetUp()
        {
            _fcHolder = new GameObject("BlockCommandListTests_Flowchart");
            _flowchart = _fcHolder.AddComponent<Flowchart>();
            _block = _flowchart.CreateBlock(Vector2.zero) as Block;
            _block.BlockName = "TestBlock";

            CommandSignals.CommandRemoved += OnCommandRemoved;
            _removedSignalCount = 0;
        }

        private int _removedSignalCount;

        private void OnCommandRemoved(ICommand cmd, IBlock block)
        {
            _removedSignalCount++;
        }

        [TearDown]
        public void TearDown()
        {
            CommandSignals.CommandRemoved -= OnCommandRemoved;
            if (_fcHolder != null)
            {
                UnityObj.DestroyImmediate(_fcHolder);
            }

            _fcHolder = null;
            _flowchart = null;
            _block = null;
        }

        private Label AddCommand(Block toBlock)
        {
            Label cmd = _fcHolder.AddComponent<Label>();
            toBlock.Add(cmd, triggerSignals: false);
            return cmd;
        }

        private Label AddCommand() => AddCommand(_block);

        [Test]
        public void Remove_RegisteredCommand_ShrinksListAndReturnsTrue()
        {
            Label first = AddCommand();
            Label second = AddCommand();

            bool removed = _block.Remove(first);

            Assert.IsTrue(removed);
            Assert.AreEqual(1, _block.CommandList.Count);
            Assert.AreSame(second, _block.CommandList[0]);
            Assert.IsFalse(_block.Contains(first));
            Assert.IsNull(_block.GetCommandWithId(first.ItemId));
        }

        [Test]
        public void Remove_CommandNotInBlock_ReturnsFalseAndLeavesListAlone()
        {
            AddCommand();
            Label stranger = _fcHolder.AddComponent<Label>();

            bool removed = _block.Remove(stranger);

            Assert.IsFalse(removed);
            Assert.AreEqual(1, _block.CommandList.Count);
        }

        [Test]
        public void Remove_Null_ReturnsFalse()
        {
            AddCommand();

            Assert.IsFalse(_block.Remove(null));
            Assert.AreEqual(1, _block.CommandList.Count);
        }

        [Test]
        public void Remove_TriggerSignals_RaisesCommandRemovedOnce()
        {
            Label cmd = AddCommand();

            _block.Remove(cmd, triggerSignals: true);

            Assert.AreEqual(1, _removedSignalCount);
        }

        [Test]
        public void Remove_NoSignals_DoesNotRaiseCommandRemoved()
        {
            Label cmd = AddCommand();

            _block.Remove(cmd, triggerSignals: false);

            Assert.AreEqual(0, _removedSignalCount);
        }

        [Test]
        public void Remove_DestroyedCommand_RemovesThatExactCommand()
        {
            // Unity treats every destroyed object as equal to null (and so to each other),
            // so a plain List.Remove would take out whichever destroyed Command came first
            Label first = AddCommand();
            Label second = AddCommand();
            Label third = AddCommand();
            UnityObj.DestroyImmediate(first);
            UnityObj.DestroyImmediate(second);

            bool removed = _block.Remove(second);

            Assert.IsTrue(removed);
            Assert.AreEqual(2, _block.CommandList.Count);
            Assert.IsTrue(ReferenceEquals(first, _block.CommandList[0]),
                "The first destroyed Command should still be in the list.");
            Assert.AreSame(third, _block.CommandList[1]);
        }

        [Test]
        public void RemoveCommandWithId_UnknownId_ReturnsFalseWithoutThrowing()
        {
            AddCommand();
            byte unusedId = 200;

            bool removed = true;
            Assert.DoesNotThrow(() => removed = _block.RemoveCommandWithId(unusedId));
            Assert.IsFalse(removed);
            Assert.AreEqual(1, _block.CommandList.Count);
        }

        [Test]
        public void RemoveCommandWithId_KnownId_RemovesCommand()
        {
            Label cmd = AddCommand();

            bool removed = _block.RemoveCommandWithId(cmd.ItemId);

            Assert.IsTrue(removed);
            Assert.AreEqual(0, _block.CommandList.Count);
        }

        [Test]
        public void GetCommandWithId_UnknownId_ReturnsNullWithoutThrowing()
        {
            AddCommand();

            ICommand found = null;
            Assert.DoesNotThrow(() => found = _block.GetCommandWithId(200));
            Assert.IsNull(found);
        }

        [Test]
        public void RemoveAllCommands_EmptiesListAndLookup()
        {
            var added = new List<Label> { AddCommand(), AddCommand(), AddCommand() };

            bool anyRemoved = _block.RemoveAllCommands(triggerSignals: false);

            Assert.IsTrue(anyRemoved);
            Assert.AreEqual(0, _block.CommandList.Count);
            for (int i = 0; i < added.Count; i++)
            {
                Assert.IsNull(_block.GetCommandWithId(added[i].ItemId));
            }
        }

        [Test]
        public void RemoveAllCommands_WithDestroyedCommands_EmptiesList()
        {
            AddCommand();
            Label destroyed = AddCommand();
            AddCommand();
            UnityObj.DestroyImmediate(destroyed);

            _block.RemoveAllCommands(triggerSignals: false);

            Assert.AreEqual(0, _block.CommandList.Count);
        }

        [Test]
        public void RemoveAllCommands_EmptyBlock_ReturnsFalse()
        {
            Assert.IsFalse(_block.RemoveAllCommands(triggerSignals: false));
        }

        [Test]
        public void FlowchartGetCommandWithId_CommandInLaterBlock_FindsItWithoutThrowing()
        {
            // The BlockManager asks each Block in turn, so earlier Blocks missing the id
            // mustn't throw
            Block laterBlock = _flowchart.CreateBlock(new Vector2(200, 0)) as Block;
            Label cmd = AddCommand(laterBlock);

            ICommand found = null;
            Assert.DoesNotThrow(() => found = _flowchart.GetCommandWithId(cmd.ItemId));
            Assert.AreSame(cmd, found);
        }

        [Test]
        public void FlowchartRemove_RegisteredCommand_RemovesItFromItsBlock()
        {
            Label cmd = AddCommand();

            bool removed = _flowchart.Remove(cmd, triggerSignals: false);

            Assert.IsTrue(removed);
            Assert.AreEqual(0, _block.CommandList.Count);
            Assert.IsFalse(_flowchart.Contains(cmd));
        }

        [Test]
        public void FlowchartRemove_CommandInNoBlock_ReturnsFalse()
        {
            AddCommand();
            Label stranger = _fcHolder.AddComponent<Label>();

            Assert.IsFalse(_flowchart.Remove(stranger, triggerSignals: false));
        }

        [Test]
        public void FlowchartRemoveCommandWithId_NoSignals_DoesNotRaiseCommandRemoved()
        {
            Label cmd = AddCommand();

            bool removed = _flowchart.RemoveCommandWithId(cmd.ItemId, triggerSignals: false);

            Assert.IsTrue(removed);
            Assert.AreEqual(0, _removedSignalCount);
        }

        [Test]
        public void CommandToString_WithoutParentBlock_DoesNotThrow()
        {
            Label orphan = _fcHolder.AddComponent<Label>();
            orphan.ParentBlock = null;

            string result = null;
            Assert.DoesNotThrow(() => result = orphan.ToString());
            StringAssert.Contains(nameof(Label), result);
            StringAssert.DoesNotContain("on Block", result);
        }

        [Test]
        public void CommandToString_WithParentBlock_NamesBlockAndFlowchart()
        {
            Label cmd = AddCommand();

            string result = cmd.ToString();

            StringAssert.Contains("on Block TestBlock", result);
            StringAssert.Contains(_fcHolder.name, result);
        }
    }
}
