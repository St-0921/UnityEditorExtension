using System;

namespace St.EditorExtension
{
    [AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
    public class DebugButtonAttribute : Attribute
    {
        public string ButtonName { get; }

        // 引数なしの場合は関数名がそのままボタン名になります
        public DebugButtonAttribute(string buttonName = null)
        {
            ButtonName = buttonName;
        }
    }
}