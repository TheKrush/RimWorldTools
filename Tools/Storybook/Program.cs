namespace Storybook
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Text.RegularExpressions;
    using System.Xml;
    using System.Xml.Linq;
    using System.Xml.Serialization;

    using RimWorld;

    using RimWorldLib;
    using RimWorldLib.Xml;

    internal class Program
    {
        #region Fields

        private static string storybookDir = string.Empty;

        #endregion

        private static void Main(string[] args)
        {
            if (args.Length == 1)
            {
                if (!Directory.Exists(args[0]))
                {
                    Console.WriteLine("Given directory does not exist.");
                    return;
                }

                storybookDir = args[0];
            }

            UpdateAboutDescription(UpdateBackstoryDef());

            Console.Write("Press any key to continue ...");
            Console.ReadKey();
        }

        private static XElement Sort(XElement element)
        {
            return new XElement(
                element.Name,
                from child in element.Elements() orderby child.Name.ToString() select Sort(child));
        }

        private static XDocument Sort(XDocument file) { return new XDocument(Sort(file.Root)); }

        private static void UpdateAboutDescription(string desc)
        {
            string xmlFile = storybookDir + @"About\About";

            XElement root = XElement.Load(xmlFile + ".txt");

            var description = root.Element("description");
            if (description != null)
            {
                description.SetValue(description.Value + Environment.NewLine + desc);
            }

            root.Save(xmlFile + ".xml");
        }

        private static string UpdateBackstoryDef()
        {
            string output = string.Empty;

            string xmlFile = storybookDir + @"Defs\BackstoryDefs\BackstoryDef";

            BackstoryDefs backstoryDefs;

            XmlSerializer xmlSerializer = new XmlSerializer(
                typeof(BackstoryDefs),
                XmlHelper.GetCommonOverrides(typeof(BackstoryDefs.BackstoryDefEx)));

            using (StreamReader streamReader = new StreamReader(xmlFile + ".txt"))
            using (XmlReader xmlReader = XmlReader.Create(streamReader))
            {
                backstoryDefs = (BackstoryDefs)xmlSerializer.Deserialize(xmlReader);
            }

            backstoryDefs.Backstories.Sort(
                (a, b) =>
                {
                    int slotCompare = a.slot.CompareTo(b.slot);
                    return slotCompare != 0 ? slotCompare : string.Compare(a.title, b.title, StringComparison.Ordinal);
                });

            const int TitleShortLengthWarn = 15;
            foreach (BackstoryDefs.BackstoryDefEx backstoryDef in backstoryDefs.Backstories)
            {
                // ensure proper title casing
                backstoryDef.title = char.ToUpper(backstoryDef.title[0]) + backstoryDef.title.Substring(1).ToLower();

                if (string.IsNullOrEmpty(backstoryDef.titleShort))
                {
                    backstoryDef.titleShort = backstoryDef.title;
                }

                // check short title length
                if (backstoryDef.slot == BackstorySlot.Adulthood
                    && backstoryDef.titleShort.Length >= TitleShortLengthWarn)
                {
                    Console.WriteLine(
                        "[" + backstoryDef.title + "][" + backstoryDef.titleShort + "] has short title of length of "
                        + backstoryDef.titleShort.Length);
                }

                if (backstoryDef.title == backstoryDef.titleShort)
                {
                    backstoryDef.titleShort = null;
                }

                // defName is always based on title
                backstoryDef.defName =
                    System.Threading.Thread.CurrentThread.CurrentCulture.TextInfo.ToTitleCase(
                        backstoryDef.title.ToLower()).Replace(" ", string.Empty);

                // remove excess spacing from baseDescription
                backstoryDef.baseDescription = new Regex(@"\s\s+").Replace(backstoryDef.baseDescription, " ");

                backstoryDef.workAllows.Sort();
                backstoryDef.workDisables.Sort();
                backstoryDef.skillGains.Sort((a, b) => string.Compare(a.defName, b.defName, StringComparison.Ordinal));
                backstoryDef.spawnCategories.Sort();

                backstoryDef.saveKeyIdentifier = "Storybook";
            }

            output += "Childhood" + Environment.NewLine;
            output += string.Join(
                Environment.NewLine,
                backstoryDefs.Backstories.Where(b => b.slot == BackstorySlot.Childhood)
                          .OrderBy(b => b.title)
                          .Select(b => "- " + b.title)) + Environment.NewLine;
            output += Environment.NewLine;
            output += "Adulthood" + Environment.NewLine;
            output += string.Join(
                Environment.NewLine,
                backstoryDefs.Backstories.Where(b => b.slot == BackstorySlot.Adulthood)
                          .OrderBy(b => b.title)
                          .Select(b => "- " + b.title)) + Environment.NewLine;
            output += Environment.NewLine;
            output += new string('-', 25) + Environment.NewLine + Environment.NewLine;
            output += "AUTHORS" + Environment.NewLine + Environment.NewLine;
            foreach (var group in backstoryDefs.Backstories.GroupBy(b => b.author).OrderBy(g => g.Key))
            {
                output += group.Key + Environment.NewLine;
                if (group.Any(b => b.slot == BackstorySlot.Childhood))
                {
                    output += "- childhood: "
                              + string.Join(
                                  ", ",
                                  group.Where(b => b.slot == BackstorySlot.Childhood)
                                    .OrderBy(b => b.title)
                                    .Select(b => b.title)) + Environment.NewLine;
                }

                if (group.Any(b => b.slot == BackstorySlot.Adulthood))
                {
                    output += "- adulthood: "
                              + string.Join(
                                  ", ",
                                  group.Where(b => b.slot == BackstorySlot.Adulthood)
                                    .OrderBy(b => b.title)
                                    .Select(b => b.title)) + Environment.NewLine;
                }

                output += Environment.NewLine;
            }

            // clear out author
            backstoryDefs.Backstories.ForEach(b => b.author = null);

            using (StreamWriter streamWriter = new StreamWriter(xmlFile + ".xml"))
            using (XmlWriter xmlWriter = XmlWriter.Create(streamWriter, new XmlWriterSettings() { Indent = true }))
            {
                xmlSerializer.Serialize(xmlWriter, backstoryDefs);
            }

            return output.Trim();
        }
    }
}
