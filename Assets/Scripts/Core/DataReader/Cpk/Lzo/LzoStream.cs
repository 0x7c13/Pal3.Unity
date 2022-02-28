//
// Copyright (c) 2017, Bianco Veigel
//
// Permission is hereby granted, free of charge, to any person obtaining a
// copy of this software and associated documentation files (the "Software"),
// to deal in the Software without restriction, including without limitation
// the rights to use, copy, modify, merge, publish, distribute, sublicense,
// and/or sell copies of the Software, and to permit persons to whom the
// Software is furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
// FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
// DEALINGS IN THE SOFTWARE.
//

namespace Core.DataReader.Cpk.Lzo
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.IO.Compression;

    /// <summary>
    /// Wrapper Stream for lzo compression
    /// </summary>
    public sealed class LzoStream : Stream
    {
        private readonly Stream Source;
        private long? _length;
        private readonly bool _leaveOpen;
        private byte[] DecodedBuffer;
        private const int MaxWindowSize = (1 << 14) + ((255 & 8) << 11) + (255 << 6) + (255 >> 2);
        private RingBuffer RingBuffer = new RingBuffer(MaxWindowSize);
        private long OutputPosition;
        private int Instruction;
        private LzoState State;

        private enum LzoState
        {
            /// <summary>
            /// last instruction did not copy any literal
            /// </summary>
            ZeroCopy = 0,
            /// <summary>
            /// last instruction used to copy between 1 literal
            /// </summary>
            SmallCopy1 = 1,
            /// <summary>
            /// last instruction used to copy between 2 literals
            /// </summary>
            SmallCopy2 = 2,
            /// <summary>
            /// last instruction used to copy between 3 literals
            /// </summary>
            SmallCopy3 = 3,
            /// <summary>
            /// last instruction used to copy 4 or more literals
            /// </summary>
            LargeCopy = 4
        }

        /// <summary>
        /// creates a new lzo stream for decompression
        /// </summary>
        /// <param name="stream">the compressed stream</param>
        /// <param name="mode">currently only decompression is supported</param>
        public LzoStream(Stream stream, CompressionMode mode)
            :this(stream, mode, false) {}

        /// <summary>
        /// creates a new lzo stream for decompression
        /// </summary>
        /// <param name="stream">the compressed stream</param>
        /// <param name="mode">currently only decompression is supported</param>
        /// <param name="leaveOpen">true to leave the stream open after disposing the LzoStream object; otherwise, false</param>
        public LzoStream(Stream stream, CompressionMode mode, bool leaveOpen)
        {
            if (mode != CompressionMode.Decompress)
                throw new NotSupportedException("Compression is not supported");
            if (!stream.CanRead)
                throw new ArgumentException("write-only stream cannot be used for decompression");
            Source = stream;
            if (!(stream is BufferedStream))
                Source = new BufferedStream(stream);
            _leaveOpen = leaveOpen;
            DecodeFirstByte();
        }

        private void DecodeFirstByte()
        {
            Instruction = Source.ReadByte();
            if (Instruction == -1)
                throw new EndOfStreamException();
            if (Instruction > 15 && Instruction <= 17)
            {
                throw new Exception();
            }
        }

        private void Copy(byte[] buffer, int offset, int count)
        {
            Debug.Assert(count > 0);
            do
            {
                var read = Source.Read(buffer, offset, count);
                if (read == 0)
                    throw new EndOfStreamException();
                RingBuffer.Write(buffer, offset, read);
                offset += read;
                count -= read;
            } while (count > 0);
        }

        private int Decode(byte[] buffer, int offset, int count)
        {
            Debug.Assert(count > 0);
            Debug.Assert(DecodedBuffer == null);
            int read;
            var i = Instruction >> 4;
            switch (i)
            {
                case 0://Instruction <= 15
                {
                    /*
                     * Depends on the number of literals copied by the last instruction.
                     */
                    switch (State)
                    {
                        case LzoState.ZeroCopy:
                        {
                            /*
                             * this encoding will be a copy of 4 or more literal, and must be interpreted
                             * like this :                         *
                             * 0 0 0 0 L L L L  (0..15)  : copy long literal string
                             * length = 3 + (L ?: 15 + (zero_bytes * 255) + non_zero_byte)
                             * state = 4  (no extra literals are copied)
                             */
                            var length = 3;
                            if (Instruction != 0)
                            {
                                length += Instruction;
                            }
                            else
                            {
                                length += 15 + ReadLength();
                            }
                            State = LzoState.LargeCopy;
                            if (length <= count)
                            {
                                Copy(buffer, offset, length);
                                read = length;
                            }
                            else
                            {
                                Copy(buffer, offset, count);
                                DecodedBuffer = new byte[length - count];
                                Copy(DecodedBuffer, 0, length - count);
                                read = count;
                            }
                            break;
                        }
                        case LzoState.SmallCopy1:
                        case LzoState.SmallCopy2:
                        case LzoState.SmallCopy3:
                            read = SmallCopy(buffer, offset, count);
                            break;
                        case LzoState.LargeCopy:
                            read = LargeCopy(buffer, offset, count);
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                    break;
                }
                case 1://Instruction < 32
                {
                    /*
                     * 0 0 0 1 H L L L  (16..31)
                     * Copy of a block within 16..48kB distance (preferably less than 10B)
                     * length = 2 + (L ?: 7 + (zero_bytes * 255) + non_zero_byte)
                     * Always followed by exactly one LE16 :  D D D D D D D D : D D D D D D S S
                     * distance = 16384 + (H << 14) + D
                     * state = S (copy S literals after this block)
                     * End of stream is reached if distance == 16384
                     */
                    int length = (Instruction & 0x7) + 2;
                    if (length == 2)
                    {
                        length += 7 + ReadLength();
                    }
                    var s = Source.ReadByte();
                    var d = Source.ReadByte();
                    if (s != -1 && d != -1)
                    {
                        d = ((d << 8) | s) >> 2;
                        var distance = 16384 + ((Instruction & 0x8) << 11) | d;
                        if (distance == 16384)
                            return -1;

                        read = CopyFromRingBuffer(buffer, offset, count, distance, length, s & 0x3);
                        break;
                    }
                    throw new EndOfStreamException();
                }
                case 2://Instruction < 48
                case 3://Instruction < 64
                {
                    /*
                     * 0 0 1 L L L L L  (32..63)
                     * Copy of small block within 16kB distance (preferably less than 34B)
                     * length = 2 + (L ?: 31 + (zero_bytes * 255) + non_zero_byte)
                     * Always followed by exactly one LE16 :  D D D D D D D D : D D D D D D S S
                     * distance = D + 1
                     * state = S (copy S literals after this block)
                     */
                    int length = (Instruction & 0x1f) + 2;
                    if (length == 2)
                    {
                        length += 31 + ReadLength();
                    }
                    var s = Source.ReadByte();
                    var d = Source.ReadByte();
                    if (s != -1 && d != -1)
                    {
                        d = ((d << 8) | s) >> 2;
                        var distance = d + 1;

                        read = CopyFromRingBuffer(buffer, offset, count, distance, length, s & 0x3);
                        break;
                    }
                    throw new EndOfStreamException();
                }
                case 4://Instruction < 80
                case 5://Instruction < 96
                case 6://Instruction < 112
                case 7://Instruction < 128
                {
                    /*
                     * 0 1 L D D D S S  (64..127)
                     * Copy 3-4 bytes from block within 2kB distance
                     * state = S (copy S literals after this block)
                     * length = 3 + L
                     * Always followed by exactly one byte : H H H H H H H H
                     * distance = (H << 3) + D + 1
                     */
                    var length = 3 + ((Instruction >> 5) & 0x1);
                    var result = Source.ReadByte();
                    if (result != -1)
                    {
                        var distance = (result << 3) + ((Instruction >> 2) & 0x7) + 1;

                        read = CopyFromRingBuffer(buffer, offset, count, distance, length, Instruction & 0x3);
                        break;
                    }
                    throw new EndOfStreamException();
                }
                default:
                {
                    /*
                     * 1 L L D D D S S  (128..255)
                     * Copy 5-8 bytes from block within 2kB distance
                     * state = S (copy S literals after this block)
                     * length = 5 + L
                     * Always followed by exactly one byte : H H H H H H H H
                     * distance = (H << 3) + D + 1
                     */
                    var length = 5 + ((Instruction >> 5) & 0x3);
                    var result = Source.ReadByte();
                    if (result != -1)
                    {
                        var distance = (result << 3) + ((Instruction & 0x1c) >> 2) + 1;

                        read = CopyFromRingBuffer(buffer, offset, count, distance, length, Instruction & 0x3);
                        break;
                    }
                    throw new EndOfStreamException();
                }
            }
            Instruction = Source.ReadByte();
            if (Instruction != -1)
            {
                OutputPosition += read;
                return read;
            }
            throw new EndOfStreamException();
        }

        private int LargeCopy(byte[] buffer, int offset, int count)
        {
            /*
             *the instruction becomes a copy of a 3-byte block from the
             * dictionary from a 2..3kB distance, and must be interpreted like this :
             * 0 0 0 0 D D S S  (0..15)  : copy 3 bytes from 2..3 kB distance
             * length = 3
             * state = S (copy S literals after this block)
             * Always followed by exactly one byte : H H H H H H H H
             * distance = (H << 2) + D + 2049
             */
            var result = Source.ReadByte();
            if (result != -1)
            {
                var distance = (result << 2) + ((Instruction & 0xc) >> 2) + 2049;

                return CopyFromRingBuffer(buffer, offset, count, distance, 3, Instruction & 0x3);
            }
            throw new EndOfStreamException();
        }

        private int SmallCopy(byte[] buffer, int offset, int count)
        {
            /*
             * the instruction is a copy of a
             * 2-byte block from the dictionary within a 1kB distance. It is worth
             * noting that this instruction provides little savings since it uses 2
             * bytes to encode a copy of 2 other bytes but it encodes the number of
             * following literals for free. It must be interpreted like this :
             *
             * 0 0 0 0 D D S S  (0..15)  : copy 2 bytes from <= 1kB distance
             * length = 2
             * state = S (copy S literals after this block)
             * Always followed by exactly one byte : H H H H H H H H
             * distance = (H << 2) + D + 1
             */
            var h = Source.ReadByte();
            if (h != -1)
            {
                var distance = (h << 2) + ((Instruction & 0xc) >> 2) + 1;

                return CopyFromRingBuffer(buffer, offset, count, distance, 2, Instruction & 0x3);
            }

            throw new EndOfStreamException();
        }

        private int ReadLength()
        {
            int b;
            int length = 0;
            while ((b = Source.ReadByte()) == 0)
            {
                if (length >= Int32.MaxValue - 1000)
                {
                    throw new Exception();
                }
                length += 255;
            }
            if (b != -1) return length + b;
            throw new EndOfStreamException();
        }

        private int CopyFromRingBuffer(byte[] buffer, int offset, int count, int distance, int copy, int state)
        {
            Debug.Assert(copy >= 0);
            var result = copy + state;
            State = (LzoState)state;
            if (count >= result)
            {
                var size = copy;
                if (copy > distance)
                {
                    size = distance;
                    RingBuffer.Copy(buffer, offset, distance, size);
                    copy -= size;
                    var copies = copy / distance;
                    for (int i = 0; i < copies; i++)
                    {
                        Buffer.BlockCopy(buffer, offset, buffer, offset + size, size);
                        offset += size;
                        copy -= size;
                    }
                    if (copies > 0)
                    {
                        var length = size * copies;
                        RingBuffer.Write(buffer, offset - length, length);
                    }
                    offset += size;
                }
                if (copy > 0)
                {
                    if (copy < size)
                        size = copy;
                    RingBuffer.Copy(buffer, offset, distance, size);
                    offset += size;
                }
                if (state > 0)
                {
                    Copy(buffer, offset, state);
                }
                return result;
            }

            if (count <= copy)
            {
                CopyFromRingBuffer(buffer, offset, count, distance, count, 0);
                DecodedBuffer = new byte[result - count];
                CopyFromRingBuffer(DecodedBuffer, 0, DecodedBuffer.Length, distance, copy - count, state);
                return count;
            }
            CopyFromRingBuffer(buffer, offset, count, distance, copy, 0);
            var remaining = count - copy;
            DecodedBuffer = new byte[state - remaining];
            Copy(buffer, offset + copy, remaining);
            Copy(DecodedBuffer, 0, state - remaining);
            return count;
        }

        private int ReadInternal(byte[] buffer, int offset, int count)
        {
            Debug.Assert(count > 0);
            if (_length.HasValue && OutputPosition >= _length)
                return -1;
            int read;
            if (DecodedBuffer == null)
            {
                if ((read = Decode(buffer, offset, count)) >= 0) return read;
                _length = OutputPosition;
                return -1;
            }
            var decodedLength = DecodedBuffer.Length;
            if (count > decodedLength)
            {
                Buffer.BlockCopy(DecodedBuffer, 0, buffer, offset, decodedLength);
                DecodedBuffer = null;
                OutputPosition += decodedLength;
                return decodedLength;
            }
            Buffer.BlockCopy(DecodedBuffer, 0, buffer, offset, count);
            if (decodedLength > count)
            {
                var remaining = new byte[decodedLength - count];
                Buffer.BlockCopy(DecodedBuffer, count, remaining, 0, remaining.Length);
                DecodedBuffer = remaining;
            }
            else
            {
                DecodedBuffer = null;
            }
            OutputPosition += count;
            return count;
        }

        #region wrapped stream methods

        public override bool CanRead
        {
            get { return true; }
        }

        public override bool CanSeek {get { return false; }}

        public override bool CanWrite {get { return false; }}

        public override long Length
        {
            get
            {
                if (_length.HasValue)
                    return _length.Value;
                throw new NotSupportedException();
            }
        }

        public override long Position
        {
            get { return OutputPosition; }
            set
            {
                if (OutputPosition == value) return;
                Seek(value, SeekOrigin.Begin);
            }
        }

        public override void Flush()
        {
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (_length.HasValue && OutputPosition >= _length)
                return 0;
            var result = 0;
            while (count > 0)
            {
                var read = ReadInternal(buffer, offset, count);
                if (read == -1)
                    return result;
                result += read;
                offset += read;
                count -= read;
            }
            return result;
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotImplementedException();
        }

        public override void SetLength(long value)
        {
            _length = value;
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new InvalidOperationException("cannot write to readonly stream");
        }

        protected override void Dispose(bool disposing)
        {
            if (!_leaveOpen)
                Source.Dispose();
            base.Dispose(disposing);
        }

        #endregion
    }
}