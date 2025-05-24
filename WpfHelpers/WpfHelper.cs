using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;

namespace WpfHelpers
{
    public static class WpfHelper
    {
        public static IEnumerable<T> FindLogicalChildren<T>(this DependencyObject depObj) where T : DependencyObject
        {
            if (depObj != null)
            {
                foreach (object rawChild in LogicalTreeHelper.GetChildren(depObj))
                {
                    if (rawChild is DependencyObject)
                    {
                        DependencyObject child = (DependencyObject)rawChild;
                        if (child is T)
                        {
                            yield return (T)child;
                        }

                        foreach (T childOfChild in FindLogicalChildren<T>(child))
                        {
                            yield return childOfChild;
                        }
                    }
                }
            }
        }

        public static List<TChildItem> FindVisualChildren<TChildItem>(
            this DependencyObject obj
            )
           where TChildItem : DependencyObject
        {
            var result = new List<TChildItem>();

            FindVisualChildren<TChildItem>(
                obj,
                result
                );

            return result;
        }

        public static void FindVisualChildren<TChildItem>(
            this DependencyObject obj,
            List<TChildItem> result
            )
           where TChildItem : DependencyObject
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(obj); i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(obj, i);

                if (child != null && child is TChildItem)
                {
                    result.Add(child as TChildItem);
                }
                else
                {
                    FindVisualChildren<TChildItem>(child, result);
                }
            }
        }

        public static List<DependencyObject> FindVisualChildren(
            this DependencyObject obj,
            string typeName
            )
        {
            var result = new List<DependencyObject>();

            FindVisualChildren(
                obj,
                typeName,
                result
                );

            return result;
        }

        public static void FindVisualChildren(
            this DependencyObject obj,
            string typeName,
            List<DependencyObject> result
            )
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(obj); i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(obj, i);

                if (child != null && child.GetType().ToString() == typeName)
                {
                    result.Add(child);
                }
                else
                {
                    FindVisualChildren(child, typeName, result);
                }
            }
        }


        public static void GetRecursiveByType<T>(
            this DependencyObject root,
            ref List<T> result
            )
            where T : FrameworkElement
        {
            if (root == null)
            {
                throw new ArgumentNullException(nameof(root));
            }

            var childrenCount = VisualTreeHelper.GetChildrenCount(root);

            for (var cc = 0; cc < childrenCount; cc++)
            {
                var control = VisualTreeHelper.GetChild(
                    root,
                    cc
                    );

                if (control is T typedControl)
                {
                    result.Add(typedControl);
                    continue;
                }

                control.GetRecursiveByType(ref result);
            }
        }

        public static FrameworkElement? GetRecursiveByTypeOrName(
            this DependencyObject root,
            string childTypeName,
            string? name = null
            )
        {
            if (root == null)
            {
                throw new ArgumentNullException(nameof(root));
            }

            var childrenCount = VisualTreeHelper.GetChildrenCount(root);

            for (var cc = 0; cc < childrenCount; cc++)
            {
                var control = VisualTreeHelper.GetChild(
                    root,
                    cc
                    );

                if (control.GetType().Name == childTypeName)
                {
                    var fe = control as FrameworkElement;
                    if (string.IsNullOrEmpty(name) || fe.Name == name)
                    {
                        return fe;
                    }
                }

                var result = control.GetRecursiveByTypeOrName(childTypeName, name);
                if (result != null)
                {
                    return result;
                }
            }

            return null;
        }

        public static TChildType GetRecursiveByName<TChildType>(
            this DependencyObject root,
            string name
            ) where TChildType : FrameworkElement
        {
            if (root == null)
            {
                throw new ArgumentNullException(nameof(root));
            }

            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            var childrenCount = VisualTreeHelper.GetChildrenCount(root);

            for (var cc = 0; cc < childrenCount; cc++)
            {
                var control = VisualTreeHelper.GetChild(
                    root,
                    cc
                    );

                if (control is TChildType fe)
                {
                    if (fe.Name == name)
                    {
                        return fe;
                    }
                }

                var result = control.GetRecursiveByName<TChildType>(name);
                if (result != null)
                {
                    return result;
                }
            }

            return null;
        }

        public static FrameworkElement GetRecursiveByName(
            this DependencyObject root,
            string name
            )
        {
            if (root == null)
            {
                throw new ArgumentNullException(nameof(root));
            }

            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            var childrenCount = VisualTreeHelper.GetChildrenCount(root);

            for (var cc = 0; cc < childrenCount; cc++)
            {
                var control = VisualTreeHelper.GetChild(
                    root,
                    cc
                    );

                if (control is FrameworkElement fe)
                {
                    if (fe.Name == name)
                    {
                        return fe;
                    }
                }

                var result = control.GetRecursiveByName(name);
                if (result != null)
                {
                    return result;
                }
            }

            return null;
        }

    }
}
