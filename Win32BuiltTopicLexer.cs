using Microsoft.VisualBasic;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using MDSDKBase;
using MDSDKDerived;

namespace MDSDK
{
    // Also see Win32HeaderLexer.

    // First, lex all the stubs to get a mapping from a type name to a header_folder + filename (knowing that UID doesn't necessarily match file name, although it mostly does),
    // in other words a url to link to.
    // Types include structs, interfaces, primitive types, stuff like that.
    // Then, lex all the SDK header files (for functions and structs... anything else?). For each parm/member, look up the type and insert "Type:" plus a link embedded
    // into the parm declaration.

    // sdk-api stubs/vb_release
    // Get the value of the UID: field, trim it, if it starts with NA then ignore the file because it's a meaningless "namespace" file.
    // If it starts with NC then it's a callback function.
    // If it starts with NE then it's an enumeration.

    // The stub filename determines the topic url. Note that the author could have renamed the file in the content branch (as long as the UID is the same), but that's not a great idea.
}