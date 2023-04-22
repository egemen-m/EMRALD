﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Hunter.Psf;
using Hunter.Hra;

using NUnit.Framework;

namespace Hunter.Tests.PerformanceShapingFactorTests
{
    [TestFixture]
    public class LagLinger_Model_Tests
    {
        [Test]
        public void Test1_value_before_k()
        {
            LagAdaptLinger lagLinger = new LagAdaptLinger();
            TestContext.WriteLine($"{lagLinger.getValue(t: 60)}");
            TestContext.WriteLine($"{lagLinger.getValue(t: 65)}");
            TestContext.WriteLine($"{lagLinger.getValue(t: 70)}");
            TestContext.WriteLine($"{lagLinger.getValue(t: 1860)}");
            TestContext.WriteLine($"{lagLinger.getValue(t: 2000)}");
            TestContext.WriteLine($"{lagLinger.getValue(t: 3660)}");
            TestContext.WriteLine($"{lagLinger.getValue(t: 7300)}");

        }

        [Test]
        public void Test1_AvailableTimeExpires()
        {
            LagAdaptLinger lagLinger = new LagAdaptLinger();
            lagLinger.TriggerLag(t: 60, k: 2);
            TestContext.WriteLine($"{lagLinger.getValue(t:60)}");
            TestContext.WriteLine($"{lagLinger.getValue(t: 65)}");
            TestContext.WriteLine($"{lagLinger.getValue(t: 70)}");
            TestContext.WriteLine($"{lagLinger.getValue(t: 1860)}");
            TestContext.WriteLine($"{lagLinger.getValue(t: 2000)}");
            TestContext.WriteLine($"{lagLinger.getValue(t: 3660)}");
            TestContext.WriteLine($"{lagLinger.getValue(t: 7300)}");

        }

        [Test]
        public void Test2_TaskCompletedBefore_Tlag()
        {
            LagAdaptLinger lagLinger = new LagAdaptLinger();
            lagLinger.TriggerLag(t: 60, k: 2);
            TestContext.WriteLine($"{lagLinger.getValue(t: 60)}");
            TestContext.WriteLine($"{lagLinger.getValue(t: 65)}");
            TestContext.WriteLine($"{lagLinger.getValue(t: 70)}");

            lagLinger.TriggerLinger(t: 1860);

            TestContext.WriteLine($"{lagLinger.getValue(t: 1860)}");
            TestContext.WriteLine($"{lagLinger.getValue(t: 2000)}");
            TestContext.WriteLine($"{lagLinger.getValue(t: 3660)}");
            TestContext.WriteLine($"{lagLinger.getValue(t: 7300)}");

        }

        [Test]
        public void Test1_csv()
        {
            LagAdaptLinger _lagLinger1 = new LagAdaptLinger();
            LagAdaptLinger _lagLinger2 = new LagAdaptLinger();
            LagAdaptLinger _lagLinger3 = new LagAdaptLinger();
            LagAdaptLinger _lagLinger4 = new LagAdaptLinger();

            double dt = 1;
            double end = 3600 * 5;
            StringBuilder csvData = new StringBuilder();

            csvData.AppendLine("Time (s),Available Time Expires before Task Completed,Task Completed before t0 + tLag,Task Completed after t0 + tLag but before Available Time expires,Double Stimuli");

            for (double x = 0; x <= end; x += dt)
            {
                if (x == 60)
                {
                    _lagLinger1.TriggerLag(x, k:5);
                    _lagLinger2.TriggerLag(x, k: 5);
                    _lagLinger3.TriggerLag(x, k: 5);
                    _lagLinger4.TriggerLag(x, k: 1.5);
                }

                if (x == 3660)
                {
                    _lagLinger4.TriggerLag(x, k: 2);
                }

                if (x == 3660 + 3600)
                {
                    _lagLinger4.TriggerLag(x, k: 2.5);
                }

                if (x == 1860 + 3600 + 3600)
                {
                   // _lagLinger4.TriggerLinger(x);
                }

                if (x == 3660 + 3600)
                {
                    _lagLinger4.TriggerLag(x, k: 3);
                }


                if (x == 1860) 
                { 
                    _lagLinger2.TriggerLinger(x);
                }

                if (x == 4260)
                { 
                    _lagLinger3.TriggerLinger(x);
                    _lagLinger4.TriggerLinger(x);
                }

                double value1 = _lagLinger1.getValue(x);
                double value2 = _lagLinger2.getValue(x);
                double value3 = _lagLinger3.getValue(x);
                double value4 = _lagLinger4.getValue(x);

                csvData.AppendLine($"{x/3600},{value1},{value2},{value3},{value4}");
            }
            string outputDirectory = TestUtility.GetOutputDirectory();
            TestUtility.CleanDirectory(outputDirectory);
            string outputFile = Path.Combine(outputDirectory, "output.csv");
            File.WriteAllText(outputFile, csvData.ToString());
        }
    }
}
