using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LinqLearn
{
    class Program
    {
        public class RuleLeaf {
            public int RuleId{get;set;}
            public string Keywords { get; set; }

        }
        public class RuleResult {
            public int RuleId { get; set; }
            public string Answer { get; set; }
        }
        public class RuleNode {
            public int Id { get; set; }
            public int MerchantID { get; set; }
            public string Source { get; set; }
            public bool ReplyAll { get; set; }
            public string Description { get; set; }
            public ICollection<RuleLeaf> AllLeafs{set;get;}
            public ICollection<RuleResult> AllResult { get; set; }
        }
        public static ICollection<RuleNode> createRuleNodes() {
            ICollection<RuleNode> ruleNodes = new List<RuleNode>();
            ruleNodes.Add(new RuleNode{Id=1,MerchantID=1,Source="",ReplyAll=true,Description="" });
            ruleNodes.Add(new RuleNode { Id = 2, MerchantID = 1, Source = "", ReplyAll = true, Description = "" });
            ruleNodes.Add(new RuleNode { Id = 3, MerchantID = 1, Source = "", ReplyAll = true, Description = "" });
            return ruleNodes;
        }
        public static ICollection<RuleLeaf> createRuleLeafs()
        {
            ICollection<RuleLeaf> ruleLeafs = new List<RuleLeaf>();
            ruleLeafs.Add(new RuleLeaf { RuleId=1,Keywords= "高血脂 降脂药 降脂药" });
            ruleLeafs.Add(new RuleLeaf { RuleId = 1, Keywords = "高胆固醇 降脂药 降脂药" });
            ruleLeafs.Add(new RuleLeaf { RuleId = 2, Keywords = "冠心病 立普妥 继续服用" });
            ruleLeafs.Add(new RuleLeaf { RuleId = 2, Keywords = "斑块 立普妥 继续服用" });
            return ruleLeafs;
        }
        public static ICollection<RuleResult> createRuleResult()
        {
            ICollection<RuleResult> ruleResult = new List<RuleResult>();
            ruleResult.Add(new RuleResult { RuleId=1,Answer= "亲，一旦随意减量或停药，血脂水平可能会反弹，心脑血管事件风险再次升高。只要没有特殊情况，请坚持长期他汀治疗." });
            ruleResult.Add(new RuleResult { RuleId = 2, Answer = "胆固醇沉积形成的斑块如同“不定时炸弹”，随时可能破裂，导致脑梗心梗等。只有坚持长期他汀治疗，才能防防止斑块破裂，减少心脑血管突发事件。" });
            return ruleResult;
        }
        public class MyBaseClass {
             public void Print()
            {
                Console.WriteLine("This is MyBaseClass");
            }
        }
        public class MyDerivedClass : MyBaseClass {
            public  void Print()
            {
                Console.WriteLine("This is MyDerivedClass class");
            }
        }
        public class SecondDerived : MyBaseClass
        {
            public  void Print()
            {
                Console.WriteLine("This is second override class");
            }
        }

        public class Student
        {
            public string First { get; set; }
            public string Last { get; set; }
            public int ID { get; set; }
            public List<int> Scores;
        }

        public class Order {
            public string First { get; set; }
            public string Last { get; set; }
            public int ID { get; set; }
            public int MoneyCount { get; set; }
        }


        public static List<Student> GetStudents()
        {
            // Use a collection initializer to create the data source. Note that each element
            //  in the list contains an inner sequence of scores.
            List<Student> students = new List<Student>
            {
               new Student {First="Svetlana", Last="Omelchenko", ID=111, Scores= new List<int> {97, 72, 81, 60}},
               new Student {First="Claire", Last="O'Donnell", ID=112, Scores= new List<int> {75, 84, 91, 39}},
               new Student {First="Sven", Last="Mortensen", ID=113, Scores= new List<int> {99, 89, 91, 95}},
               new Student {First="Cesar", Last="Garcia", ID=114, Scores= new List<int> {72, 81, 65, 84}},
               new Student {First="Debra", Last="Garcia", ID=115, Scores= new List<int> {97, 89, 85, 82}}
            };

                return students;

        }

        public static List<Order> GetOrders()
        {
            List<Order> orders = new List<Order> {
                new Order { ID=111,MoneyCount=333},
                new Order { ID=112,MoneyCount=222},
            };

            return orders;
        }
        public static string SearchResult(ICollection<RuleNode> ruleNodes, ICollection<RuleLeaf> ruleLeafs, ICollection<RuleResult> ruleResults,string searchStr)
        {
            
            List<string> allTokens = searchStr.Split(" ".ToArray(),StringSplitOptions.RemoveEmptyEntries).ToList();

            HashSet<string> tokensSet = new HashSet<string>();
            foreach(string token in allTokens)
            {
                if (tokensSet.Contains(token.ToLower()) == false)
                    tokensSet.Add(token);
            }
            Dictionary<int, List<RuleLeaf>> groupRuleLeafs = new Dictionary<int, List<RuleLeaf>>();
            Dictionary<int, List<RuleResult>> groupRuleResult = new Dictionary<int, List<RuleResult>>();
            foreach(RuleLeaf rl in ruleLeafs)
            {
                if (groupRuleLeafs.ContainsKey(rl.RuleId) == false)
                {
                    groupRuleLeafs[rl.RuleId] = new List<RuleLeaf>();
                    groupRuleLeafs[rl.RuleId].Add(rl);
                }else
                    groupRuleLeafs[rl.RuleId].Add(rl);
            }
            foreach(RuleNode rn in ruleNodes)
            {
                if (groupRuleLeafs.ContainsKey(rn.Id))
                {
                    rn.AllLeafs = groupRuleLeafs[rn.Id];
                }
            }
            foreach(RuleResult rr in ruleResults)
            {
                if (groupRuleResult.ContainsKey(rr.RuleId) == false)
                {
                    groupRuleResult[rr.RuleId] = new List<RuleResult>();
                }
                groupRuleResult[rr.RuleId].Add(rr);
            }
            foreach (RuleNode rn in ruleNodes)
            {
                if (rn.AllLeafs == null) continue;
                bool flag = true;
                foreach(RuleLeaf rl in rn.AllLeafs)
                {
                    string[] keys = rl.Keywords.Split(" ".ToArray(), StringSplitOptions.RemoveEmptyEntries);
                    bool innerFlag = false;
                    foreach(string key in keys)
                    {
                        if (tokensSet.Contains(key))
                        {
                            innerFlag = true;
                            break;
                        }
                    }
                    if (!innerFlag)
                    {
                        flag = false;
                        break;
                    }
                }
                if (flag)
                {
                    if (groupRuleResult.ContainsKey(rn.Id))
                    {
                        foreach(RuleResult rr in groupRuleResult[rn.Id])
                        {
                            Console.WriteLine(rr.Answer);
                        }
                    }
                }
            }
            return null;
        }
        static void Main2()
        {
            //test any
            {
                List<string> strs = new List<string> { "1","2","3"};
                HashSet<string> hSets = new HashSet<string>();
                foreach(string s in strs)
                {
                    hSets.Add(s);
                }
                List<string> strsRight = new List<string> { "5", "2", "5" };
                var t = strsRight.Any(a=>hSets.Contains(a));
                Console.WriteLine(t);
                Console.ReadLine();
            }
            /*MyBaseClass mdc = new MyDerivedClass();
            mdc.Print();
            Console.ReadKey();*/
            // Obtain the data source.
            /*
             * List<Student> students = new List<Student>
            {
               new Student {First="Svetlana", Last="Omelchenko", ID=111, Scores= new List<int> {97, 72, 81, 60}},
               new Student {First="Claire", Last="O'Donnell", ID=112, Scores= new List<int> {75, 84, 91, 39}},
               new Student {First="Sven", Last="Mortensen", ID=113, Scores= new List<int> {99, 89, 91, 95}},
               new Student {First="Cesar", Last="Garcia", ID=114, Scores= new List<int> {72, 81, 65, 84}},
               new Student {First="Debra", Last="Garcia", ID=115, Scores= new List<int> {97, 89, 85, 82}}
            };
             */
            //Lookup<string, int> lk = new Lookup<string, int>();
            ICollection<RuleNode> ruleNodes = createRuleNodes();
            ICollection<RuleLeaf> ruleLeafs = createRuleLeafs();
            ICollection<RuleResult> ruleResults = createRuleResult();
            SearchResult(ruleNodes, ruleLeafs, ruleResults, "高血脂 降脂药 降脂药");
            return;
            List<Student> students = GetStudents();
            var aggres = students.Aggregate((l, r) => { if (l.ID > 100) return l; return l.ID > r.ID ? l : r; });
            Console.WriteLine(aggres.First);
            var ss = students.Max(x=>x.ID);
            Console.WriteLine(ss);
            /*foreach(var s in ss)
            {
                Console.WriteLine(s);
            }*/
            return;
            List<Order> orders = GetOrders();
            var stuOrder = from stu in students
                           join order in orders on stu.ID equals order.ID
                           select new { ID = stu.ID, Name = stu.First + stu.Last, Money = order.MoneyCount };
            foreach(var so in stuOrder)
            {
                Console.WriteLine(so.ID + ":" + so.Name + ":" + so.Money);
            }
            var mylist = from stu in students
                         group stu by stu.First[0] into g
                         orderby g.Key
                         select g;
            foreach (var group in mylist)
            {
                Console.WriteLine(group.Key+"----------------------------------------");
                foreach(Student stu in group)
                {
                    Console.WriteLine(stu.First);
                }
            }
            Console.ReadLine();
            // Group by true or false.
            // Query variable is an IEnumerable<IGrouping<bool, Student>>
            var booleanGroupQuery =
                from student in students
                group student by student.Scores.Average() >= 80; //pass or fail!

            // Execute the query and access items in each group
            foreach (var studentGroup in booleanGroupQuery)
            {
                Console.WriteLine(studentGroup.Key == true ? "High averages" : "Low averages");
                foreach (var student in studentGroup)
                {
                    Console.WriteLine("   {0}, {1}:{2}", student.Last, student.First, student.Scores.Average());
                }
            }

            // Keep the console window open in debug mode.
            Console.WriteLine("Press any key to exit.");
            Console.ReadKey();
        }
    }
}
