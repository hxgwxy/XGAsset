using UnityEditor;
using UnityEngine;

namespace XGAsset.Editor.GUI
{
    public abstract class BaseWindow : EditorWindow
    {
        public virtual Rect DrawRect => new Rect(0, 0, 0, 0);
    }
}