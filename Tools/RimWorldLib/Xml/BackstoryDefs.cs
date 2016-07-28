namespace RimWorldLib.Xml
{
    using System.Collections.Generic;
    using System.Xml.Serialization;

    using CommunityCoreLibrary;

    [XmlRoot("Defs")]
    public class BackstoryDefs
    {
        #region Fields

        [XmlElement("CommunityCoreLibrary.BackstoryDef")]
        public List<BackstoryDefEx> Backstories;

        #endregion

        public class BackstoryDefEx : BackstoryDef
        {
            #region Fields

            public string author;

            #endregion
        }
    }
}