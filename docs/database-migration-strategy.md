# Database Migration Strategy for BusBus

## Current Issues
- Manual SQL scripts are error-prone and hard to track
- Schema changes require manual database updates
- No version control for database schema changes
- Risk of production/development schema drift

## Recommended Solution: Entity Framework Core Migrations

### 1. Migration-Based Schema Management

Instead of manual SQL scripts, use EF Core migrations:

```bash
# Create a new migration for any model changes
dotnet ef migrations add AddPMDriverIdToRoutes

# Update the database
dotnet ef database update

# Generate SQL script for production deployment
dotnet ef migrations script --output migration.sql
```

### 2. Environment-Specific Configuration

Set up different connection strings for different environments:

**appsettings.Development.json:**
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost\\SQLEXPRESS;Database=BusBusDB_Dev;Trusted_Connection=true;TrustServerCertificate=true;"
  }
}
```

**appsettings.Production.json:**
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=prod-server;Database=BusBusDB;User Id=busbus_app;Password=secure_password;TrustServerCertificate=true;"
  }
}
```

### 3. Database Initialization Strategy

Create a database seeding strategy that works across environments:

```csharp
public class DatabaseInitializer
{
    public static async Task InitializeAsync(AppDbContext context, ILogger logger)
    {
        // Apply pending migrations
        await context.Database.MigrateAsync();

        // Seed default data if needed
        await SeedDefaultDataAsync(context, logger);
    }
}
```

### 4. CI/CD Pipeline Integration

Integrate database updates into your build pipeline:

```yaml
# Example for Azure DevOps or GitHub Actions
- name: Update Database Schema
  run: |
    dotnet ef database update --connection "${{ secrets.DATABASE_CONNECTION_STRING }}"
```

## Benefits

1. **Version Control**: All schema changes are tracked in source control
2. **Rollback Capability**: Easy to rollback database changes
3. **Environment Consistency**: Same schema across all environments
4. **Automated Deployment**: Database updates as part of CI/CD
5. **Team Collaboration**: No more manual schema sync issues

## Implementation Steps

1. Ensure all current schema changes are applied
2. Create initial migration from current database state
3. Set up environment-specific configurations
4. Update deployment process to include migrations
5. Train team on migration workflow
