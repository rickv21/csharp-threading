using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileManager.Extensions
{
    public static class VisualElementExtensions
    {
        public static T FindParentOfType<T>(this VisualElement self) where T : VisualElement
        {
            var parent = self.Parent;
            while (parent != null)
            {
                if (parent is T target)
                {
                    return target;
                }
                parent = parent.Parent;
            }
            return null;
        }
    }
}
