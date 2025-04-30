using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace FreeAIr.Helper
{
    public static class ReflectionHelper
    {

        public static MethodInfo GetMethodFromInstance(
            this object target,
            string methodName
            )
        {
            var type = target.GetType();
            var result = type.GetMethod(methodName);
            return result;
        }

#pragma warning disable VSTHRD200 // Use "Async" suffix for async methods
        public static async Task<T> InvokeTaskMethodAsync<T>(
            this object target,
            MethodInfo method,
            object[] parameters = null
            )
        {
            var task = method.Invoke(
                target,
                parameters ?? new object[0]
                ) as Task;
            if (task is null)
            {
                return default;
            }

            await task;

            var taskType = task.GetType();
            var resultProperty = taskType.GetProperty("Result");
            if (resultProperty is null)
            {
                return default;
            }

            var result = (T)resultProperty.GetValue(task);
            return result;
        }
#pragma warning restore VSTHRD200 // Use "Async" suffix for async methods


    }
}
