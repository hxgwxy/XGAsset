using UnityEditor;
using UnityEngine;

namespace XGAsset.Editor.GUI
{
    public abstract class BaseSubWindow
    {
        protected readonly BaseWindow ParentWindow;
        protected Rect ParentRect => ParentWindow != null ? ParentWindow.DrawRect : new Rect(0, 0, 0, 0);

        protected BaseSubWindow(BaseWindow parentWindow)
        {
            ParentWindow = parentWindow;
        }

        public bool IsValid => ParentWindow != null;

        public abstract void Initialize();

        public abstract void Draw();

        public abstract void Reload();
    }
}