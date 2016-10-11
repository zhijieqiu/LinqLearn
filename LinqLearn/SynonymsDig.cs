using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using JiebaNet.Segmenter;

namespace LinqLearn
{
    class SynonymsDig
    {
        static Dictionary<string,HashSet<string>> strToList = new DefaultDictionary<string, HashSet<string>>();
        static Dictionary<string,string> strToParent = new Dictionary<string, string>();

        private static string GetFather(string s)
        {
            if (strToParent.ContainsKey(s) == false)
            {
                strToParent[s] = s;
                return s;
            }
            if (strToParent[s] != s)
                strToParent[s] = GetFather(strToParent[s]);
            return strToParent[s];
        }
        private static void Union(string x,string y)
        {
            string x_p = GetFather(x), y_p = GetFather(y);
            if (x_p != y_p)
            {
                strToParent[y_p] = x_p;
                strToList[x_p].UnionWith(strToList[y_p]);
                strToList[y_p].Clear();
            }
        }
        public static void GenerateCandidate(string candidateFile, string finalFile)
        {
            StreamReader sr = new StreamReader(candidateFile);
            string line = null;
            while ((line = sr.ReadLine()) != null)
            {
                line = line.Trim();
                string[] tokens = line.Split('\t');
                foreach (string _s in tokens)
                {
                    string s = GetFather(_s);
                    if (strToList.ContainsKey(s) == false)
                    {
                        strToList[s] = new HashSet<string>();
                        
                    }
                    foreach (string token in tokens)
                    {

                        
                        if (strToList[s].Contains(token) == false)
                        {
                            strToList[s].Add(token);
                        }
                    }
                    
                }
                for (int i = 1; i < tokens.Length; i++)
                {
                    Union(tokens[0],tokens[i]);
                }
            }
            
            sr.Close();
            StreamWriter sw = new StreamWriter(finalFile);
            foreach (var kv in strToList)
            {
                string s = "";
                if (strToParent[kv.Key] != kv.Key)
                    continue;
                foreach (string word in kv.Value)
                {
                    s += " " + word;
                }
                s= s.Trim();
                sw.WriteLine(s);
            }
            sw.Close();
        }

        public static void Main3()
        {
            GenerateCandidate("D:\\zhijie\\regResult\\finalSynonyms.txt", "D:\\zhijie\\regResult\\finalSynonyms2.txt");
            return;
        }
    }
}
