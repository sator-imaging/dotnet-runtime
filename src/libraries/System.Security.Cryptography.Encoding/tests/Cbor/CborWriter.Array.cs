﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable
using System.Buffers.Binary;
using System.Text;

namespace System.Security.Cryptography.Encoding.Tests.Cbor
{
    internal partial class CborWriter
    {
        public void WriteStartArray(int definiteLength)
        {
            if (definiteLength < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(definiteLength), "must be non-negative integer.");
            }

            WriteUnsignedInteger(CborMajorType.Array, (ulong)definiteLength);
            PushDataItem(CborMajorType.Array, (uint)definiteLength);
        }

        public void WriteEndArray()
        {
            if (!_remainingDataItems.HasValue)
            {
                // indefinite-length map, add break byte
                EnsureWriteCapacity(1);
                WriteInitialByte(new CborInitialByte(CborInitialByte.IndefiniteLengthBreakByte));
            }

            PopDataItem(CborMajorType.Array);
        }

        public void WriteStartArrayIndefiniteLength()
        {
            EnsureWriteCapacity(1);
            WriteInitialByte(new CborInitialByte(CborMajorType.Array, CborAdditionalInfo.IndefiniteLength));
            DecrementRemainingItemCount();
            PushDataItem(CborMajorType.Array, expectedNestedItems: null);
        }
    }
}
