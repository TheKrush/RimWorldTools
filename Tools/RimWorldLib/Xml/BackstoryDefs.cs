// --------------------------------------------------------------------------------------------------------------------
// <copyright file="BackstoryDefs.cs" company="Lost Minions">
//   Copyright (c) Lost Minions. All rights reserved.
// </copyright>
// <summary>
//   Defines the BackstoryDefs type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace RimWorldLib.Xml
{
    using System.Collections.Generic;
    using System.Xml.Serialization;

    using CommunityCoreLibrary;

    [XmlRoot("Defs")]
    public class BackstoryDefs
    {
        [XmlElement("CommunityCoreLibrary.BackstoryDef")]
        public List<BackstoryDefEx> Backstories { get; set; }

        public class BackstoryDefEx : BackstoryDef
        {
            [XmlElement("author")]
            public string Author { get; set; }
        }
    }
}