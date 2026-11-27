using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using AtMycelia.Hyphlow;
using AtMycelia.Hyphlow.EditorExt;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Audio;
using Type = System.Type;
using UnityObj = UnityEngine.Object;

namespace VScriptingTests.Variables
{
    /// <summary>
    /// Covers which types the add-variable menu offers, and that picking an item in the searchable
    /// popup adds the variable it's labelled as.
    /// </summary>
    public class VariableSelectMenuTests
    {
        private VariableSet _set;

        [TearDown]
        public void TearDown()
        {
            VarMenuProbe.Source = null;
            if (_set != null)
            {
                UnityObj.DestroyImmediate(_set);
            }

            _set = null;
        }

        [Test]
        public void MenuTypes_AreOnlyMuscariablesMeantForTheMenu()
        {
            foreach (Type type in VarMenuProbe.Types)
            {
                VariableInfoAttribute info = type.GetCustomAttribute<VariableInfoAttribute>();
                Assert.IsTrue(typeof(Muscariable).IsAssignableFrom(type), $"{type.Name} isn't a Muscariable.");
                Assert.IsNotNull(info, $"{type.Name} has no VariableInfo.");
                Assert.IsTrue(info.ShowInMenu, $"{type.Name} has ShowInMenu = false.");
                Assert.IsFalse(info.IsTest, $"{type.Name} is a test type.");
            }
        }

        [Test]
        public void MenuTypes_HaveOneEntryPerContentType()
        {
            List<Type> contentTypes = VarMenuProbe.Types
                .Select(type => type.GetCustomAttribute<VariableInfoAttribute>().ContentType)
                .ToList();

            CollectionAssert.AllItemsAreUnique(contentTypes);
        }

        [Test]
        public void MenuTypes_PreferTheMuscariableOverItsLegacyCounterpart()
        {
            CollectionAssert.Contains(VarMenuProbe.Types, typeof(AudioMixerMuscariable));
            CollectionAssert.DoesNotContain(VarMenuProbe.Types, typeof(AudioMixerVariable));
        }

        [Test]
        public void PopupItems_PointAtTheTypeTheyAreLabelledAs()
        {
            var popup = new VarMenuProbe();

            foreach (BasePopupWindowContent.FilteredListItem item in popup.Items)
            {
                Type pointedAt = VarMenuProbe.Types[item.origIndex];
                Assert.AreEqual(VarMenuProbe.LabelFor(pointedAt), item.name);
            }
        }

        [Test]
        public void SelectingAPopupItem_AddsAVariableOfThatContentType()
        {
            _set = ScriptableObject.CreateInstance<VariableSet>();
            VarMenuProbe.Source = _set;
            var popup = new VarMenuProbe();
            BasePopupWindowContent.FilteredListItem audioMixerItem = popup.Items
                .Single(item => VarMenuProbe.Types[item.origIndex] == typeof(AudioMixerMuscariable));

            popup.Select(audioMixerItem.origIndex);

            Assert.AreEqual(1, _set.Variables.Count);
            Assert.AreEqual(typeof(AudioMixer), _set.Variables[0].ContentType);
        }

        /// <summary>
        /// Exposes the menu's protected bits to the tests.
        /// </summary>
        private class VarMenuProbe : VariableSelectPopupWindowContent
        {
            public VarMenuProbe() : base("", 200, 200) { }

            public static IReadOnlyList<Type> Types => MenuTypes;
            public IReadOnlyList<FilteredListItem> Items => _allItems;

            public static IMuscariableSource Source
            {
                set => _curSource = value;
            }

            public static string LabelFor(Type varType) =>
                MakeDisplayLabel(VariableEditor.GetVariableInfo(varType));

            public void Select(int origIndex) => SelectByOrigIndex(origIndex);
        }
    }
}
