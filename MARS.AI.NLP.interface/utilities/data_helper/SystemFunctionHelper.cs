using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MARS.AIL.NLP.Inter.utilities.data_helper
{
    internal class SystemFunctionHelper
    {
        public static T[] PopAllToArray<T>(Stack<T> stack)
        {
            // Create an array with the size of the stack count
            T[] array = new T[stack.Count];

            // Pop all elements from the stack into the array
            for (int i = 0; i < array.Length; i++)
            {
                array[i] = stack.Pop();
            }

            return array;
        }
    }
}
