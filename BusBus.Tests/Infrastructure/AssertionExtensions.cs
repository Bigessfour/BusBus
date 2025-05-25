using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BusBus.Models;
using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.EntityFrameworkCore;

namespace BusBus.Tests.Infrastructure
{
    /// <summary>
    /// Extension methods to help with common test assertions
    /// </summary>
    public static class AssertionExtensions
    {
        /// <summary>
        /// Asserts that a Route entity has the expected values
        /// </summary>
        public static void ShouldMatchRoute(this Route actual, Route expected)
        {
            using var scope = new AssertionScope();
            
            actual.Should().NotBeNull();
            if (actual == null) throw new ArgumentNullException(nameof(actual));
            if (expected == null) throw new ArgumentNullException(nameof(expected));
            actual.Id.Should().Be(expected.Id);
            actual.Name.Should().Be(expected.Name);
            actual.RouteDate.Should().Be(expected.RouteDate);
            actual.ScheduledTime.Should().Be(expected.ScheduledTime);
            actual.StartLocation.Should().Be(expected.StartLocation);
            actual.EndLocation.Should().Be(expected.EndLocation);
            actual.AMStartingMileage.Should().Be(expected.AMStartingMileage);
            actual.AMEndingMileage.Should().Be(expected.AMEndingMileage);
            actual.PMStartMileage.Should().Be(expected.PMStartMileage);
            actual.PMEndingMileage.Should().Be(expected.PMEndingMileage);
            actual.AMRiders.Should().Be(expected.AMRiders);
            actual.PMRiders.Should().Be(expected.PMRiders);
        }
        
        /// <summary>
        /// Asserts that a Driver entity has the expected values
        /// </summary>
        public static void ShouldMatchDriver(this Driver actual, Driver expected)
        {
            using var scope = new AssertionScope();
            
            if (actual == null) throw new ArgumentNullException(nameof(actual));
            if (expected == null) throw new ArgumentNullException(nameof(expected));
            actual.Id.Should().Be(expected.Id);
            actual.FirstName.Should().Be(expected.FirstName);
            actual.LastName.Should().Be(expected.LastName);
            actual.PhoneNumber.Should().Be(expected.PhoneNumber);
            actual.Email.Should().Be(expected.Email);
            // Removed: EmployeeNumber and HireDate checks
        }
        
        /// <summary>
        /// Asserts that a collection contains the expected number of items
        /// </summary>
        public static void ShouldHaveCount<T>(this IEnumerable<T> collection, int expectedCount)
        {
            collection.Should().HaveCount(expectedCount);
        }
        
        /// <summary>
        /// Asserts that a database operation succeeds and returns the expected type
        /// </summary>
        public static async Task<T> ShouldSucceedAsync<T>(this Task<T> task)
        {
            // Removed NotThrowAsync: just await the task and return result
            return await task;
        }
        
        /// <summary>
        /// Asserts that an entity exists in the database with the given ID
        /// </summary>
        public static async Task<T> ShouldExistInDatabaseAsync<T>(this DbSet<T> dbSet, object id) where T : class
        {
            if (dbSet == null) throw new ArgumentNullException(nameof(dbSet));
            var entity = await dbSet.FindAsync(id);
            entity.Should().NotBeNull($"Entity with ID {id} should exist in the database");
            return entity!;
        }
        
        /// <summary>
        /// Asserts that an entity does not exist in the database with the given ID
        /// </summary>
        public static async Task ShouldNotExistInDatabaseAsync<T>(this DbSet<T> dbSet, object id) where T : class
        {
            if (dbSet == null) throw new ArgumentNullException(nameof(dbSet));
            var entity = await dbSet.FindAsync(id);
            entity.Should().BeNull($"Entity with ID {id} should not exist in the database");
        }
    }
}
