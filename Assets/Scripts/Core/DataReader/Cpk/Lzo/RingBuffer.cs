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

    /// <summary>
    /// fixed sized ring buffer
    /// </summary>
    internal class RingBuffer
    {
        private readonly byte[] _buffer;
        private int _position;
        private readonly int _size;

        /// <summary>
        /// create a new RingBuffer with the specified size
        /// </summary>
        /// <param name="size">the size of the buffer</param>
        public RingBuffer(int size)
        {
            _buffer = new byte[size];
            _size = size;
        }

        /// <summary>
        /// set the position relative to the current position
        /// </summary>
        /// <remarks>wraps the position of the end is reached</remarks>
        /// <param name="offset">relative offset</param>
        public void Seek(int offset)
        {
            _position += offset;
            if (_position > _size)
            {
                do
                {
                    _position -= _size;
                } while (_position > _size);
                return;
            }
            while (_position < 0)
            {
                _position += _size;
            }
        }

        /// <summary>
        /// copies as sequence of bytes from the Ringbuffer at the specified distance into the buffer and also the RingBuffer itself
        /// </summary>
        /// <param name="buffer">An array of bytes. When this method returns, the buffer contains the specified byte array with the values between offset and (offset + count - 1) replaced by the bytes read from the RingBuffer</param>
        /// <param name="offset">The zero-based byte offset in buffer at which to begin storing the data read from the RingBuffer</param>
        /// <param name="distance">The distance to seek backwards before starting to copy</param>
        /// <param name="count">The maximum number of bytes to be read from the RingBuffer</param>
        public void Copy(byte[] buffer, int offset, int distance, int count)
        {
            if (_position - distance > 0 && _position + count < _size)
            {
                if (count < 10)
                {
                    do
                    {
                        var value = _buffer[_position - distance];
                        _buffer[_position++] = value;
                        buffer[offset++] = value;
                    } while (--count > 0);
                }
                else
                {
                    Buffer.BlockCopy(_buffer, _position - distance, buffer, offset, count);
                    Buffer.BlockCopy(buffer, offset, _buffer, _position, count);
                    _position += count;
                }
            }
            else
            {
                Seek(-distance);
                Read(buffer, offset, count);
                Seek(distance - count);
                Write(buffer, offset, count);
            }
        }

        /// <summary>
        /// reads a sequence of bytes from the RingBuffer and advances the position within the RingBuffer by the number of bytes read
        /// </summary>
        /// <param name="buffer">An array of bytes. When this method returns, the buffer contains the specified byte array with the values between offset and (offset + count - 1) replaced by the bytes read from the RingBuffer</param>
        /// <param name="offset">The zero-based byte offset in buffer at which to begin storing the data read from the RingBuffer</param>
        /// <param name="count">The maximum number of bytes to be read from the RingBuffer</param>
        public void Read(byte[] buffer, int offset, int count)
        {
            if (count < 10 && (_position + count) < _size)
            {
                do
                {
                    buffer[offset++] = _buffer[_position++];
                } while (--count > 0);
            }
            else
            {
                while (count > 0)
                {
                    var copy = _size - _position;
                    if (copy > count)
                    {
                        Buffer.BlockCopy(_buffer, _position, buffer, offset, count);
                        _position += count;
                        break;
                    }
                    Buffer.BlockCopy(_buffer, _position, buffer, offset, copy);
                    _position = 0;
                    count -= copy;
                    offset += copy;
                }
            }
        }

        /// <summary>
        /// writes a sequence of bytes to the RingBuffer and advances the current position within this RingBuffer by the number of bytes written
        /// </summary>
        /// <param name="buffer">An array of bytes. This method copies count bytes from buffer to the RingBuffer.</param>
        /// <param name="offset">The zero-based byte offset in buffer at which to begin copying bytes to the RingBuffer.</param>
        /// <param name="count">The number of bytes to be written to the RingBuffer.</param>
        public void Write(byte[] buffer, int offset, int count)
        {
            if (count < 10 && (_position + count) < _size)
            {
                do
                {
                    _buffer[_position++] = buffer[offset++];
                } while (--count > 0);
            }
            else
            {
                while (count > 0)
                {
                    var cnt = _size - _position;
                    if (cnt > count)
                    {
                        Buffer.BlockCopy(buffer, offset, _buffer, _position, count);
                        _position += count;
                        return;
                    }
                    Buffer.BlockCopy(buffer, offset, _buffer, _position, cnt);
                    _position = 0;
                    offset += cnt;
                    count -= cnt;
                }
            }
        }

        /// <summary>
        /// creates a deep clone
        /// </summary>
        /// <returns></returns>
        public RingBuffer Clone()
        {
            var result = new RingBuffer(_size) {_position = _position};
            Buffer.BlockCopy(_buffer, 0, result._buffer, 0, _size);
            return result;
        }
    }
}