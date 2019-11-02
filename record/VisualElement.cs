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

using System.Runtime.Serialization;

[DataContract(Name = "com.ats.recorder.VisualElement")]
public class VisualElement
{
    private int _hpos = 0;
    private int _vpos = 0;

    public VisualElement() { }

    public VisualElement(string tag, string criterias, int foundElements, double[] bound, long duration)
    {
        this.FoundElements = foundElements;
        this.Bound = new TestBound(bound);
        this.SearchDuration = duration;

        if (tag != string.Empty)
        {
            this.Tag = tag;
        }

        if (criterias != string.Empty)
        {
            this.Criterias = criterias;
        }
    }

    public void UpdatePosition(string hpos, string hposValue, string vpos, string vposValue)
    {
        if (hpos != string.Empty)
        {
            Hpos = hpos;
            int.TryParse(hposValue, out _hpos);
        }

        if (vpos != string.Empty)
        {
            Vpos = vpos;
            int.TryParse(vposValue, out _vpos);
        }
    }

    [DataMember(Name = "bound")]
    public TestBound Bound;

    [DataMember(Name = "criterias")]
    public string Criterias;

    [DataMember(Name = "foundElements")]
    public int FoundElements;

    [DataMember(Name = "searchDuration")]
    public double SearchDuration;

    [DataMember(Name = "tag")]
    public string Tag;

    [DataMember(Name = "hpos")]
    public string Hpos;

    [DataMember(Name = "hposValue")]
    public int HposValue
    {
        get { return _hpos; }
        set { }
    }

    [DataMember(Name = "vpos")]
    public string Vpos;

    [DataMember(Name = "vposValue")]
    public int VposValue
    {
        get { return _vpos; }
        set { }
    }
}