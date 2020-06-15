using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Text.Json;
using System.Net.Http;
using static System.Text.Json.JsonElement;
using System.Linq;

namespace nucheck
{
    class Program
    {
        static async Task Main(string[] args)
        {
            XmlDocument doc = new XmlDocument();
            Console.WriteLine(args[0]);
            FileStream fs = new FileStream(args[0], FileMode.Open, FileAccess.Read);

            doc.Load(fs);

            XmlNodeList nodes = doc.GetElementsByTagName("PackageReference");
            for (int i = 0; i < nodes.Count; i++)
            {
                string packageName = nodes[i].Attributes.GetNamedItem("Include").Value;
                Console.WriteLine(packageName);
                Version packageVersion;
                if (nodes[i].Attributes.GetNamedItem("Version") != null)
                {
                    Version.TryParse(nodes[i].Attributes.GetNamedItem("Version").Value, out packageVersion);
                    List<Version> allTheVersions = new List<Version>();

                    HttpClient client = new HttpClient();
                    HttpResponseMessage message = await client.GetAsync($"https://api-v2v3search-0.nuget.org/search/query?q=packageid:{packageName.ToLowerInvariant()}&ignoreFilter=true");
                    string json = await message.Content.ReadAsStringAsync();

                    JsonDocument packageDoc = await System.Text.Json.JsonDocument.ParseAsync(await message.Content.ReadAsStreamAsync());
                    JsonElement root = packageDoc.RootElement;
                    ObjectEnumerator oe = root.EnumerateObject();
                    while (oe.MoveNext())
                    {
                        if (oe.Current.Name == "data")
                        {
                            ArrayEnumerator ae = oe.Current.Value.EnumerateArray();
                            while (ae.MoveNext())
                            {
                                ObjectEnumerator re = ae.Current.EnumerateObject();
                                while (re.MoveNext())
                                {
                                    if (re.Current.Name == "Version")
                                    {
                                        Version v;
                                        if (Version.TryParse(re.Current.Value.GetString(), out v))
                                        {
                                            allTheVersions.Add(v);
                                        }
                                    }
                                }
                            }

                            var newerVersions = allTheVersions.Where(ver => ver.CompareTo(packageVersion) > 0);
                            foreach (Version newerVersion in newerVersions)
                            {
                                Console.WriteLine($"{packageName} - {newerVersion}");
                            }
                        }
                    }
                }
            }
        }
    }
}
