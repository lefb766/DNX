// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.Framework.DesignTimeHost.Models.OutgoingMessages
{
    public class DiagnosticsMessage
    {
        public IEnumerable<DiagnosticsInfo> Errors { get; }

        public IEnumerable<DiagnosticsInfo> Warnings { get; }
    }

    public class DiagnosticsInfo
    {
        public string Path { get; }

        public int Line { get; }

        public int Column { get; }

        public string Message { get; }
    }
}