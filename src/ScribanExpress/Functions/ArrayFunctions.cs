﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ScribanExpress.Functions
{
   public class ArrayFunctions
    {
        public static string Join<T>(IEnumerable<T> list, string delimiter)
        {
            if (list == null)
            {
                return string.Empty;
            }

            var text = new StringBuilder();
            bool afterFirst = false;
            foreach (var obj in list)
            {
                if (afterFirst)
                {
                    text.Append(delimiter);
                }
                text.Append(obj);
                afterFirst = true;
            }
            return text.ToString();
        }

        public static IEnumerable<T> Reverse<T>(IEnumerable<T> list)
        {
            if (list == null)
            {
                return Enumerable.Empty<T>();
            }
            
            return list.Reverse();
        }
    }
}
