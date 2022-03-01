// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2022, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Editor.SourceGenerator.Base
{
    public interface ISourceGenerator
    {
        public void GenerateSourceClass(CodeWriter writer, string fileName, string nameSpace);
    }
}