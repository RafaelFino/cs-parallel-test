using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
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

            public decimal Average => Math.Round((decimal)Result.Count()/Math.Max(ElapsedTime, 1) , 0);

            public static void MakeHeader()
            {
                Console.WriteLine("\n");
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
                return $"| {Execute.Method.Name.PadRight(40)} | {Average.ToString().PadRight(20)} | {ElapsedTime.ToString().PadRight(20)} | {Result.Count().ToString().PadRight(20)} | {Qty.ToString().PadRight(20)} | {Assert.ToString().PadRight(20)} |";
            }
        }
        
        static void Main(string[] args)
        {
            const int start = 100000;
            const int times = 5;

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
                Console.Write($"\n[{DateTime.Now}] ## Testing for {qty} items");
                foreach (var action in actions)
                {
                    StartProgressBar(action);

                    var sw = Stopwatch.StartNew();
                    var actionResult = action(qty);
                    sw.Stop();
                    
                    results.Add(new TestResult()
                    {
                        Assert = actionResult.Count() == qty,
                        Execute = action,
                        Qty = qty,
                        Result = actionResult,
                        ElapsedTime = sw.ElapsedMilliseconds
                    });
                    
                    FinishProgressBar();
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

        private static void FinishProgressBar()
        {
            Console.CursorLeft = _progressStart - 2;
            Console.Write(string.Empty.PadRight(_progessSize + 10, ' '));
        }

        private static void StartProgressBar(Func<int, IEnumerable<int>> action)
        {
            Console.Write($"\n[{DateTime.Now}] [{action.Method.Name}] Running ");
            Console.CursorLeft = _progressStart - 1;
            Console.Write('[');
            Console.CursorLeft = _progessSize + 1;
            Console.Write(']');
            Console.CursorLeft = _progressStart;
        }

        private static char _progressBar = '#';
        private static int _progessSize = 140;
        private static int _progressStart = 70;
        private static int _progressInterval = 1000000;
        
        static int CalcIntValue(int seed)
        {
            // Just to process something and simulate CPU use
            if (seed % _progressInterval == 0)
            {
                Console.Write(_progressBar);

                if (Console.CursorLeft > _progessSize)
                {
                    Console.CursorLeft += 1;
                    Console.Write($" {seed}");
                    Console.CursorLeft = _progressStart;
                    
                    if (_progressBar == '>')
                    {
                        _progressBar = '#';
                    }
                    else
                    {
                        _progressBar = '>';
                    }
                }
            }
            
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