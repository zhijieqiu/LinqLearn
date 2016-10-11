using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

namespace LinqLearn
{
    public delegate int MyDelegate(int a, int b);
    
    class MyDownloadString
    {
        public static int MyAdd(int x, int y)
        {
            Console.WriteLine("{0}+{1}={2}",x,y,x+y);
            return x + y;
        }
        Stopwatch sw = new Stopwatch();
        public void DoRun()
        {
            sw.Start();
            Task<int> t1 = CountCharactersAsync(1,"http://www.microsoft.com");
            Task<int> t2 = CountCharactersAsync2(2,"http://www.illustratedcsharp.com");
            Thread.Sleep(1000);
            Task<int>[] tasks = new Task<int>[] { t1,t2};
            Task.WaitAny(tasks);
            Console.WriteLine("Task 1 : {0} finished", t1.IsCompleted ? "": "not");
            Console.WriteLine("Task 2 : {0} finished", t2.IsCompleted ? "": "not");
            Console.Read();
        }
        private async Task<int> CountCharactersAsync(int id, string site)
        {
            WebClient wc = new WebClient();
            string result = await wc.DownloadStringTaskAsync(new Uri(site));
            await Task.Delay(1000);
            Console.WriteLine(" Call {0} completed: {1} ms ,resultLength:{2}", id,sw.Elapsed.TotalMilliseconds,result.Length);
            return result.Length;
        }
        private async Task<int> CountCharactersAsync2(int id, string site)
        {
            WebClient wc = new WebClient();
            Task<String> result = wc.DownloadStringTaskAsync(new Uri(site));
            await result;
            Console.WriteLine(" Call {0} completed: {1} ms, resultLength:{2}", id, sw.Elapsed.TotalMilliseconds,result.Result.Length);
           
            return result.Result.Length;
        }
        private int TestAsync()
        {
           
            return 0;
        }

        class Program {
            private async static Task<int> CountCharactersAsync3(int id, string site)
            {
                WebClient wc = new WebClient();
                Task<String> result = wc.DownloadStringTaskAsync(new Uri(site));
                await result;
                //Console.WriteLine(" Call {0} completed: {1} ms, resultLength:{2}", id, sw.Elapsed.TotalMilliseconds, result.Result.Length);
                Console.WriteLine("countcharactersasync3");
                return result.Result.Length;
            }
            private async static Task<int> Mytest()
            {
                Task<int> tt = CountCharactersAsync3(3, "http://www.microsoft.com");
                await tt;
                return tt.Result;
            }
            static void Main2()
            {
                var tt = Mytest();
                tt.Wait();
                Console.WriteLine("finished");
                Thread.Sleep(1000);
                MyDelegate mydelegate = new MyDelegate((x, y) => x + y);
                mydelegate += MyAdd;
                mydelegate(2,3);
                List<int> myList = new List<int>() { 1,2,3};
                List<int> myList2 = new List<int>() { 4,5,6};
                List<List<int>> llist = new List<List<int>>() { myList,myList2};
                List<int> myList3 = new List<int>() { 1, 2, 3 };
                List<int> myList4 = new List<int>() { 4, 5, 6 };
                List<List<int>> llist2 = new List<List<int>>() { myList3, myList4 };
                List<List<List<int>>> lllist = new List<List<List<int>>>() { llist, llist2 };
                var t = lllist.SelectMany(x=>x);
                foreach(var m in t)
                {
                    Console.WriteLine(m);
                }
                MyDownloadString ds = new MyDownloadString();
                ds.DoRun();
            }
        }

    }
}
