using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using System.IO;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using System.Xml;
using System.Diagnostics;
using DocumentFormat.OpenXml;
using System.Text.RegularExpressions;
using System.Reflection;
using System.Runtime.Remoting.Metadata.W3cXsd2001;
using System.Threading;
using DocumentFormat.OpenXml.Wordprocessing;
using Newtonsoft.Json;
using JiebaNet.Segmenter;

namespace LinqLearn
{
   
    class KeyWordTransform
    {
        private List<string> GetSheetNames(WorkbookPart workBookPart)
        {
            List<string> sheetNames = new List<string>();
            Sheets sheets = workBookPart.Workbook.Sheets;
            foreach (Sheet sheet in sheets)
            {
                string sheetName = sheet.Name;
                if (!string.IsNullOrEmpty(sheetName))
                {
                    sheetNames.Add(sheetName);
                }
            }
            return sheetNames;
        }
        
        /// <summary>
        /// 根据WorkbookPart和sheetName获取该Sheet下所有Row数据
        /// </summary>
        /// <param name="workBookPart">WorkbookPart对象</param>
        /// <param name="sheetName">SheetName</param>
        /// <returns>该SheetName下的所有Row数据</returns>
        public IEnumerable<Row> GetWorkBookPartRows(WorkbookPart workBookPart, string sheetName)
        {
            IEnumerable<Row> sheetRows = null;
            //根据表名在WorkbookPart中获取Sheet集合
            IEnumerable<Sheet> sheets = workBookPart.Workbook.Descendants<Sheet>().Where(s => s.Name == sheetName);
            if (sheets.Count() == 0)
            {
                return null;//没有数据
            }

            WorksheetPart workSheetPart = workBookPart.GetPartById(sheets.First().Id) as WorksheetPart;
            //获取Excel中得到的行
            sheetRows = workSheetPart.Worksheet.Descendants<Row>();

            return sheetRows;
        }

        private DataTable WorkSheetToTable(WorkbookPart workBookPart, string sheetName)
        {
            //创建Table
            DataTable dataTable = new DataTable(sheetName);

            //根据WorkbookPart和sheetName获取该Sheet下所有行数据
            IEnumerable<Row> sheetRows = GetWorkBookPartRows(workBookPart, sheetName);
            if (sheetRows == null || sheetRows.Count() <= 0)
            {
                return null;
            }

            //将数据导入DataTable,假定第一行为列名,第二行以后为数据
            foreach (Row row in sheetRows)
            {
                //获取Excel中的列头
                if (row.RowIndex == 1)
                {
                    List<DataColumn> listCols = GetDataColumn(row, workBookPart);
                    dataTable.Columns.AddRange(listCols.ToArray());
                }
                else
                {
                    //Excel第二行同时为DataTable的第一行数据
                    DataRow dataRow = GetDataRow(row, dataTable, workBookPart);
                    if (dataRow != null)
                    {
                        dataTable.Rows.Add(dataRow);
                    }
                }
            }
            return dataTable;
        }
        public DataTable ExcelToDataTable(string sheetName, string filePath)
        {
            
            DataTable dataTable = new DataTable();
            try
            {
                //根据Excel流转换为spreadDocument对象
                using (SpreadsheetDocument spreadDocument = SpreadsheetDocument.Open(filePath, false))//Excel文档包
                {
                    //Workbook workBook = spreadDocument.WorkbookPart.Workbook;//主文档部件的根元素
                    //Sheets sheeets = workBook.Sheets;//块级结构（如工作表、文件版本等）的容器
                    WorkbookPart workBookPart = spreadDocument.WorkbookPart;
                    //获取Excel中SheetName集合
                    List<string> sheetNames = GetSheetNames(workBookPart);

                    if (sheetNames.Contains(sheetName))
                    {
                        //根据WorkSheet转化为Table
                        dataTable = WorkSheetToTable(workBookPart, sheetName);
                    }
                }
            }
            catch (Exception exp)
            {
                //throw new Exception("可能Excel正在打开中,请关闭重新操作！");
                Console.WriteLine(exp.Message);
            }
            return dataTable;
        }
        private List<DataColumn> GetDataColumn(Row row, WorkbookPart workBookPart)
        {
            List<DataColumn> listCols = new List<DataColumn>();
            foreach (Cell cell in row)
            {
                string cellValue = GetCellValue(cell, workBookPart);
                DataColumn col = new DataColumn(cellValue);
                listCols.Add(col);
            }
            return listCols;
        }

        /// <summary>
        /// 根据Excel行\数据库表\WorkbookPart对象获取数据DataRow
        /// </summary>
        /// <param name="row">Excel中行对象</param>
        /// <param name="dateTable">数据表</param>
        /// <param name="workBookPart">WorkbookPart对象</param>
        /// <returns>返回一条数据记录</returns>
        private DataRow GetDataRow(Row row, DataTable dateTable, WorkbookPart workBookPart)
        {
            //读取Excel中数据,一一读取单元格,若整行为空则忽视该行
            DataRow dataRow = dateTable.NewRow();
            IEnumerable<Cell> cells = row.Elements<Cell>();

            int cellIndex = 0;//单元格索引
            int nullCellCount = cellIndex;//空行索引
            foreach (Cell cell in row)
            {
                string cellVlue = GetCellValue(cell, workBookPart);
                if (string.IsNullOrEmpty(cellVlue))
                {
                    nullCellCount++;
                }

                dataRow[cellIndex] = cellVlue;
                cellIndex++;
            }
            if (nullCellCount == cellIndex)//剔除空行
            {
                dataRow = null;//一行中单元格索引和空行索引一样
            }
            return dataRow;
        }
        private List<string> GetNumberFormatsStyle(WorkbookPart workBookPart)
        {
            List<string> dicStyle = new List<string>();
            Stylesheet styleSheet = workBookPart.WorkbookStylesPart.Stylesheet;
            OpenXmlElementList list = styleSheet.NumberingFormats.ChildElements;//获取NumberingFormats样式集合

            foreach (var element in list)//格式化节点
            {
                if (element.HasAttributes)
                {
                    using (OpenXmlReader reader = OpenXmlReader.Create(element))
                    {
                        if (reader.Read())
                        {
                            if (reader.Attributes.Count > 0)
                            {
                                string numFmtId = reader.Attributes[0].Value;//格式化ID
                                string formatCode = reader.Attributes[1].Value;//格式化Code
                                dicStyle.Add(formatCode);//将格式化Code写入List集合
                            }
                        }
                    }
                }
            }
            return dicStyle;
        }
        public string GetCellValue(Cell cell, WorkbookPart workBookPart)
        {
            string cellValue = string.Empty;
            if (cell.ChildElements.Count == 0)//Cell节点下没有子节点
            {
                return cellValue;
            }
            string cellRefId = cell.CellReference.InnerText;//获取引用相对位置
            string cellInnerText = cell.CellValue.InnerText;//获取Cell的InnerText
            cellValue = cellInnerText;//指定默认值(其实用来处理Excel中的数字)

            //获取WorkbookPart中NumberingFormats样式集合
            List<string> dicStyles = GetNumberFormatsStyle(workBookPart);
            //获取WorkbookPart中共享String数据
            SharedStringTable sharedTable = workBookPart.SharedStringTablePart.SharedStringTable;

            try
            {
                EnumValue<CellValues> cellType = cell.DataType;//获取Cell数据类型
                if (cellType != null)//Excel对象数据
                {
                    switch (cellType.Value)
                    {
                        case CellValues.SharedString://字符串
                            //获取该Cell的所在的索引
                            int cellIndex = int.Parse(cellInnerText);
                            cellValue = sharedTable.ChildElements[cellIndex].InnerText;
                            break;
                        case CellValues.Boolean://布尔
                            cellValue = (cellInnerText == "1") ? "TRUE" : "FALSE";
                            break;
                        case CellValues.Date://日期
                            cellValue = Convert.ToDateTime(cellInnerText).ToString();
                            break;
                        case CellValues.Number://数字
                            cellValue = Convert.ToDecimal(cellInnerText).ToString();
                            break;
                        default: cellValue = cellInnerText; break;
                    }
                }
                else//格式化数据
                {
                    if (dicStyles.Count > 0 && cell.StyleIndex != null)//对于数字,cell.StyleIndex==null
                    {
                        int styleIndex = Convert.ToInt32(cell.StyleIndex.Value);
                        string cellStyle = dicStyles[styleIndex - 1];//获取该索引的样式
                        if (cellStyle.Contains("yyyy") || cellStyle.Contains("h")
                            || cellStyle.Contains("dd") || cellStyle.Contains("ss"))
                        {
                            //如果为日期或时间进行格式处理,去掉“;@”
                            cellStyle = cellStyle.Replace(";@", "");
                            while (cellStyle.Contains("[") && cellStyle.Contains("]"))
                            {
                                int otherStart = cellStyle.IndexOf('[');
                                int otherEnd = cellStyle.IndexOf("]");

                                cellStyle = cellStyle.Remove(otherStart, otherEnd - otherStart + 1);
                            }
                            double doubleDateTime = double.Parse(cellInnerText);
                            DateTime dateTime = DateTime.FromOADate(doubleDateTime);//将Double日期数字转为日期格式
                            if (cellStyle.Contains("m")) { cellStyle = cellStyle.Replace("m", "M"); }
                            if (cellStyle.Contains("AM/PM")) { cellStyle = cellStyle.Replace("AM/PM", ""); }
                            cellValue = dateTime.ToString(cellStyle);//不知道为什么Excel 2007中格式日期为yyyy/m/d
                        }
                        else//其他的货币、数值
                        {
                            cellStyle = cellStyle.Substring(cellStyle.LastIndexOf('.') - 1).Replace("\\", "");
                            decimal decimalNum = decimal.Parse(cellInnerText);
                            cellValue = decimal.Parse(decimalNum.ToString(cellStyle)).ToString();
                        }
                    }
                }
            }
            catch (Exception exp)
            {
                //string expMessage = string.Format("Excel中{0}位置数据有误,请确认填写正确！", cellRefId);
                //throw new Exception(expMessage);
                cellValue = "N/A";
            }
            return cellValue;
        }
        public static void test()
        {
            string fileName = @"D:\zhijie\1.txt";
            StreamReader sr = new StreamReader(fileName);
            StreamWriter sw = new StreamWriter(@"D:\zhijie\2.txt");
            while (true)
            {
                string line = "";
                string curLine = "";
                while((curLine= sr.ReadLine()) != null)
                {
                    curLine = curLine.Trim();
                    line += " " + curLine;
                    string[] tokens = line.Split("\t".ToArray(),StringSplitOptions.RemoveEmptyEntries);

                    if (tokens.Length == 4)
                    {
                        line = line.Trim();
                        sw.WriteLine(line);
                        break;
                    }
                }
                if (curLine == null) break;
            }
            sr.Close();
            sw.Close();
        }
        private class QAItem
        {
            public string RoutePath { get; set; }
            public string QKeywords { get; set; }
            public string Question { get; set; }
            public string AKeywords { get; set; }
            public List<string> AnswerLines { get; set; }
        }
        public static void generateRoute(string inputFileName, string outPutFileName)
        {
            string fileName = inputFileName;
            StreamReader sr = new StreamReader(fileName);
            StreamWriter sw = new StreamWriter(outPutFileName);
            List<string> lines = new List<string>();
            string lline = null;
            while ((lline = sr.ReadLine()) != null)
            {
                lines.Add(lline);
            }
            int i = 0;
            List<QAItem> qas = new List<QAItem>();
            while (i < lines.Count())
            {
                string line = "";
                string curLine = "";
                QAItem qa = new QAItem();
                while (i < lines.Count())
                {
                    string tmpLine = lines[i];
                    curLine = tmpLine;
                    line +=  curLine+"\n";
                    
                    string[] tokens = line.Split("\t".ToArray(), StringSplitOptions.None);
                    i++;
                    if (tokens.Length == 4)
                    {
                        while (i < lines.Count())
                        {
                            curLine = lines[i];
                            if (curLine.Contains("\t") == false)
                                line +=  curLine+"\n";
                            else
                            {
                                break;
                            }
                            i++;
                        }
                        //sw.WriteLine(line.Trim());
                        break;
                    }
                }
                if (line.StartsWith("\n")) line = line.Substring(1);
                string[] ftks = line.Split("\t".ToArray(), StringSplitOptions.None);
                qa.RoutePath = ftks[0];
                qa.RoutePath = qa.RoutePath.Replace(" (", "|");
                qa.RoutePath = qa.RoutePath.Substring(0, qa.RoutePath.Length - 1);
                qa.QKeywords = ftks[1];
                qa.Question = ftks[2];
                qa.AKeywords = "";
                qa.AnswerLines = ftks[3].Split("\n".ToArray(), StringSplitOptions.None).ToList();
                qas.Add(qa);
            }
            string[] splitSegs = new string[] {"\"MirrorLink","MirrorLink","CarPlay","Android Auto", "\"CarPlay","\"Android Auto" };
            Dictionary<string, string> mydict = splitSegs.ToDictionary(x => x);
            foreach (QAItem qa in qas)
            {
                bool hasSubtitle = false;
                foreach (string s in qa.AnswerLines)
                {
                    if (mydict.ContainsKey(s))
                    {
                        hasSubtitle = true;
                        break;
                    }
                }
                if (hasSubtitle)
                {
                    Dictionary<string,int> stoi = new Dictionary<string, int>();
                    int index = 0;
                    foreach (string s in qa.AnswerLines)
                    {
                        if (mydict.ContainsKey(s))
                        {
                            stoi[s] = index;
                        }
                        index++;
                    }
                    var titlesLines = stoi.ToList().OrderBy(x => x.Value).ToList();
                    
                    for (int j = 0; j < titlesLines.Count(); j++)
                    {
                        
                        string routePath = qa.RoutePath + "|" + titlesLines[j].Key.Trim("\"".ToArray());
                        string answerLine = "";
                        int upperCnt = j == titlesLines.Count() - 1 ? qa.AnswerLines.Count() : titlesLines[j + 1].Value;
                        for (int k = titlesLines[j].Value + 1; k < upperCnt; k++)
                        {
                            if (k == titlesLines[j].Value + 1) answerLine += qa.AnswerLines[k];
                            else answerLine += "\n" + qa.AnswerLines[k];
                        }
                        answerLine = answerLine.Trim("\n".ToArray());
                        sw.Write("{0}\t{1}\t{2}\t{3}\t{4}\n", routePath, qa.QKeywords, qa.Question, qa.AKeywords,
                            answerLine);
                    }
                }
                else
                {
                    string answerLine = string.Join("\n", qa.AnswerLines);
                    answerLine=answerLine.Trim("\n".ToArray());
                    sw.Write("{0}\t{1}\t{2}\t{3}\t{4}\n",qa.RoutePath,qa.QKeywords,qa.Question,qa.AKeywords,answerLine);
                }
            }
            sr.Close();
            sw.Close();
        }
        public static void test2(string inputFileName,string outPutFileName)
        {
            string fileName = inputFileName;
            StreamReader sr = new StreamReader(fileName);
            StreamWriter sw = new StreamWriter(outPutFileName);
            List<string> lines=  new List<string>();
            string lline = null;
            while ((lline = sr.ReadLine())!=null)
            {
                lines.Add(lline);
            }
            int i = 0;
            while (i<lines.Count())
            {
                string line = "";
                string curLine = "";
                while (i < lines.Count())
                {
                    string tmpLine = lines[i].Trim();
                    if (tmpLine.Length == 0)
                        curLine = tmpLine;
                    else curLine = lines[i];
                    line += " " + curLine;
                    string[] tokens = line.Split("\t".ToArray(), StringSplitOptions.RemoveEmptyEntries);
                    i++;
                    if (tokens.Length == 4)
                    {
                        while (i < lines.Count())
                        {
                            curLine = lines[i].Trim();
                            if (curLine.Contains("\t") == false)
                                line += " " + curLine;
                            else
                            {
                                break;
                            }
                            i++;
                        }
                        sw.WriteLine(line.Trim());
                        break;
                    }
                }
                
            }
            sr.Close();
            sw.Close();
        }
        public static void RegGenerate(List<HashSet<string>> ls,int index,string reg,List<string> allRegex)
        {
            if (index == ls.Count())
            {
                allRegex.Add(reg);
                return;
            }
            string tmp = "";
            if (ls.Count() == 1)
            {
                foreach (string key in ls[index])
                {
                    tmp += key;
                }
            }
            else if(ls.Count()>1)
            {
                int i = 0;
                tmp += "(";
                foreach (string key in ls[index])
                {
                    if (i == 0)
                        tmp += key;
                    else tmp += "|" + key;
                    i++;
                }
                tmp += ")";
            }

            string s = reg + tmp;
            if (index > 0) s = reg + "[\\s\\S]*" + tmp;
            RegGenerate(ls, index + 1, s, allRegex);
        }

        private static void swap(List<HashSet<string>> ls, int i, int j)
        {
            HashSet<string> tmp = ls[i];
            ls[i] = ls[j];
            ls[j] = tmp;
        }
        public static void RegGenerate3(List<HashSet<string>> ls, int index, string reg, List<string> allRegex)
        {
            if (index == ls.Count() - 1)
            {
                allRegex.Add(RegGenerate2(ls));
            }
            else
            {
                for (int j = index; j < ls.Count(); j++)
                {
                    swap(ls,index,j);
                    RegGenerate3(ls, index + 1, reg, allRegex);
                    swap(ls, j, index);
                }
            }
        }
        public static string RegGenerate2(List<HashSet<string>> ls)
        {
            string ret = "";
            int i = 0;
            int j = 0;
            foreach (HashSet<string> h in ls)
            {

                if(h==null||h.Count()==0) continue;
                if (j != 0)
                {
                    ret += "[\\s\\S]*";
                }
                if (h.Count() == 1)
                {
                    i = 0;
                    foreach (var key in h)
                    {
                        if (i == 0)
                            ret += key.Trim();
                        else
                            ret += "|" + key.Trim();
                        i++;
                    }
                    j++;
                    continue;
                }
                ret += "(";
                i = 0;
                foreach (var key in h)
                {
                    if (i == 0)
                        ret += key.Trim();
                    else
                        ret += "|" + key.Trim();
                    i++;
                }
                ret += ")";
                j++;
            }
            return ret;
        }
        public static void transform()
        {
            string inputFileName = @"C:\Users\zhiq\Desktop\new 3.txt";
            string formarFileName = "D:\\zhijie\\return.txt";
            generateRoute(inputFileName,formarFileName);
            return;
            test2(inputFileName,formarFileName);
            //return;
            StreamReader sr = new StreamReader(formarFileName);
            StreamWriter sw = new StreamWriter(@"D:\zhijie\regResult\regResult3.txt",true);
            string line = null;
            string[] splits = new string[] { "and","And"};
            string[] splits2 = new string[] { "or", "Or" ,"OR"};
            int lineIndex = 0;
            while((line = sr.ReadLine()) != null)
            {
                lineIndex++;
                if (lineIndex == 1) continue;
                string[] tokens = line.Split("\t".ToArray(),StringSplitOptions.RemoveEmptyEntries);
                if (tokens.Length < 4) continue;
                sw.WriteLine("BEGIN_NEW_RULE");
                sw.WriteLine("9842");
                sw.WriteLine(tokens[2]);
                sw.WriteLine(tokens[3]);
                string expre = tokens[1].Trim();

                string[] innerTokens = expre.Split(splits,StringSplitOptions.RemoveEmptyEntries);
                List<HashSet<string>> ls = new List<HashSet<string>>();
                foreach (string token in innerTokens)
                {
                    string[] keywords = token.Split(splits2,StringSplitOptions.RemoveEmptyEntries);
                    HashSet<string> kset = new HashSet<string>();
                    foreach(string key in keywords)
                    {
                        if (kset.Contains(key.Trim().Trim("\"".ToArray()).Trim()) == false) kset.Add(key.Trim().Trim("\"".ToArray()).Trim());
                    }
                    ls.Add(kset);
                }
                List<string> regs = new List<string>();
                //string ret = RegGenerate2(ls);
                RegGenerate3(ls,0,"",regs);
                foreach(string s in regs)
                {
                    sw.WriteLine("9842\t"+tokens[2]+"\t"+s+"");
                }
                //sw.WriteLine("9842\t" + tokens[2] + "\t" + ret + "");
            }
            sr.Close();
            sw.Close();
        }

        class Answer
        {
            public int Id { get; set; }
           
            public int Score { set; get; }
            public int Test { set; get; }
            public string Content { get; set; }
        }

        
        class MyObj
        {
            public List<Answer> Answers
            {
                get; set;
            }
        }

        private static void GetAllProblems()
        {
            StreamReader sr = new StreamReader(@"D:\zhijie\regResult\regResult.txt");
            StreamWriter sw = new StreamWriter(@"D:\zhijie\regResult\testProblems.txt");
            string line = null;
            HashSet<string> problems = new HashSet<string>();
            while ((line = sr.ReadLine()) != null)
            {
                string[] tokens = line.Split("\t".ToArray(), StringSplitOptions.RemoveEmptyEntries);
                if (tokens.Length < 3) continue;
                if (problems.Contains(tokens[1]) == false)
                {
                    problems.Add(tokens[1]);
                    sw.WriteLine(tokens[1]);
                }
            }
            sr.Close();
            sw.Close();
        }
        private static Dictionary<string,WordItem> strToInverseItems = new DefaultDictionary<string, WordItem>();
        private static Dictionary<int,Rule> ruleidToRule = new DefaultDictionary<int, Rule>();
        class Rule
        {
            public int PatternCnt { get; set; }
            public List<List<int>> SubItemCnts { get; set; }
            public int Id { get; set; }
            public string Pattern { get; set; }
            public Rule()
            {
                SubItemCnts = new List<List<int>>();
            }

            public string Tag()
            {
                string ret = "" + Id;
                ret += "\t" + PatternCnt + "\t" + Pattern+"\t";
                foreach (var list in SubItemCnts)
                {
                    string tmp = "";
                    if (list.Any())
                        tmp = ""+list[0];
                    for (int i = 1; i < list.Count(); i++)
                        tmp += "&" + list[i];
                    tmp += "$";
                    ret += tmp;
                }
                if (ret.EndsWith("$")) ret = ret.TrimEnd("$".ToArray());
                return ret;
            }

            public Rule(string tag)
            {
                string[] tokens = tag.Split("\t".ToArray(), StringSplitOptions.RemoveEmptyEntries);
                Id = int.Parse(tokens[0]);
                PatternCnt = int.Parse(tokens[1]);
                Pattern = tokens[2];
                tokens = tokens[3].Split("$".ToArray(), StringSplitOptions.RemoveEmptyEntries);
                SubItemCnts = new List<List<int>>();
                foreach (string token in tokens)
                {
                    string[] cntStrings = token.Split("&".ToArray(), StringSplitOptions.RemoveEmptyEntries);
                    SubItemCnts.Add(new List<int>());
                    foreach (string s in cntStrings)
                        SubItemCnts.Last().Add(int.Parse(s));
                }
            }
            public void printMySelf()
            {
                Console.WriteLine("patterncnt:{0} Pattern:{1}",PatternCnt,Pattern);
                foreach (var list in SubItemCnts)
                {
                    foreach (var cnt in list)
                    {
                        Console.Write(cnt+" ");
                    }
                    Console.WriteLine();
                }
            }
        }

        class WordItem
        {
            public string Word { get; set; }
            public IEnumerable<InverseItem> InverseItems { get; set; }

            public string Tag()
            {
                string ret = Word;
                foreach (InverseItem ii in InverseItems)
                {
                    ret += "\t" + ii.Tag();
                }
                return ret;
            }

            public WordItem()
            {
                InverseItems = new List<InverseItem>();
            }
            public WordItem(string tag)
            {
                string[] tokens = tag.Split("\t".ToArray(), StringSplitOptions.RemoveEmptyEntries);
                Word = tokens[0];
                var tmp = new List<InverseItem>();
                for (int i = 1; i < tokens.Length; i++)
                {
                    tmp.Add(new InverseItem(tokens[i],Word));
                }
                InverseItems = tmp;
            }
        }

        class InverseItem
        {
            public string Word { get; set; }
            public int RuleId { get; set; }
            
            public int PatternId { get; set; }
            public int SubPatternId { get; set; }
            public int SubPatternIndex { get; set; }

            public string SubPatternTag()
            {
                return "" + RuleId + "|" + PatternId + "|" + SubPatternId;
            }

            public string Tag()
            {
                return RuleId + "|" + PatternId + "|" + SubPatternId+"|"+SubPatternIndex;
            }

            public InverseItem()
            {
                
            }
            public InverseItem(string tag,string word)
            {
                string[] tokens = tag.Split("|".ToArray(), StringSplitOptions.RemoveEmptyEntries);
                Word = word;
                RuleId = int.Parse(tokens[0]);
                PatternId = int.Parse(tokens[1]);
                SubPatternId = int.Parse(tokens[2]);
                SubPatternIndex = int.Parse(tokens[3]);
            }
            
        }

        static int NJie(int n)
        {
            int ret = 1;
            for (int i = 2; i <= n; i++) ret *= i;
            return ret;
        }
        static Dictionary<string, HashSet<string>> wordToSynonyms = new Dictionary<string, HashSet<string>>();

        public static void loadSynonyms(string fileName)
        {
            StreamReader sr= new StreamReader(fileName);
            string line = null;
            while ((line = sr.ReadLine()) != null)
            {
                string[] tokens = line.Split(' ');
                HashSet<string> strs = new HashSet<string>();
                foreach (string token in tokens)
                {
                    if (strs.Contains(token) == false) strs.Add(token);
                }
                foreach (string token in tokens) wordToSynonyms[token] = strs;
            }
            sr.Close();
        }
        //(高血脂|高胆固醇|血脂高|高脂血症|胆固醇偏高|降低血脂|胆固醇超了)[\s\S]*生活方式
        public class QAlgoBasedProvider
        {
            static Dictionary<int,Rule>  ruleIdToRule = new Dictionary<int, Rule>();
            static Dictionary<string,WordItem> wordToItems = new Dictionary<string, WordItem>();
            static List<InverseItem> _allInverseItems = new List<InverseItem>();

            
            private static string baseDir = "D:\\zhijie\\QInverse\\";
            public static void SaveModel()
            {
                string ruleFile = baseDir + "rules.pickle";
                string inverseFile = baseDir + "inverse.pickle";
                StreamWriter sw = new StreamWriter(ruleFile);
                StreamWriter sw2 = new StreamWriter(inverseFile);
                
                foreach (var kv in ruleIdToRule)
                {
                    sw.WriteLine(kv.Value.Tag());
                }
                foreach (var kv in wordToItems)
                {
                    sw2.WriteLine(kv.Value.Tag());
                }
                sw.Close();
                sw2.Close();
                Console.Write("SaveModel finished\n");
            }

            public static void LoadModel()
            {
                string ruleFile = baseDir + "rules.pickle";
                string inverseFile = baseDir + "inverse.pickle";
                StreamReader sr = new StreamReader(ruleFile);
                StreamReader sr2 = new StreamReader(inverseFile);
                string line = null;
                ruleIdToRule.Clear();
                wordToItems.Clear();
                while ((line = sr.ReadLine()) != null)
                {
                    Rule rule = new Rule(line);
                    ruleIdToRule[rule.Id] = rule;
                }
                while ((line = sr2.ReadLine()) != null)
                {
                    WordItem item = new WordItem(line);
                    wordToItems[item.Word] = item;
                }
                sr.Close();
                sr2.Close();
                Console.Write("LoadModel finished\n");
            }
            private static void AllTokens(string token, JiebaSegmenter seg,Rule rule)
            {
                string[] _words = token.Split("|".ToArray(), StringSplitOptions.RemoveEmptyEntries);
                HashSet<string> hwords = new HashSet<string>();
                foreach (string w in _words)
                {
                    hwords.Add(w);
                    if (wordToSynonyms.ContainsKey(w))
                    {
                        hwords.UnionWith(wordToSynonyms[w]);
                    }
                }
                List<string> words = hwords.ToList();
                rule.SubItemCnts.Add(new List<int>());
                int subPatternId = 0;
                foreach (string _word in words)
                {
                    var innerWords = seg.Cut(_word,true);
                    rule.SubItemCnts.Last().Add(innerWords.Count());//add subpattern cnt
                    int subPatternIndex = 0;
                    foreach (var word in innerWords)
                    {
                        InverseItem iitem = new InverseItem() {
                            Word =word,
                            PatternId = rule.SubItemCnts.Count()-1,
                            RuleId = rule.Id,
                            SubPatternId = subPatternId,
                            SubPatternIndex = subPatternIndex++
                        };
                        _allInverseItems.Add(iitem);
                    }
                    subPatternId++;
                }
            }
            private static List<Rule> GenerateRulesFromFile(string fileName)
            {
                var segmenter = new JiebaSegmenter();
                segmenter.LoadUserDict(QAlgoBasedProvider.baseDir + "user_dict.txt");
                //StreamWriter sw = new StreamWriter(baseDir + "realPattern");
                int ruleId = 0;
                List<Rule> rules = new List<Rule>();
                
                string[] seps = new string[] { "[\\s\\S]*" };
                using (StreamReader sr = new StreamReader(fileName))
                {
                    string line = null;
                    while ((line = sr.ReadLine()) != null)
                    {
                        string[] tokens = line.Split(seps, StringSplitOptions.RemoveEmptyEntries);
                        //for (int i = 0; i < NJie(tokens.Length) - 1; i++) sr.ReadLine();
                        Rule rule = new Rule() {Id = ruleId,PatternCnt = tokens.Length,Pattern = line};
                        if (line.CompareTo("立普妥[\\s\\S]*(失眠|心脏疼痛)") == 0)
                        {
                            Console.WriteLine();
                        }
                        //sw.WriteLine(line);
                        rules.Add(rule);
                        ruleIdToRule[rule.Id] = rule;
                        string token = null;
                        foreach (string _token in tokens)
                        {
                            token = _token;
                            if (token.StartsWith("(")) token = token.Substring(1, token.Length - 2);
                            AllTokens(token,segmenter,rule);
                        }
                        //rule.printMySelf();
                        ruleId++;
                    }
                    WordItems(_allInverseItems);
                }
                _allInverseItems.Clear();
                //sw.Close();
                return rules;
            }
            private static List<Rule> Rules()
            {
                List<Rule> allRules = new List<Rule>();
                allRules.Add(new Rule() {PatternCnt = 3, Id = 1});
                ruleidToRule[allRules.Last().Id] = allRules.Last();
                allRules.Add(new Rule() {PatternCnt = 2, Id = 2});
                ruleidToRule[allRules.Last().Id] = allRules.Last();
                allRules.Add(new Rule() {PatternCnt = 3, Id = 3});
                ruleidToRule[allRules.Last().Id] = allRules.Last();
                allRules.Add(new Rule() {PatternCnt = 4, Id = 4});
                ruleidToRule[allRules.Last().Id] = allRules.Last();
                return allRules;
            }

            private static List<WordItem> WordItems()
            {
                List<WordItem> wordItems = new List<WordItem>();
                List<InverseItem> inverseItems = new List<InverseItem>();
                var groupItems = inverseItems.GroupBy(x => x.Word);
                foreach (var group in groupItems)
                {

                    wordItems.Add(new WordItem() {Word = group.Key, InverseItems = group});
                    strToInverseItems[group.Key] = wordItems.Last();
                }
                return wordItems;
            }

            private static List<WordItem> WordItems(List<InverseItem> inverseItems)
            {
                List<WordItem> wordItems = new List<WordItem>();

                var groupItems = inverseItems.GroupBy(x => x.Word);
                foreach (var group in groupItems)
                {

                    wordItems.Add(new WordItem() {Word = group.Key, InverseItems = group});
                    QAlgoBasedProvider.wordToItems[group.Key] = wordItems.Last();
                }
                return wordItems;
            }

            private static List<InverseItem> InverseItems()
            {
                List<InverseItem> inverseItems = new List<InverseItem>();
                inverseItems.Add(new InverseItem() {Word = "高血脂", RuleId = 1, PatternId = 1});
                inverseItems.Add(new InverseItem() {Word = "高血脂", RuleId = 2, PatternId = 1});
                inverseItems.Add(new InverseItem() {Word = "立普妥", RuleId = 1, PatternId = 2});
                inverseItems.Add(new InverseItem() {Word = "立普妥", RuleId = 3, PatternId = 1});
                inverseItems.Add(new InverseItem() {Word = "服用", RuleId = 1, PatternId = 3});
                return inverseItems;
            }

            private static List<Rule> AllMatchRules(string question)
            {
                List<Rule> rules = new List<Rule>();
                var segmenter = new JiebaSegmenter();
                segmenter.LoadUserDict(QAlgoBasedProvider.baseDir+"user_dict.txt");
                var tokens = segmenter.Cut(question);
                List<InverseItem> inverseItems= new List<InverseItem>();

                foreach (var token in tokens)
                {
                    if (strToInverseItems.ContainsKey(token) == false) continue;
                    WordItem witem = strToInverseItems[token];
                    inverseItems.AddRange(witem.InverseItems);

                }
                var groupItems = inverseItems.GroupBy(x => x.RuleId);
                Dictionary<int,HashSet<int>> ruleIdToCnt = new Dictionary<int, HashSet<int>>();
                foreach (var group in groupItems)
                {
                    ruleIdToCnt[group.Key] = new HashSet<int>();
                    foreach (var t in group)
                    {
                        if (ruleIdToCnt[group.Key].Contains(t.PatternId) == false)
                            ruleIdToCnt[group.Key].Add(t.PatternId);
                    }
                    if (ruleidToRule.ContainsKey(group.Key) &&
                        ruleIdToCnt[group.Key].Count() == ruleidToRule[group.Key].PatternCnt)
                    {
                        Console.WriteLine("rule id {0} is matched",group.Key);
                        rules.Add(ruleidToRule[group.Key]);
                    }
                }
                return rules;
            }
            private static List<Rule> AllMatchRules2(string question,bool needLower = true)
            {
                if(needLower==true)
                    question = question.ToLower();
                List<Rule> rules = new List<Rule>();
                var segmenter = new JiebaSegmenter();
                var tokens = segmenter.Cut(question,true);
                
                List<InverseItem> inverseItems = new List<InverseItem>();
                StreamWriter sw = new StreamWriter("D:\\zhijie\\tmpResult.txt");
                foreach (var token in tokens)
                {
                    if (QAlgoBasedProvider.wordToItems.ContainsKey(token) == false) continue;
                    WordItem witem = QAlgoBasedProvider.wordToItems[token];
                    inverseItems.AddRange(witem.InverseItems);
                    if (wordToSynonyms.ContainsKey(token))
                    {
                        foreach (string word in wordToSynonyms[token])
                        {
                            if (word != token&&QAlgoBasedProvider.wordToItems.ContainsKey(word))
                            {
                                inverseItems.AddRange(QAlgoBasedProvider.wordToItems[word].InverseItems);
                            }
                        }
                    }
                }
                var groupItems = inverseItems.GroupBy(x => x.SubPatternTag());
                Dictionary<int,HashSet<int>> ruleToPatternId = new Dictionary<int, HashSet<int>>();
                foreach (var group in groupItems)
                {
                    string tag = group.Key;
                    string[] _tokens = tag.Split("|".ToArray());
                    int ruleId = int.Parse(_tokens[0]), patternId = int.Parse(_tokens[1]),subpatternId = int.Parse(_tokens[2]);
                    if (ruleId == 35)
                    {
                        Console.Write(ruleId+"\n");
                    }
                    if (QAlgoBasedProvider.ruleIdToRule.ContainsKey(ruleId) == false) continue;
                    int needCnt = QAlgoBasedProvider.ruleIdToRule[ruleId].SubItemCnts[patternId][subpatternId];
                    HashSet<int> allIndexes = new HashSet<int>();
                    foreach (var item in group)
                    {
                        if (allIndexes.Contains(item.SubPatternIndex) == false) allIndexes.Add(item.SubPatternIndex);
                    }
                    if (allIndexes.Count() == needCnt)
                    {
                        if(ruleToPatternId.ContainsKey(ruleId)==false)
                            ruleToPatternId[ruleId] = new HashSet<int>();
                        if (ruleToPatternId[ruleId].Contains(patternId) == false)
                            ruleToPatternId[ruleId].Add(patternId);
                    }
                }
                foreach (var kv in ruleToPatternId)
                {
                    if (QAlgoBasedProvider.ruleIdToRule.ContainsKey(kv.Key) == false) continue;
                    if (kv.Value.Count() == QAlgoBasedProvider.ruleIdToRule[kv.Key].PatternCnt)
                    {
                        //System.Console.WriteLine("rule id {0} is matched,the pattern is \n{1}",kv.Key, ruleIdToRule[kv.Key].Pattern);
                        sw.WriteLine("rule id {0} is matched,the pattern is \n{1}", kv.Key, ruleIdToRule[kv.Key].Pattern);
                        sw.Flush();
                        rules.Add(QAlgoBasedProvider.ruleIdToRule[kv.Key]);
                    }
                   //sw.WriteLine("________________________________________________________");
                } 
                sw.Close();
                return rules;
            }
            public static void Main()
            {
                string TItle = "JJJJ_title";
                string atitle = $"hello,jack,{TItle} is not ok";
                Console.WriteLine(atitle);
                Console.ReadLine();
                {
                    loadSynonyms("D:\\zhijie\\regResult\\finalSynonyms2.txt");
                    Console.OutputEncoding = Encoding.UTF8;
                    Console.InputEncoding = Encoding.UTF8;
                    QAlgoBasedProvider.GenerateRulesFromFile(QAlgoBasedProvider.baseDir+"realPattern");
                    Console.WriteLine("generate finished");
                    QAlgoBasedProvider.SaveModel();
                    AllMatchRules2("购买到立普妥有的有条形码/商品码，有的没有，是否都是真的", false);
                    string question = null;
                    StreamReader sr = new StreamReader("D:\\zhijie\\regResult\\testProblems2.txt");
                    StreamWriter swMatch = new StreamWriter("D:\\zhijie\\regResult\\ruleMatch2.txt");
                    StreamWriter swNoMatch = new StreamWriter("D:\\zhijie\\regResult\\ruleNoMatch2.txt");
                    DateTime st1 = DateTime.Now;
                    int rightCnt = 0;
                    
                    while ((question = sr.ReadLine()) != null)
                    {
                        List<Rule> rules = AllMatchRules2(question,true);
                        if (rules.Count() == 0)
                        {
                            rules = AllMatchRules2(question,false);
                        }
                        if (rules.Count() == 0)
                        {
                            swNoMatch.WriteLine(question);
                        }
                        else
                        {
                            rightCnt++;
                            swMatch.WriteLine(question);
                            foreach (Rule rule in rules)
                            {
                                swMatch.WriteLine(rule.Pattern);
                            }
                            swMatch.Write("-----------------------------------------------\n");
                        }
                        
                        //Console.Write("Please input:");
                    }
                    Console.WriteLine(rightCnt);
                    swMatch.Close();
                    swNoMatch.Close();
                    DateTime dt2 = DateTime.Now;
                    TimeSpan ts = dt2 - st1;
                    Console.WriteLine(ts.TotalMilliseconds);
                    return;
                }
                {
                    var segmenter = new JiebaSegmenter();
                    var segments = segmenter.Cut("我来到北京清华大学", cutAll: true);
                    Console.WriteLine("【全模式】：{0}", string.Join("/ ", segments));
                    return;
                }
                {
                    Rules();
                    var vis = InverseItems();
                    WordItems(vis);
                    var matchedRules = AllMatchRules("服用立普妥，高血脂复查正常，还需要继续服药吗？");
                }
                return;
                GetAllProblems();
                return;
                {
                    string regStr2 = @"(立普妥|进口|地产|国产|紫色|绿色)[\s\S]*(哪个好|区别|一样吗|不同)";

                    if (Regex.IsMatch("fuck地产ch超级fuck@@@@@*……*&******.。一样吗", regStr2))
                    {
                        Console.WriteLine("true");
                    }
                    else Console.WriteLine("false");
                    return;
                }
                {
                    string str = "hello";
                    string nullStr = null;
                    string emptyStr = String.Empty;

                    string tempStr = str + nullStr;
                    // Output of the following line: hello
                    Console.WriteLine(tempStr);

                    bool b = (emptyStr == nullStr);
                    // Output of the following line: False
                    Console.WriteLine(b);

                    // The following line creates a new empty string.
                    string newStr = emptyStr + nullStr;

                    // Null strings and empty strings behave differently. The following
                    // two lines display 0.
                    Console.WriteLine(emptyStr.Length);
                    Console.WriteLine(newStr.Length);
                    // The following line raises a NullReferenceException.
                    //Console.WriteLine(nullStr.Length);

                    // The null character can be displayed and counted, like other chars.
                    string s1 = "\x0" + "abc";
                    string s2 = "abc" + "\x0";
                    // Output of the following line: * abc*
                    Console.WriteLine("*" + s1 + "*");
                    // Output of the following line: *abc *
                    Console.WriteLine("*" + s2 + "*");
                    // Output of the following line: 4
                    Console.WriteLine(s2.Length);
                    int? x = null;
                    int y = x ?? -1;
                    Console.WriteLine(y);
                    string schemaJson = @"{'description':'a person'},'properties':{'name':'jack','height':}";
                }
                {
                    Dictionary<string, int> points = new Dictionary<string, int>
                    {
                        {"James", 9001},
                        {"Jo", 3474},
                        {"Jess", 11926}
                    };

                    string json = JsonConvert.SerializeObject(points);

                    Console.WriteLine(json);
                    return;
                }
                long? lv = null;
                if (lv == null)
                {
                    Console.WriteLine("fuck");
                }
                {
                    string s = "{\"Answers\": [{\"Id\": 0,\"Content\": \"string\",\"Score\": 0}]}";
                    MyObj obj = JsonConvert.DeserializeObject<MyObj>(s);
                    Console.WriteLine(obj.Answers.Count);
                    foreach (var answer in obj.Answers)
                    {
                        Console.WriteLine("{0} {1} {2}", answer.Id, answer.Content, answer.Score);
                    }
                    string js = JsonConvert.SerializeObject(obj);
                    Console.WriteLine(js);
                    Console.WriteLine();
                    Console.ReadLine();

                }
                List<int> lints = new List<int>() {1, 2, 3};
                var t = lints.Where(x => x > 2).AsQueryable();
                lints.Add(19);
                foreach (var ti in t)
                {
                    Console.WriteLine(ti);
                }
                transform();
                return;
                string line = "lane=1;speed=30.3mph;acceleration=2.5mph/s";

                Regex reg = new Regex(@"speed\s*=\s*([\d\.]+)\s*(mph|km/h|m/s)*");

                Match match = reg.Match(line);

                Console.WriteLine();
                string regStr = @"(立普妥|进口|地产|国产|紫色|绿色)[\s\S]*(哪个好|区别|一样吗|不同)";

                if (Regex.IsMatch("fuck地产ch超级fuck@@@@@*……*&******.。一样吗", regStr))
                {
                    Console.WriteLine("true");
                }
                else Console.WriteLine("false");
                return;
                test();
                return;
                KeyWordTransform kwt = new KeyWordTransform();
                DataTable dataTable = kwt.ExcelToDataTable("1", @"D:\zhijie\1.xlsx");
                foreach (Row row in dataTable.Rows)
                {
                    System.Console.WriteLine(row.ToString());
                }
                Console.WriteLine("fuck");
            }
        }
    }
}
