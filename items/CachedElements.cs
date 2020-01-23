/*
Licensed to the Apache Software Foundation (ASF) under one
or more contributor license agreements.  See the NOTICE file
distributed with this work for additional information
regarding copyright ownership.  The ASF licenses this file
to you under the Apache License, Version 2.0 (the
"License"); you may not use this file except in compliance
with the License.  You may obtain a copy of the License at

  http://www.apache.org/licenses/LICENSE-2.0

Unless required by applicable law or agreed to in writing,
software distributed under the License is distributed on an
"AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY
KIND, either express or implied.  See the License for the
specific language governing permissions and limitations
under the License.
 */

using System.Collections.Generic;

namespace windowsdriver.items
{
    sealed class CachedElements
    {
        private static CachedElements instance = null;
        private static readonly object padlock = new object();
        
        private readonly Dictionary<string, AtsElement> elements;

        public CachedElements()
        {
            elements = new Dictionary<string, AtsElement>();
        }

        public static CachedElements Instance
        {
            get
            {
                lock (padlock)
                {
                    if (instance == null)
                    {
                        instance = new CachedElements();
                    }
                    return instance;
                }
            }
        }
        
        public void Add(string id, AtsElement elem)
        {
            elements.Add(id, elem);
        }

        public AtsElement GetElementById(string id)
        {
            elements.TryGetValue(id, out AtsElement value);
            return value;
        }
    }
}