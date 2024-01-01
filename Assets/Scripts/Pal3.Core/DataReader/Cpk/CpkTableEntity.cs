// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2024, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.Core.DataReader.Cpk
{
    using System.Runtime.InteropServices;

    // 0000 0000-0000 0000-0000 0000-0000 0001 (0x0001) 有效
    // 0000 0000-0000 0000-0000 0000-0000 0010 (0x0002) 目录
    // 0000 0000-0000 0000-0000 0000-0000 0100 (0x0004) 大文件
    // 0000 0000-0000 0000-0000 0000-0001 0000 (0x0010) 文件已被删除
    // 0000 0000-0000 0000-1000 0000-0000 0000 (0x8000) CRC有重复
    public enum CpkTableEntityFlag
    {
        None              =   0x0,
        IsValid           =   0x1,           // 是否是合法文件？
        IsDir             =   0x2,           // 是否是目录
        IsLargeFile       =   0x4,           // 大文件
        IsDeleted         =   0x10,          // 是否已删除
        IsNotCompressed   =   0x10000,       // 是否未压缩
    };

    // CPK 文件表结构
    [StructLayout(LayoutKind.Sequential)]
    public struct CpkTableEntity
    {
        // CRC较验
        [MarshalAs(UnmanagedType.U4)]
        public uint CRC;

        // 标志
        [MarshalAs(UnmanagedType.U4)]
        public uint Flag;

        // 父目录CRC
        [MarshalAs(UnmanagedType.U4)]
        public uint FatherCRC;

        // 开始位置
        [MarshalAs(UnmanagedType.U4)]
        public uint StartPos;

        // 压缩后长度
        [MarshalAs(UnmanagedType.U4)]
        public uint PackedSize;

        // 原文件长度
        [MarshalAs(UnmanagedType.U4)]
        public uint OriginSize;

        // 附加信息长度
        [MarshalAs(UnmanagedType.U4)]
        public uint ExtraInfoSize;

        public bool IsEmpty()
        {
            return Flag == (uint) CpkTableEntityFlag.None;
        }

        public bool IsCompressed()
        {
            return (Flag & (uint)CpkTableEntityFlag.IsNotCompressed) == 0;
        }

        public bool IsValid()
        {
            return (Flag & (uint)CpkTableEntityFlag.IsValid) != 0;
        }

        public bool IsDirectory()
        {
            return (Flag & (uint)CpkTableEntityFlag.IsDir) != 0;
        }

        public bool IsDeleted()
        {
            return (Flag & (uint)CpkTableEntityFlag.IsDeleted) != 0;
        }
    }
}