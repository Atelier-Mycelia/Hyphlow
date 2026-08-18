using JetBrains.Annotations;
using System;

namespace AtMycelia.ControlPanel
{
    [AttributeUsage(AttributeTargets.Class)]
    public class MainTabAttribute : Attribute
    {
        public string TabUxmlPath { get; }
        public int Order { get; }

        public MainTabAttribute(string tabUxmlPath, int order = 0)
        {
            TabUxmlPath = tabUxmlPath;
            Order = order;
        }
    }

    [AttributeUsage(AttributeTargets.Class)]
    public class SubtabAttribute : Attribute
    {
        public Type ParentTabType { get; }
        public string TabUxmlPath { get; }
        public int Order { get; }

        public SubtabAttribute(Type parentTabType, string tabUxmlPath, int order = 0)
        {
            ParentTabType = parentTabType;
            TabUxmlPath = tabUxmlPath;
            Order = order;
        }
    }


}