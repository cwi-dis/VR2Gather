using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace VRT.UserRepresentation.TVM.Utils
{
    public static class ByteArrayExtensions
    {
        public static Vector3[] ToVector3Array(this byte[] arr)
        {
            int sizeofVector3 = 3 * sizeof(float);

            if (arr.Length % sizeofVector3 != 0)
                throw new ArgumentException("byte array cannot be converted to vector3 array");

            int nElements = arr.Length / sizeofVector3;
            Vector3[] varr = new Vector3[nElements];
            int offset = 0;
            for (int i = 0; i < nElements; i++)
            {
                float x = BitConverter.ToSingle(arr, offset);
                offset += sizeof(float);
                float y = BitConverter.ToSingle(arr, offset);
                offset += sizeof(float);
                float z = BitConverter.ToSingle(arr, offset);
                offset += sizeof(float);
                varr[i] = new Vector3(x, y, z);
            }
            return varr;
        }

        public static Color[] ToColorArray(this byte[] arr)
        {
            int sizeofColor = 4 * sizeof(float);

            if (arr.Length % sizeofColor != 0)
                throw new ArgumentException("byte array cannot be converted to color array");

            int nElements = arr.Length / sizeofColor;
            Color[] varr = new Color[nElements];
            int offset = 0;
            for (int i = 0; i < nElements; i++)
            {
                float r = BitConverter.ToSingle(arr, offset);
                offset += sizeof(float);
                float g = BitConverter.ToSingle(arr, offset);
                offset += sizeof(float);
                float b = BitConverter.ToSingle(arr, offset);
                offset += sizeof(float);
                float a = BitConverter.ToSingle(arr, offset);
                offset += sizeof(float);
                varr[i] = new Color(r, g, b);
            }
            return varr;
        }

        public static int[] ToIntArray(this byte[] arr)
        {
            int sizeOfInt = sizeof(int);

            if (arr.Length % sizeOfInt != 0)
                throw new ArgumentException("byte array cannot be converted to int array");

            int nElements = arr.Length / sizeOfInt;
            int[] iarr = new int[arr.Length / sizeOfInt];
            int offset = 0;
            for (int i = 0; i < nElements; i++)
            {
                int v = BitConverter.ToInt32(arr, offset);
                offset += sizeof(int);
                iarr[i] = v;
            }
            return iarr;
        }
    }
}
