// --------------------------------------------------------------------------------------------------------------------
// <copyright file="XmlHelper.cs" company="Lost Minions">
//   Copyright (c) Lost Minions. All rights reserved.
// </copyright>
// <summary>
//   Defines the XmlHelper type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace RimWorldLib
{
    using System;
    using System.Collections;
    using System.Linq;
    using System.Reflection;
    using System.Xml.Serialization;

    using Verse;

    public static class XmlHelper
    {
        public static XmlAttributeOverrides GetCommonOverrides(Type type, XmlAttributeOverrides overrides = null)
        {
            if (overrides == null)
            {
                overrides = new XmlAttributeOverrides();
            }

            if (type == null)
            {
                return overrides;
            }

            var fields = type.GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);

            var listFields = fields.Where(f => typeof(IList).IsAssignableFrom(f.FieldType));
            foreach (var field in listFields)
            {
                // make all list types tag their items <li>
                // for some reason this is how RimWorld expects them
                overrides.Add(
                    type, 
                    field.Name, 
                    new XmlAttributes() { XmlArrayItems = { new XmlArrayItemAttribute("li") } });
            }

            var unsavedFields = fields.Where(f => f.IsDefined(typeof(UnsavedAttribute), false));
            foreach (var field in unsavedFields)
            {
                // add in XmlIgnore attributes too all items with the Unsaved attribute so we ignore them
                overrides.Add(type, field.Name, new XmlAttributes { XmlIgnore = true });
            }

            overrides = GetCommonOverrides(type.BaseType, overrides);
            return overrides;
        }
    }
}
