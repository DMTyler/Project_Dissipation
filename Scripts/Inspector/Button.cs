using System;
using UnityEngine;

namespace DGraphics.Dissipation.Inspector
{
    [AttributeUsage(AttributeTargets.Method, Inherited = true)]
    public class ButtonAttribute : PropertyAttribute
    {
        public string Label { get; private set; }

        /// <summary>
        /// 可以不传参，此时按钮文字默认为方法名字；也可以传参自定义按钮文字。
        /// </summary>
        public ButtonAttribute(string label = null)
        {
            Label = label;
        }
    }
}