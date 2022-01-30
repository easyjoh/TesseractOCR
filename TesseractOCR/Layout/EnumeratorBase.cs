﻿//
// EnumeratorBase.cs
//
// Author: Kees van Spelde <sicos2002@hotmail.com>
//
// Copyright 2021-2022 Kees van Spelde
//
// Licensed under the Apache License, Version 2.0 (the "License");
//
// - You may not use this file except in compliance with the License.
// - You may obtain a copy of the License at
//
//   http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;
using System.Runtime.InteropServices;
using TesseractOCR.Enums;
using TesseractOCR.Helpers;
using TesseractOCR.Interop;

// ReSharper disable UnusedMember.Global

namespace TesseractOCR.Layout
{
    public class EnumeratorBase
    {
        #region Fields
        /// <summary>
        ///     Handle that is returned by Tesseract engine
        /// </summary>
        protected HandleRef EngineHandleRef;

        /// <summary>
        ///     Handle that is returned by TessApi.Native.BaseApiGetIterator
        /// </summary>
        protected HandleRef IteratorHandleRef;

        /// <summary>
        ///     <see cref="Pix.Image"/>
        /// </summary>
        protected HandleRef ImageHandleRef;

        /// <summary>
        ///     The <see cref="PageIteratorLevel"/>
        /// </summary>
        protected PageIteratorLevel PageIteratorLevel;

        /// <summary>
        ///     Flag to check if we are doing our first enumeration
        /// </summary>
        protected bool First = true;

        /// <summary>
        ///     When set then we log at a debug level
        /// </summary>
        protected bool LogDebug;
        #endregion

        #region Properties
        /// <summary>
        ///     Returns <c>true</c> if the iterator is at the first element at the current <see cref="PageIteratorLevel"/>
        /// </summary>
        /// <returns><c>true</c> when at the beginning</returns>
        public bool IsAtBeginning => TessApi.Native.PageIteratorIsAtBeginningOf(IteratorHandleRef, PageIteratorLevel) == Constants.True;

        /// <summary>
        ///     Returns the text for the current <see cref="PageIteratorLevel"/>
        /// </summary>
        public string Text => TessApi.ResultIteratorGetUTF8Text(IteratorHandleRef, PageIteratorLevel);

        /// <summary>
        ///     Returns the confidence for the current <see cref="PageIteratorLevel"/>
        /// </summary>
        /// <returns></returns>
        public float Confidence => TessApi.Native.ResultIteratorGetConfidence(IteratorHandleRef, PageIteratorLevel);

        /// <summary>
        ///     Returns a binary (gray) <see cref="Pix.Image"/> at the current <see cref="PageIteratorLevel"/>
        /// </summary>
        /// <returns>The <see cref="Pix.Image"/> or <c>null</c> when it fails</returns>
        public Pix.Image BinaryImage => Pix.Image.Create(TessApi.Native.PageIteratorGetBinaryImage(IteratorHandleRef, PageIteratorLevel));

        /// <summary>
        ///     Returns a <see cref="Pix.Image"/> from what is seen at the current <see cref="PageIteratorLevel"/>>
        /// </summary>
        /// <remarks>
        ///     Image.Item1 = The image<br/>
        ///     Image.Item2 = The left coordinate of the image<br/>
        ///     Image.Item3 = The top coordinate of the image<br/>
        /// </remarks>
        public Tuple<Pix.Image, int, int> Image
        {
            get
            {
                var image = Pix.Image.Create(TessApi.Native.PageIteratorGetImage(IteratorHandleRef, PageIteratorLevel, 0, ImageHandleRef, out var left, out var top));
                return new Tuple<Pix.Image, int, int>(image, top, left);
            }
        }

        /// <summary>
        ///     Returns the bounding <see cref="Rect"/> of the current <see cref="PageIteratorLevel"/>
        /// </summary>
        /// <returns>Returns the <see cref="Rect"/> or <c>null</c> when it fails</returns>
        public Rect? BoundingBox
        {
            get
            {
                if (TessApi.Native.PageIteratorBoundingBox(IteratorHandleRef, PageIteratorLevel, out var x1, out var y1, out var x2, out var y2) == Constants.True)
                    return Rect.FromCoords(x1, y1, x2, y2);

                return null;
            }
        }

        /// <summary>
        ///     Returns the baseline of the current <see cref="PageIteratorLevel"/>
        /// </summary>
        /// <remarks>
        ///     The baseline is the line that passes through (x1, y1) and (x2, y2).
        ///     WARNING: with vertical text, baselines may be vertical! Returns <c>false</c> if there is no baseline at the current
        ///     position
        /// </remarks>
        /// <returns>Returns the <see cref="Rect"/> or <c>null</c> when it fails</returns>
        public Rect? Baseline
        {
            get
            {
                if (TessApi.Native.PageIteratorBaseline(IteratorHandleRef, PageIteratorLevel, out var x1, out var y1, out var x2, out var y2) == Constants.True)
                    return Rect.FromCoords(x1, y1, x2, y2);

                return null;
            }
        }

        /// <summary>
        ///     Returns the <see cref="ElementProperties"/> of the current <see cref="PageIteratorLevel"/>
        /// </summary>
        public ElementProperties Properties
        {
            get
            {
                TessApi.Native.PageIteratorOrientation(IteratorHandleRef, 
                    out var orientation, 
                    out var writingDirection,
                    out var textLineOrder,
                    out var deskewAngle);

                return new ElementProperties(orientation, textLineOrder, writingDirection, deskewAngle);
            }
        }
        #endregion

        #region Reset
        /// <summary>
        ///     Resets the enumerator to the first element
        /// </summary>
        public void Reset()
        {
            throw new NotSupportedException($"Not supported at the '{PageIteratorLevel}'");
        }
        #endregion

        #region MoveNext
        /// <summary>
        ///     Moves to the next element at the current <see cref="PageIteratorLevel"/>
        /// </summary>
        /// <returns><c>true</c> when there is a next <see cref="Paragraph"/>, otherwise <c>false</c></returns>
        public bool MoveNext()
        {
            var result = TessApi.Native.PageIteratorNext(IteratorHandleRef, PageIteratorLevel) == Constants.True;

            if (result)
            {
                if (LogDebug)
                    Logger.LogDebug($"Moved to the next '{PageIteratorLevel}' element");
                else
                    Logger.LogInformation($"Moved to the next '{PageIteratorLevel}' element");
            }
            else
                Logger.LogError($"Failed to move to the next '{PageIteratorLevel}' element");

            return result;
        }
        #endregion


        #region Dispose
        /// <summary>
        ///     Does not do a thing, we have to implement it because of the IEnumerator interface
        /// </summary>
        public void Dispose()
        {
            // We have to implement this method because of the IEnumerator interface
            // but we have nothing to do here so just ignore it
        }
        #endregion
    }
}
