#pragma once

using namespace System;
using namespace System::IO;

namespace Comparer {
    public ref class Comparer {
    private:
        static const int BYTES_TO_READ = sizeof(Int64);

    public:
        static bool FilesAreEqual(FileInfo ^ first, FileInfo ^ second);
    };
}
