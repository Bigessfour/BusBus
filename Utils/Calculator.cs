#nullable enable
using System;

namespace BusBus.Utils
{
    /// <summary>
    /// Utility class for mathematical calculations
    /// </summary>
    public static class Calculator
    {
        /// <summary>
        /// Adds two integers
        /// </summary>
        /// <param name="a">First number</param>
        /// <param name="b">Second number</param>
        /// <returns>Sum of the two numbers</returns>
        public static int Add(int a, int b)
        {
            return a + b;
        }

        /// <summary>
        /// Calculates mileage difference
        /// </summary>
        /// <param name="startMileage">Starting mileage</param>
        /// <param name="endMileage">Ending mileage</param>
        /// <returns>Mileage difference</returns>
        public static int CalculateMileageDifference(int startMileage, int endMileage)
        {
            return Math.Max(0, endMileage - startMileage);
        }

        /// <summary>
        /// Calculates total daily mileage
        /// </summary>
        /// <param name="amMileage">AM mileage</param>
        /// <param name="pmMileage">PM mileage</param>
        /// <returns>Total daily mileage</returns>
        public static int CalculateTotalDailyMileage(int amMileage, int pmMileage)
        {
            return amMileage + pmMileage;
        }
    }
}
