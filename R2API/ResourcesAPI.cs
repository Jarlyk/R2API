using System;
using System.Collections.Generic;
using System.Text;
using R2API.Utils;

namespace R2API {
    [R2APISubmodule]
    public static class ResourcesAPI {
        [R2APISubmoduleInit(Stage = InitStage.SetHooks)]
        internal static void InitHooks() {
        }
    }
}
