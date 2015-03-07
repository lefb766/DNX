﻿// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.Framework.Runtime
{
    public sealed class FileFormatWarning
    {
        public FileFormatWarning(string message, string projectFilePath, JToken token)
        {
            Message = message;
            Path = projectFilePath;

            var lineInfo = (IJsonLineInfo)token;

            Column = lineInfo.LinePosition;
            Line = lineInfo.LineNumber;
        }

        public string Message { get; }

        public string Path { get; }

        public int Line { get; }

        public int Column { get; }

        public override bool Equals(object obj)
        {
            var other = obj as FileFormatWarning;

            return other != null &&
                Message.Equals(other.Message, System.StringComparison.Ordinal) &&
                Path.Equals(other.Path, System.StringComparison.OrdinalIgnoreCase) &&
                Line == other.Line &&
                Column == other.Line;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }
}