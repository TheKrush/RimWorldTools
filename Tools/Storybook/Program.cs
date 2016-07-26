// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Program.cs" company="">
// </copyright>
// <summary>
//   This program simply does some cleanup converting my version of the About\About.xml and
//   Defs\BackstoryDefs\BackstoryDefs.xml (mine are the .txt) to remove some things I use for generating part of the
//   About.xml description.
// </summary>
// --------------------------------------------------------------------------------------------------------------------
namespace Storybook
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text.RegularExpressions;
    using System.Xml.Linq;

    internal class Program
    {
        #region Fields

        private static string StorybookDir = string.Empty;

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

                StorybookDir = args[0];
            }

            UpdateAboutDescription(UpdateBackstoryDef());
        }

        private static XElement Sort(XElement element)
        {
            return new XElement(
                element.Name, 
                from child in element.Elements() orderby child.Name.ToString() select Sort(child));
        }

        private static XDocument Sort(XDocument file)
        {
            return new XDocument(Sort(file.Root));
        }

        private static void UpdateAboutDescription(string desc)
        {
            string xmlDir = StorybookDir + @"About\";
            string xmlFileIn = @"About.txt";
            string xmlFileOut = @"About.xml";

            XElement root = XElement.Load(xmlDir + xmlFileIn);

            var description = root.Element("description");
            if (description != null)
            {
                description.SetValue(description.Value + Environment.NewLine + desc);
            }

            root.Save(xmlDir + xmlFileOut);
        }

        private static string UpdateBackstoryDef()
        {
            string output = string.Empty;

            string xmlDir = StorybookDir + @"Defs\BackstoryDefs\";
            string xmlFileIn = @"BackstoryDef.txt";
            string xmlFileOut = @"BackstoryDef.xml";

            XElement root = XElement.Load(xmlDir + xmlFileIn);
            var sortedElements =
                root.Elements("CommunityCoreLibrary.BackstoryDef")
                    .OrderByDescending(x => (string)x.Element("slot"))
                    .ThenBy(x => (string)x.Element("title"));

            Dictionary<string, List<KeyValuePair<string, string>>> authors =
                new Dictionary<string, List<KeyValuePair<string, string>>>();

            string prevSlot = string.Empty;
            foreach (XElement xElement in sortedElements)
            {
                string slot = (string)xElement.Element("slot");
                if (slot != prevSlot)
                {
                    if (!string.IsNullOrEmpty(prevSlot))
                    {
                        output += Environment.NewLine;
                    }

                    output += slot + Environment.NewLine;
                    prevSlot = slot;
                }

                // ensure proper title casing
                string title = (string)xElement.Element("title");
                title = char.ToUpper(title[0]) + title.Substring(1).ToLower();
                xElement.SetElementValue("title", title);

                string titleShort = (string)xElement.Element("titleShort");
                const int TitleShortLengthMax = 12;
                int titleShortLength = (string.IsNullOrEmpty(titleShort) ? title : titleShort).Length;
                if (titleShortLength > TitleShortLengthMax)
                {
                    Console.WriteLine(
                        "[" + title + "][" + titleShort + "] has short title of length of " + titleShortLength);
                }

                if (!string.IsNullOrEmpty(titleShort) && title == titleShort)
                {
                    xElement.SetElementValue("titleShort", null);
                }

                // defName is always based on title
                string uppercaseTitle =
                    System.Threading.Thread.CurrentThread.CurrentCulture.TextInfo.ToTitleCase(title.ToLower());
                string defName = uppercaseTitle.Replace(" ", string.Empty);
                xElement.SetElementValue("defName", defName);

                // remove excess spacing from baseDescription
                string baseDescription = (string)xElement.Element("baseDescription");
                baseDescription = new Regex(@"\s\s+").Replace(baseDescription, " ");
                xElement.SetElementValue("baseDescription", baseDescription);

                var workDisablesElement = xElement.Element("workDisables");
                if (workDisablesElement != null)
                {
                    var workDisables = workDisablesElement.Elements("li");
                    if (workDisables.Any())
                    {
                        workDisables = workDisables.OrderBy(x => (string)x).ToArray();
                        workDisablesElement.ReplaceAll(workDisables);
                    }
                    else
                    {
                        xElement.SetElementValue("workDisables", null);
                    }
                }

                if (xElement.Element("skillGains") != null)
                {
                    var skillGains = xElement.Element("skillGains").Elements("li");
                    if (skillGains.Any())
                    {
                        skillGains = skillGains.OrderBy(x => (string)x.Element("defName")).ToArray();
                        xElement.Element("skillGains").ReplaceAll(skillGains);
                    }
                    else
                    {
                        xElement.SetElementValue("skillGains", null);
                    }
                }

                if (xElement.Element("spawnCategories") != null)
                {
                    var spawnCategories = xElement.Element("spawnCategories").Elements("li");
                    if (spawnCategories.Any())
                    {
                        spawnCategories = spawnCategories.OrderBy(x => (string)x).ToArray();
                        xElement.Element("spawnCategories").ReplaceAll(spawnCategories);
                    }
                    else
                    {
                        xElement.SetElementValue("spawnCategories", null);
                    }
                }

                // remove any saveKeyIdentifier
                xElement.SetElementValue("saveKeyIdentifier", null);

                // move author to the last element
                string author = (string)xElement.Element("author") ?? "Krush";
                xElement.SetElementValue("author", null);
                xElement.SetElementValue("author", null); // remove author

                output += "- " + uppercaseTitle + Environment.NewLine;

                if (!authors.ContainsKey(author))
                {
                    authors.Add(author, new List<KeyValuePair<string, string>>());
                }

                authors[author].Add(new KeyValuePair<string, string>(slot, uppercaseTitle));
            }

            root.ReplaceAll(sortedElements);
            root.Save(xmlDir + xmlFileOut);

            output += Environment.NewLine;
            output += new string('-', 25) + Environment.NewLine;
            output += Environment.NewLine;
            output += "AUTHORS" + Environment.NewLine;
            output += Environment.NewLine;
            foreach (string author in authors.Keys.OrderBy(a => a))
            {
                output += author + Environment.NewLine;
                var childhood = authors[author].Where(t => t.Key == "Childhood");
                if (childhood.Any())
                {
                    output += "- childhood: " + string.Join(", ", childhood.OrderBy(t => t.Value).Select(t => t.Value))
                              + Environment.NewLine;
                }

                var adulthood = authors[author].Where(t => t.Key == "Adulthood");
                if (adulthood.Any())
                {
                    output += "- adulthood: " + string.Join(", ", adulthood.OrderBy(t => t.Value).Select(t => t.Value))
                              + Environment.NewLine;
                }

                output += Environment.NewLine;
            }

            return output.Trim();
        }
    }
}