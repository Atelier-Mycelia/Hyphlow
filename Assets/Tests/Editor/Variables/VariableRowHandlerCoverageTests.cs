using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using AtMycelia.Hyphlow;
using AtMycelia.Hyphlow.EditorExt;
using NUnit.Framework;
using UnityEngine.UIElements;
using Type = System.Type;

namespace VScriptingTests.Variables
{
    /// <summary>
    /// Every variable type users can add should get its own row, not quietly fall back to a base
    /// type's (the AudioMixer types once showed up as UnityObject rows that way).
    /// </summary>
    public class VariableRowHandlerCoverageTests
    {
        private static IEnumerable<Type> ContentTypesOfferedInMenu()
        {
            return VariableTypeRegistry.AllMuscariableTypes
                .Select(type => type.GetCustomAttribute<VariableInfoAttribute>())
                .Where(info => info != null && info.ShowInMenu && !info.IsTest)
                .Select(info => info.ContentType)
                .Distinct();
        }

        [Test]
        public void EveryContentTypeInTheMenu_HasARowHandlerOfItsOwn()
        {
            IDictionary<Type, Type> lookup = RowVisualHandlerRegistry.VisualHandlerLookup;
            List<string> missing = ContentTypesOfferedInMenu()
                .Where(contentType => !lookup.ContainsKey(contentType))
                .Select(contentType => contentType.Name)
                .ToList();

            Assert.IsEmpty(missing, "No row handler registered for: " + string.Join(", ", missing));
        }

        [Test]
        public void EveryMenuRowHandler_FindsItsTemplate()
        {
            IDictionary<Type, Type> lookup = RowVisualHandlerRegistry.VisualHandlerLookup;
            var missing = new List<string>();
            foreach (Type contentType in ContentTypesOfferedInMenu())
            {
                if (!lookup.TryGetValue(contentType, out Type handlerType))
                {
                    continue; // Covered by the test above
                }

                VisualTreeAsset template = RowTemplates.Resolve(handlerType);
                if (template == null)
                {
                    missing.Add(handlerType.Name);
                }
            }

            Assert.IsEmpty(missing, "These handlers' templates didn't load: " + string.Join(", ", missing));
        }

        // RowVisualHandler finds the value editor by name; without it the row can't show or edit the value
        [Test]
        public void EveryMenuRowTemplate_HasAValueField()
        {
            IDictionary<Type, Type> lookup = RowVisualHandlerRegistry.VisualHandlerLookup;
            var missing = new List<string>();
            foreach (Type contentType in ContentTypesOfferedInMenu())
            {
                if (!lookup.TryGetValue(contentType, out Type handlerType))
                {
                    continue;
                }

                VisualTreeAsset template = RowTemplates.Resolve(handlerType);
                if (template != null && template.CloneTree().Q("ValueField") == null)
                {
                    missing.Add(handlerType.Name);
                }
            }

            Assert.IsEmpty(missing, "These handlers' templates have no element named 'ValueField': " +
                string.Join(", ", missing));
        }

        [TestCase(typeof(UnityEngine.Audio.AudioMixer), typeof(AudioMixerRowVisualHandler))]
        [TestCase(typeof(UnityEngine.Audio.AudioMixerGroup), typeof(AudioMixerGroupRowVisualHandler))]
        [TestCase(typeof(UnityEngine.Audio.AudioMixerSnapshot), typeof(AudioMixerSnapshotRowVisualHandler))]
        public void AudioMixerTypes_ResolveToTheirOwnHandlers(Type contentType, Type expectedHandler)
        {
            Type resolved = new RowVisualHandlerResolver()
                .ResolveHandler(RowVisualHandlerRegistry.VisualHandlerLookup, contentType);

            Assert.AreEqual(expectedHandler, resolved);
        }
    }
}
