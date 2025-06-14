using Lighthouse.Backend.Models;

namespace Lighthouse.Backend.Tests.API
{
    internal static class RunChartDataGenerator
    {


        public static Dictionary<int, List<WorkItemBase>> GenerateRunChartData(int[] input)
        {
            var runChartData = new Dictionary<int, List<WorkItemBase>>();

            for (var index = 0; index < input.Length; index++)
            {
                runChartData[index] = new List<WorkItemBase>();

                for (var i = 0; i < input[index]; i++)
                {
                    runChartData[index].Add(new WorkItemBase
                    {
                        Id = index * 10 + i,
                        Name = $"Item {index * 10 + i}",
                        CreatedDate = DateTime.Now.AddDays(-index),
                        StartedDate = DateTime.Now.AddDays(-index + 1),
                        ClosedDate = DateTime.Now.AddDays(-index + 2)
                    });
                }
            }

            return runChartData;
        }
    }
}
