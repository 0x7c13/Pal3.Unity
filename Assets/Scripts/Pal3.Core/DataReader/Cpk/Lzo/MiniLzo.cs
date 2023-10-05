/**
 * ManagedLZO.MiniLZO
 *
 * Minimalistic reimplementation of minilzo in C#
 *
 * @author Shane Eric Bryldt, Copyright (C) 2006, All Rights Reserved
 * @note Uses unsafe/fixed pointer contexts internally
 * @liscence Bound by same liscence as minilzo as below, see file COPYING
 */

/* Based on minilzo.c -- mini subset of the LZO real-time data compression library

   This file is part of the LZO real-time data compression library.

   Copyright (C) 2005 Markus Franz Xaver Johannes Oberhumer
   Copyright (C) 2004 Markus Franz Xaver Johannes Oberhumer
   Copyright (C) 2003 Markus Franz Xaver Johannes Oberhumer
   Copyright (C) 2002 Markus Franz Xaver Johannes Oberhumer
   Copyright (C) 2001 Markus Franz Xaver Johannes Oberhumer
   Copyright (C) 2000 Markus Franz Xaver Johannes Oberhumer
   Copyright (C) 1999 Markus Franz Xaver Johannes Oberhumer
   Copyright (C) 1998 Markus Franz Xaver Johannes Oberhumer
   Copyright (C) 1997 Markus Franz Xaver Johannes Oberhumer
   Copyright (C) 1996 Markus Franz Xaver Johannes Oberhumer
   All Rights Reserved.

   The LZO library is free software; you can redistribute it and/or
   modify it under the terms of the GNU General Public License,
   version 2, as published by the Free Software Foundation.

   The LZO library is distributed in the hope that it will be useful,
   but WITHOUT ANY WARRANTY; without even the implied warranty of
   MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
   GNU General Public License for more details.

   You should have received a copy of the GNU General Public License
   along with the LZO library; see the file COPYING.
   If not, write to the Free Software Foundation, Inc.,
   51 Franklin Street, Fifth Floor, Boston, MA 02110-1301, USA.

   Markus F.X.J. Oberhumer
   <markus@oberhumer.com>
   http://www.oberhumer.com/opensource/lzo/
 */

/*
 * NOTE:
 *   the full LZO package can be found at
 *   http://www.oberhumer.com/opensource/lzo/
 */

namespace Pal3.Core.DataReader.Cpk.Lzo
{
    using System;
    //using System.Diagnostics;

    public static class MiniLzo
    {
        private const uint M2_MAX_OFFSET = 0x0800;

        public static unsafe void Decompress(byte[] src, byte[] dst)
        {
            uint t = 0;
            fixed (byte* input = src, output = dst)
            {
                byte* pos = null;
                byte* ip_end = input + src.Length;
                byte* op_end = output + dst.Length;
                byte* ip = input;
                byte* op = output;
                bool match = false;
                bool match_next = false;
                bool match_done = false;
                bool copy_match = false;
                bool first_literal_run = false;
                bool eof_found = false;

                if (*ip > 17)
                {
                    t = (uint)(*ip++ - 17);
                    if (t < 4)
                        match_next = true;
                    else
                    {
                        //Debug.Assert(t > 0);
                        if ((op_end - op) < t)
                            throw new OverflowException("Output Overrun");
                        if ((ip_end - ip) < t + 1)
                            throw new OverflowException("Input Overrun");
                        do
                        {
                            *op++ = *ip++;
                        } while (--t > 0);
                        first_literal_run = true;
                    }
                }
                while (!eof_found && ip < ip_end)
                {
                    if (!match_next && !first_literal_run)
                    {
                        t = *ip++;
                        if (t >= 16)
                            match = true;
                        else
                        {
                            if (t == 0)
                            {
                                if ((ip_end - ip) < 1)
                                    throw new OverflowException("Input Overrun");
                                while (*ip == 0)
                                {
                                    t += 255;
                                    ++ip;
                                    if ((ip_end - ip) < 1)
                                        throw new OverflowException("Input Overrun");
                                }
                                t += (uint)(15 + *ip++);
                            }
                            //Debug.Assert(t > 0);
                            if ((op_end - op) < t + 3)
                                throw new OverflowException("Output Overrun");
                            if ((ip_end - ip) < t + 4)
                                throw new OverflowException("Input Overrun");
                            for (int x = 0; x < 4; ++x, ++op, ++ip)
                                *op = *ip;
                            if (--t > 0)
                            {
                                if (t >= 4)
                                {
                                    do
                                    {
                                        for (int x = 0; x < 4; ++x, ++op, ++ip)
                                            *op = *ip;
                                        t -= 4;
                                    } while (t >= 4);
                                    if (t > 0)
                                    {
                                        do
                                        {
                                            *op++ = *ip++;
                                        } while (--t > 0);
                                    }
                                }
                                else
                                {
                                    do
                                    {
                                        *op++ = *ip++;
                                    } while (--t > 0);
                                }
                            }
                        }
                    }
                    if (!match && !match_next)
                    {
                        first_literal_run = false;

                        t = *ip++;
                        if (t >= 16)
                            match = true;
                        else
                        {
                            pos = op - (1 + M2_MAX_OFFSET);
                            pos -= t >> 2;
                            pos -= *ip++ << 2;
                            if (pos < output || pos >= op)
                                throw new OverflowException("Lookbehind Overrun");
                            if ((op_end - op) < 3)
                                throw new OverflowException("Output Overrun");
                            *op++ = *pos++;
                            *op++ = *pos++;
                            *op++ = *pos++;
                            match_done = true;
                        }
                    }
                    match = false;
                    do
                    {
                        if (t >= 64)
                        {
                            pos = op - 1;
                            pos -= (t >> 2) & 7;
                            pos -= *ip++ << 3;
                            t = (t >> 5) - 1;
                            if (pos < output || pos >= op)
                                throw new OverflowException("Lookbehind Overrun");
                            if ((op_end - op) < t + 2)
                                throw new OverflowException("Output Overrun");
                            copy_match = true;
                        }
                        else if (t >= 32)
                        {
                            t &= 31;
                            if (t == 0)
                            {
                                if ((ip_end - ip) < 1)
                                    throw new OverflowException("Input Overrun");
                                while (*ip == 0)
                                {
                                    t += 255;
                                    ++ip;
                                    if ((ip_end - ip) < 1)
                                        throw new OverflowException("Input Overrun");
                                }
                                t += (uint)(31 + *ip++);
                            }
                            pos = op - 1;
                            pos -= (*(ushort*)ip) >> 2;
                            ip += 2;
                        }
                        else if (t >= 16)
                        {
                            pos = op;
                            pos -= (t & 8) << 11;

                            t &= 7;
                            if (t == 0)
                            {
                                if ((ip_end - ip) < 1)
                                    throw new OverflowException("Input Overrun");
                                while (*ip == 0)
                                {
                                    t += 255;
                                    ++ip;
                                    if ((ip_end - ip) < 1)
                                        throw new OverflowException("Input Overrun");
                                }
                                t += (uint)(7 + *ip++);
                            }
                            pos -= (*(ushort*)ip) >> 2;
                            ip += 2;
                            if (pos == op)
                                eof_found = true;
                            else
                                pos -= 0x4000;
                        }
                        else
                        {
                            pos = op - 1;
                            pos -= t >> 2;
                            pos -= *ip++ << 2;
                            if (pos < output || pos >= op)
                                throw new OverflowException("Lookbehind Overrun");
                            if ((op_end - op) < 2)
                                throw new OverflowException("Output Overrun");
                            *op++ = *pos++;
                            *op++ = *pos++;
                            match_done = true;
                        }
                        if (!eof_found && !match_done && !copy_match)
                        {
                            if (pos < output || pos >= op)
                                throw new OverflowException("Lookbehind Overrun");
                            //Debug.Assert(t > 0);
                            if ((op_end - op) < t + 2)
                                throw new OverflowException("Output Overrun");
                        }
                        if (!eof_found && t >= 2 * 4 - 2 && (op - pos) >= 4 && !match_done && !copy_match)
                        {
                            for (int x = 0; x < 4; ++x, ++op, ++pos)
                                *op = *pos;
                            t -= 2;
                            do
                            {
                                for (int x = 0; x < 4; ++x, ++op, ++pos)
                                    *op = *pos;
                                t -= 4;

                            } while (t >= 4);
                            if (t > 0)
                            {
                                do
                                {
                                    *op++ = *pos++;
                                } while (--t > 0);
                            }
                        }
                        else if(!eof_found && !match_done)
                        {
                            copy_match = false;

                            *op++ = *pos++;
                            *op++ = *pos++;
                            do
                            {
                                *op++ = *pos++;
                            } while (--t > 0);
                        }

                        if (!eof_found && !match_next)
                        {
                            match_done = false;

                            t = (uint)(ip[-2] & 3);
                            if (t == 0)
                                break;
                        }
                        if (!eof_found)
                        {
                            match_next = false;
                            //Debug.Assert(t > 0);
                            //Debug.Assert(t < 4);
                            if ((op_end - op) < t)
                                throw new OverflowException("Output Overrun");
                            if ((ip_end - ip) < t + 1)
                                throw new OverflowException("Input Overrun");
                            *op++ = *ip++;
                            if (t > 1)
                            {
                                *op++ = *ip++;
                                if (t > 2)
                                    *op++ = *ip++;
                            }
                            t = *ip++;
                        }
                    } while (!eof_found && ip < ip_end);
                }
                if (!eof_found)
                    throw new OverflowException("EOF Marker Not Found");
                else
                {
                    //Debug.Assert(t == 1);
                    if (ip > ip_end)
                        throw new OverflowException("Input Overrun");
                    else if (ip < ip_end)
                        throw new OverflowException("Input Not Consumed");
                }
            }
        }
    }
}
