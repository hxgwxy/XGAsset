using UnityEditor.IMGUI.Controls;

namespace XGAsset.Editor.GUI.Base
{
    public abstract class BaseTreeView : TreeView
    {
        protected BaseTreeView(TreeViewState state) : base(state)
        {
        }

        protected BaseTreeView(TreeViewState state, MultiColumnHeader multiColumnHeader) : base(state, multiColumnHeader)
        {
        }

        // public abstract void SetData(object data);

        public virtual void SetData(object data)
        {
            BuildRoot();
            Reload();
        }
    }
}