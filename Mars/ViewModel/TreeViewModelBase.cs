

using Route2NSEx.src.Marquis.systemUtil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mars.ViewModel
{
#if !v_16AndUp
    public class TreeViewModelBase : ViewModelBase
#else 
    public class TreeViewModelBase : ViewModelBase
#endif
    {
        ViewModelBase parent;

        public ViewModelBase Parent
        {
            get { return parent; }
            set { parent = value; }
        }

#if v_16AndUp
        private static MLogger Logger = MLogger.GetLogger(typeof(TreeViewModelBase));
        public T TraceParentNodeToSpecialType<T,T1>()
        {
            if (this.parent!=null)
            {
                if (!(parent is T1))
                {
                    return default(T);
                }
                if ((parent is T))
                {
                    try
                    {
                        return (T)(object)parent;
                    }
                    catch (Exception e)
                    {
                        Logger.Error("TraceParentNodeToSpecialType",string.Format("Can't cast type [{0}] to T [{1}], exception:[2]", parent.GetType(), typeof(T),
                            e.Message),e);
                        return default(T);
                    }
                    
                }
                return ((TreeViewModelBase)parent).TraceParentNodeToSpecialType<T,T1>();
            }
            return default(T);
        }
#endif
    }
}
