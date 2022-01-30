using System;
using System.Collections.Generic;
using System.Linq;

namespace SchVictorina.WebAPI.Utilites
{
    public static class ArrayUtilites
    {
        public static object RandomMemberFromArray<T>(T[] array)
        {
            Random random = new Random();
            var randomInt = random.Next(0, array.Length);
            return array[randomInt];
        }
        public static object AddMemberToArray<T>(T[] array, T newElement)
        {
            var arrayList = array.ToList();
            arrayList.Add(newElement);
            return arrayList.ToArray();
        }
    }
}
