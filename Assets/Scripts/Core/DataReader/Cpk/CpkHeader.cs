// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2022, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Core.DataReader.Cpk
{
    using System.Runtime.InteropServices;

    // CPK 文件头结构  32 * 4 bytes
    [StructLayout(LayoutKind.Sequential)]
    internal struct CpkHeader
    {
        // 类型标志
        [MarshalAs(UnmanagedType.U4)]
        public uint Label;

        // 版本
        [MarshalAs(UnmanagedType.U4)]
        public uint Version;

        // 文件表起始字节
        [MarshalAs(UnmanagedType.U4)]
        public uint TableStart;

        // 数据块起始地址
        [MarshalAs(UnmanagedType.U4)]
        public uint DataStart;

        // 最大文件存放个数
        [MarshalAs(UnmanagedType.U4)]
        public uint MaxFileNum;

        // 当前文件个数
        [MarshalAs(UnmanagedType.U4)]
        public uint FileNum;

        // 文件是否被整理过
        [MarshalAs(UnmanagedType.U4)]
        public uint IsFormatted;

        // 文件头大小
        [MarshalAs(UnmanagedType.U4)]
        public uint SizeOfHeader;

        // 有效Table项个数，包括有效文件和碎片
        [MarshalAs(UnmanagedType.U4)]
        public uint ValidTableNum;

        // 最大Table项个数
        [MarshalAs(UnmanagedType.U4)]
        public uint MaxTableNum;

        // 碎片数目，应该为 ValidTableNum - FileNum
        [MarshalAs(UnmanagedType.U4)]
        public uint FragmentNum;

        // 当前包大小
        [MarshalAs(UnmanagedType.U4)]
        public uint PackageSize;

        // Reserved[20]
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 20)]
        public uint[] Reserved;
    }
}
