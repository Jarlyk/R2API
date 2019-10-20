﻿using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using Object = UnityEngine.Object;

namespace R2API {
    public interface IResourcesProvider {
        string ModPrefix { get; }

        Object Load(string path, Type type);

        ResourceRequest LoadAsync(string path, Type type);

        Object[] LoadAll(Type type);
    }
}
