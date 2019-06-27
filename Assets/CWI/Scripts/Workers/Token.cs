using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Workers
{
    public class Token
    {
        public Token(int forks) { totalForks = forks; }
        public Token(Token token) { original = token; currentBuffer = token.currentBuffer; currentSize = token.currentSize; }

        public int totalForks;
        public int currentForks;
        public System.IntPtr currentBuffer;
        public int currentSize;
        public Token original;
    }
}