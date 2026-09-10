using System;
using System.Collections;
using System.Collections.Generic;

namespace AtMycelia.Hyphlow.EditorExt.FcWindow
{

    public abstract class FcwModuleDispatcher : IModuleDispatcher<IFlowchartWindowModule>
    {
        private readonly List<IFlowchartWindowModule> _modules = new List<IFlowchartWindowModule>();
        private readonly Dictionary<Type, IList> _responderBuckets = new Dictionary<Type, IList>();
        public abstract void ToggleSubs(bool on);


        public virtual void AddModule(object module)
        {
            if (module is not IFlowchartWindowModule flowchartModule)
            {
                throw new ArgumentException($"Module must implement {nameof(IFlowchartWindowModule)}", nameof(module));
            }
            AddModule((IFlowchartWindowModule)module);
        }

        public virtual void RemoveModule(object module)
        {
            if (module is not IFlowchartWindowModule flowchartModule)
            {
                throw new ArgumentException($"Module must implement {nameof(IFlowchartWindowModule)}", nameof(module));
            }

            RemoveModule((IFlowchartWindowModule)module);
        }

        public virtual void AddModule(IFlowchartWindowModule module)
        {
            _modules.Add(module);

            AddAsResponder(module);
        }

        protected abstract void AddAsResponder(IFlowchartWindowModule module);
        protected abstract void RemoveAsResponder(IFlowchartWindowModule module);

        public virtual void RemoveModule(IFlowchartWindowModule module)
        {
            _modules.Remove(module);
            RemoveAsResponder(module);
        }

        public virtual void ClearModules()
        {
            for (int i = 0; i < _modules.Count; i++)
            {
                _modules[i].Dispose();
            }

            _modules.Clear();
            _responderBuckets.Clear();
        }

    }
}