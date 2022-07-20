﻿namespace DynamicFilter.Security.Cryptography
{
    internal class SafeProxyC : SafeProxy
    {
        private const uint Poly = 0x82F63B78u;

        internal SafeProxyC()
        {
            Init(Poly);
        }
    }
}
