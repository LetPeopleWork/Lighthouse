using System.Globalization;
using Lighthouse.Backend.API.DTO;
using Lighthouse.Backend.Models;

namespace Lighthouse.Backend.Services.Implementation
{
    public static class BlackoutDaysExtensions
    {
        public static HashSet<int> GetBlackoutDayIndices(this IEnumerable<BlackoutPeriod> blackoutPeriods, DateTime startDate, DateTime endDate)
        {
            var indices = new HashSet<int>();
            var totalDays = (endDate.Date - startDate.Date).Days + 1;

            foreach (var period in blackoutPeriods)
            {
                var periodStart = period.Start.ToDateTime(TimeOnly.MinValue);
                var periodEnd = period.End.ToDateTime(TimeOnly.MinValue);

                var overlapStart = periodStart < startDate.Date ? startDate.Date : periodStart;
                var overlapEnd = periodEnd > endDate.Date ? endDate.Date : periodEnd;

                if (overlapStart > overlapEnd)
                {
                    continue;
                }

                var startIndex = (overlapStart - startDate.Date).Days;
                var endIndex = (overlapEnd - startDate.Date).Days;

                for (var i = startIndex; i <= endIndex && i < totalDays; i++)
                {
                    indices.Add(i);
                }
            }

            return indices;
        }

        public static bool IsBlackoutDay(this IEnumerable<BlackoutPeriod> blackoutPeriods, DateOnly date)
        {
            return blackoutPeriods.Any(p => date >= p.Start && date <= p.End);
        }

        public static bool IsBlackoutDay(this IEnumerable<BlackoutPeriod> blackoutPeriods, DateTime dateTime)
        {
            var date = DateOnly.FromDateTime(dateTime.Date);
            return blackoutPeriods.IsBlackoutDay(date);
        }

        public static bool HasOverlapWithDateRange(this IEnumerable<BlackoutPeriod> blackoutPeriods, DateTime startDate, DateTime endDate)
        {
            var rangeStart = DateOnly.FromDateTime(startDate.Date);
            var rangeEnd = DateOnly.FromDateTime(endDate.Date);

            return blackoutPeriods.Any(p => p.Start <= rangeEnd && p.End >= rangeStart);
        }

        public static ProcessBehaviourChart AnnotateBlackoutDays(this IEnumerable<BlackoutPeriod> blackoutPeriods, ProcessBehaviourChart chart)
        {
            if (chart.DataPoints.Length == 0)
            {
                return chart;
            }

            if (!blackoutPeriods.Any())
            {
                return chart;
            }

            var annotatedPoints = chart.DataPoints.Select(dp =>
            {
                var dateTime = DateTime.Parse(dp.XValue, CultureInfo.InvariantCulture);
                var isBlackout = blackoutPeriods.IsBlackoutDay(dateTime);
                return isBlackout ? dp with { IsBlackout = true } : dp;
            }).ToArray();

            return chart with { DataPoints = annotatedPoints };
        }
    }
}
