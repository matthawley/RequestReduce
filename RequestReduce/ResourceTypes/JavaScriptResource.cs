﻿using System.Collections.Generic;
using System.Text.RegularExpressions;
using RequestReduce.Configuration;
using RequestReduce.IOC;

namespace RequestReduce.ResourceTypes
{
    public class JavaScriptResource : IResourceType
    {
        private static readonly string scriptFormat = @"<script src=""{0}"" type=""text/javascript"" ></script>";
        private static readonly Regex ScriptPattern = new Regex(@"<script[^>]+src=['""]?.*?['""]?[^>]+>\s*?(</script>)?", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public string FileName
        {
            get { return "RequestReducedScript.js"; }
        }

        public IEnumerable<string> SupportedMimeTypes
        {
            get { return new[] { "text/javascript", "application/javascript", "application/x-javascript" }; }
        }

        public string TransformedMarkupTag(string url)
        {
            return string.Format(scriptFormat, url);
        }

        public Regex ResourceRegex
        {
            get { return ScriptPattern; }
        }



        public System.Func<string, string, bool> TagValidator
        {
            get 
            { 
                return ((tag, url) => 
                {
                    var urlsToIgnore = RRContainer.Current.GetInstance<RRConfiguration>().JavaScriptUrlsToIgnore;
                    foreach (var ignoredUrl in urlsToIgnore.Split(new char[]{','}, System.StringSplitOptions.RemoveEmptyEntries))
                    {
                        if(url.ToLower().Contains(ignoredUrl.ToLower()))
                            return false;
                    }
                    return true;
                }); 
            }
        }
    }
}
