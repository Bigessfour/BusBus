# Working with GitHub Actions in BusBus

This guide provides a comprehensive overview of how to work with GitHub Actions in the BusBus project.

## Understanding GitHub Actions Workflows

GitHub Actions are defined in YAML files located in the `.github/workflows` directory. Each workflow file defines:

1. **Triggers** - When the workflow should run
2. **Jobs** - Groups of steps that execute on the same runner
3. **Steps** - Individual tasks that run commands or actions

## Available Workflows

The BusBus project includes the following GitHub Actions workflows:

1. **BusBus CI** (`ci.yaml`) - Basic continuous integration workflow
   - Builds the project
   - Runs tests
   - Generates code coverage reports

2. **BusBus Extended CI** (`extended-ci.yaml`) - Enhanced workflow with additional features
   - Everything from the basic CI workflow
   - Weekly scheduled runs
   - Code style checks
   - Release creation
   - Artifacts publishing

## Triggering Workflows Manually

You can trigger workflows manually from GitHub's web interface:

1. Go to your repository on GitHub
2. Click on the "Actions" tab
3. Select the workflow you want to run
4. Click "Run workflow" button
5. Select the branch to run it on
6. Click "Run workflow" again

## Viewing Workflow Results

After a workflow runs, you can view the results:

1. Go to the "Actions" tab in your repository
2. Click on the workflow run you want to view
3. See the summary and logs for each job and step
4. Download artifacts (like coverage reports) from the "Artifacts" section

## Common Tasks

### Running Tests Locally (Same as CI)

To run the same tests that GitHub Actions runs:

```powershell
dotnet test --collect:"XPlat Code Coverage" --settings ./CodeCoverage.runsettings --results-directory TestResults/Coverage /p:Threshold=80
```

### Generating Coverage Reports Locally

To generate the same coverage report locally:

```powershell
# Install the tool if you haven't already
dotnet tool install -g dotnet-reportgenerator-globaltool

# Generate the report
reportgenerator -reports:TestResults/Coverage/**/coverage.cobertura.xml -targetdir:TestResults/Coverage/Report -reporttypes:Html
```

### Creating a Release

To create a release:

1. Create and push a tag:
   ```powershell
   git tag v1.0.0
   git push origin v1.0.0
   ```
2. The workflow will automatically create a release

## Customizing the Workflows

To customize the existing workflows:

1. Edit the YAML files in the `.github/workflows` directory
2. Commit and push your changes
3. The updated workflow will be used for future runs

### Adding New Steps

To add a new step to a workflow, add it to the `steps` section:

```yaml
steps:
  # Existing steps...
  
  - name: My new step
    run: echo "This is a new step"
```

### Adding New Jobs

To add a new job, add it under the `jobs` section:

```yaml
jobs:
  # Existing jobs...
  
  my-new-job:
    runs-on: ubuntu-latest
    steps:
      - name: Check out code
        uses: actions/checkout@v4
      
      - name: Run a command
        run: echo "Hello from my new job"
```

## Troubleshooting

If a workflow fails, you can:

1. Check the logs in the GitHub Actions tab
2. Look for specific error messages
3. Run the same commands locally to debug
4. Make changes to fix the issues
5. Push again to trigger a new workflow run

## GitHub Actions Best Practices

1. **Keep workflows focused** - Each workflow should have a clear purpose
2. **Use reusable actions** - Use existing actions when possible
3. **Cache dependencies** - Speed up workflows by caching
4. **Limit workflow triggers** - Only run workflows when necessary
5. **Secure sensitive data** - Use GitHub Secrets for API keys, etc.
6. **Use descriptive names** - Give steps clear names to identify issues easily

## Additional Resources

- [GitHub Actions Documentation](https://docs.github.com/en/actions)
- [GitHub Actions Marketplace](https://github.com/marketplace?type=actions)
- [GitHub Actions for .NET](https://github.com/actions/setup-dotnet)
