using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using static System.Threading.Tasks.Parallel;

namespace parallel_test
{
    class Program
    {
        private class TestResult
        {
            public bool Assert = true;
            public long ElapsedTime = 0;
            public Func<int, IEnumerable<int>> Execute;
            public IEnumerable<int> Result = null;
            public decimal Qty = 0;
            private static string _header = $"| {"Name".PadRight(40)} | {"Average (msg/ms)".PadRight(20)} | {"Elapsed (ms)".PadRight(20)} | {"Count (qty)".PadRight(20)} | {"Expected (qty)".PadRight(20)} | {"Assert".PadRight(20)} |";

            public decimal Average => Math.Round(Qty/ElapsedTime, 4);

            public static void MakeHeader()
            {
                            
                Console.WriteLine("".PadRight(_header.Length, '-'));
                Console.WriteLine(_header);
                Console.WriteLine("".PadRight(_header.Length, '-'));                
                
            }
            
            public static void MakeFooter()
            {
                Console.WriteLine("".PadRight(_header.Length, '-'));   
            }

            public void WriteLine()
            {
                Console.ForegroundColor = Assert ? ConsoleColor.White : ConsoleColor.Red;
                Console.WriteLine(this.ToString());                
            }

            public override string ToString()
            {
                return
                    $"| {Execute.Method.Name.PadRight(40)} | {Average.ToString().PadRight(20)} | {ElapsedTime.ToString().PadRight(20)} | {Result.Count().ToString().PadRight(20)} | {Qty.ToString().PadRight(20)} | {Assert.ToString().PadRight(20)} |";
            }
        }
        
        static void Main(string[] args)
        {
            const int start = 100000;
            const int times = 6;

            var results = new List<TestResult>();

            var actions = new List<Func<int, IEnumerable<int>>>()
            {
                SeqInsert,
                SeqPreAllocInsert, 
                ParallelUnsafeInsert,
                ParallelLockInsert,
                ParallelThreadSafeInsert,
                ParallelPreAllocInsert,
            };
            
            
            // Execute tests
            var qty = start;
            for (var i = 0; i < times; i++)
            {
                Console.WriteLine($"[{DateTime.Now}] ## Testing for {qty} items...");
                foreach (var action in actions)
                {
                    Console.WriteLine($"[{DateTime.Now}] [{action.Method.Name}] Running...");
                    
                    var sw = Stopwatch.StartNew();
                    var result = action(qty);
                    sw.Stop();
                    
                    results.Add(new TestResult()
                    {
                        Assert = result.Count() == qty,
                        Execute = action,
                        Qty = qty,
                        Result = result,
                        ElapsedTime = sw.ElapsedMilliseconds
                    });
                }
                qty *= 10;
            }
            
            TestResult.MakeHeader();

            // Show results
            foreach (var line in results.OrderBy(p => p.Average))
            {
                line.WriteLine();
            }
            
            TestResult.MakeFooter();
        }

        static int CalcIntValue(int seed)
        {
            // Just to process something
            return int.MaxValue / seed.GetHashCode().ToString().ToCharArray().Sum(c => (int)c);
        }

        static IEnumerable<int> SeqInsert(int qty)
        {
            var ret = new List<int>();
            for (var i = 0; i < qty; i++)
            {
                ret.Add(CalcIntValue(i));
            }
            

            return ret;
        }
        
        static IEnumerable<int> SeqPreAllocInsert(int qty)
        {
            var ret = new int[qty];
            for (var i = 0; i < qty; i++)
            {
                ret[i] = CalcIntValue(i);
            }

            return ret;
        }
        
        static IEnumerable<int> ParallelPreAllocInsert(int qty)
        {
            var ret = new int[qty];
            For (0, qty, i =>
            {
                ret[i] = CalcIntValue(i);
            });

            return ret;
        }          
        
        static IEnumerable<int> ParallelUnsafeInsert(int qty)
        {
            var ret = new List<int>(qty);

            For(0, qty, i =>
            {
                ret.Add(CalcIntValue(i));
                
            });

            return ret;
        }        
        
        static IEnumerable<int> ParallelLockInsert(int qty)
        {
            var ret = new List<int>(qty);

            For(0, qty, i =>
            {
                lock (ret)
                {
                    ret.Add(CalcIntValue(i));    
                }
            });

            return ret;
        }
        
        static IEnumerable<int> ParallelThreadSafeInsert(int qty)
        {
            var ret = new ConcurrentBag<int>();

            For(0, qty, i => ret.Add(CalcIntValue(i)));

            return ret;
        }         
    }
}