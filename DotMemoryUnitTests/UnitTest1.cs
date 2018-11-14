using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using FluentAssertions;
using JetBrains.dotMemoryUnit;
using Xunit;
using Xunit.Abstractions;

namespace DotMemoryUnitTests
{
    public class UnitTest1
    {
        public UnitTest1(ITestOutputHelper output)
        {
            DotMemoryUnitTestOutput.SetOutputMethod(output.WriteLine);
        }

        [Fact]
        public void CountMemoryInstances()
        {
            var employee1 = new Employee();
            var employee2 = new Employee();

            dotMemory.Check(check =>
                check.GetObjects(where => where.Type.Is<Employee>()).ObjectsCount.Should().Be(2));
        }

        [Fact]
        public void LeakingObjects()
        {
            var calculator = new Calculator();

            calculator.DoSomething();

            //calculator.Dispose();

            dotMemory.Check(memory =>
            {
                memory.GetObjects(where => where.Type == typeof(Employee)).ObjectsCount.Should().Be(0);
            });
        }
        
        [Fact]
        public void LeakingObjects2()
        {
            var calculator = new Calculator();

            calculator.DoSomething();

            var memorySnapshot = dotMemory.Check();

            calculator.Dispose();

            dotMemory.Check(memory =>
            {
                memory.GetDifference(memorySnapshot)
                    .GetDeadObjects(where => where.Type == typeof(Employee))
                    .ObjectsCount.Should().Be(1);
            });
        }

        [Fact]
        public void CheckpointCountDifferences()
        {
            var employee1 = new Employee();

            var memoryCheckPoint = dotMemory.Check();

            var employee2 = new Employee();
            var employee3 = new Employee();

            dotMemory.Check(memory =>
                    memory.GetDifference(memoryCheckPoint)
                        .GetNewObjects()
                        .GetObjects(i => i.Type == typeof(Employee))
                        .ObjectsCount.Should().Be(2));

        }

        [Fact]
        public void SizeInBytes()
        {
            var employees = new List<Employee>();

            for (var i = 0; i < 100; i++)
            {
                employees.Add(new Employee());
            }

            dotMemory.Check(memory =>
                memory.GetObjects(i => i.Type == typeof(Employee))
                   .SizeInBytes.Should().BeLessOrEqualTo(2500));
        }

        [Fact]
        [DotMemoryUnit(CollectAllocations = true)]
        public void CountAllocations()
        {
            var employees = new Employees();

            var snapshot = dotMemory.Check();

            employees.CrappyMethod(10);
            //employees.BetterMethod(10);

            dotMemory.Check(memory =>
            {
                memory.GetObjects(where => where.Type == typeof(Employee)).ObjectsCount.Should().Be(0);

                memory.GetTrafficFrom(snapshot)
                    .Where(where => where.Type == typeof(Employee))
                    .AllocatedMemory.ObjectsCount.Should().Be(1);
            });
        }
    }

    public class Employee
    {
    }

    public class Employees
    {
        public void CrappyMethod(int count)
        {
            for (int i = 0; i < count; i++)
            {
                var employee = new Employee();

                //Do stuff
            }
        }

        public void BetterMethod(int count)
        {
            var employee = new Employee();

            for (int i = 0; i < count; i++)
            {
                //Do stuff
            }
        }
    }

    public class Calculator : IDisposable
    {
        private readonly Leaky _leaky;

        public Calculator()
        {
            _leaky = new Leaky();
        }

        public void DoSomething()
        {
            _leaky.DoSomething(new Employee());
        }

        public void Dispose()
        {
            _leaky.Dispose();
        }
    }

    public class Leaky : IDisposable
    {
        private readonly List<Employee> _employees = new List<Employee>();

        public void DoSomething(Employee employee)
        {
            _employees.Add(employee);
        }

        public void Dispose()
        {
            _employees.Clear();
        }
    }
}
