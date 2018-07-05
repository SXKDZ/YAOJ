#include "comparer.h"

namespace Comparer {
    bool Comparer::FilesAreEqual(FileInfo ^ first, FileInfo ^ second) {
        if (first->Length != second->Length) {
            return false;
        }

        if (String::Equals(first->FullName, second->FullName, StringComparison::OrdinalIgnoreCase)) {
            return true;
        }

        int iterations = (int)Math::Ceiling((double)first->Length / BYTES_TO_READ);

        auto fs1 = first->OpenRead();
        auto fs2 = second->OpenRead();

        auto one = gcnew array<Byte>(BYTES_TO_READ);
        auto two = gcnew array<Byte>(BYTES_TO_READ);

        for (int i = 0; i < iterations; ++i) {
            fs1->Read(one, 0, BYTES_TO_READ);
            fs2->Read(two, 0, BYTES_TO_READ);

            if (BitConverter::ToInt64(one, 0) != BitConverter::ToInt64(two, 0)) {
                fs1->Close();
                fs2->Close();

                return false;
            }
        }

        fs1->Close();
        fs2->Close();

        return true;
    }
}
