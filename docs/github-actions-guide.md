# Using GitHub Actions with BusBus

This document provides detailed information about how to use and customize the GitHub Actions workflow in the BusBus project.

## What is GitHub Actions?

GitHub Actions is a continuous integration and continuous delivery (CI/CD) platform that allows you to automate your build, test, and deployment pipeline directly from GitHub. It enables you to create workflows that build and test every pull request to your repository, or deploy merged pull requests to production.

## BusBus CI Workflow

The BusBus project includes a CI workflow that automatically runs whenever code is pushed to the main branch or when a pull request is created. This workflow helps ensure code quality by building the application, running tests, and generating code coverage reports.

### Workflow File Location

The workflow is defined in the `.github/workflows/ci.yml` file in the repository.

### Workflow Trigger Events

The workflow is triggered on:
- Push events to the `main` branch
- Pull request events targeting the `main` branch

### Workflow Steps

1. **Checkout Code**: Checks out the repository code
2. **Setup .NET**: Sets up the .NET SDK version 8.0
3. **Restore Dependencies**: Restores NuGet packages
4. **Build**: Builds the solution
5. **Run Tests with Coverage**: Executes tests and collects code coverage metrics
6. **Generate Coverage Report**: Creates an HTML report from the coverage data
7. **Upload Coverage Report**: Uploads the coverage report as a workflow artifact

## Viewing Workflow Results

### Build and Test Results

1. Go to the GitHub repository in your web browser
2. Click on the "Actions" tab
3. Find the most recent workflow run and click on it
4. Review the output of each step in the workflow

### Code Coverage Results

1. After a workflow run completes, click on the run
2. Scroll down to the "Artifacts" section
3. Download the "coverage-report" artifact
4. Extract the ZIP file
5. Open the "Report/index.html" file in a web browser to view the coverage report

## Customizing the Workflow

### Changing the Trigger Events

To change when the workflow runs, modify the `on` section in the workflow file:

```yaml
on:
  push:
    branches: [ main, develop ]  # Add more branches as needed
  pull_request:
    branches: [ main, develop ]  # Add more branches as needed
```

### Modifying Test Thresholds

The current workflow enforces an 80% code coverage threshold. To change this:

1. Find the line containing `/p:Threshold=80` in the workflow file
2. Change the value to your desired coverage percentage

### Adding Deployment Steps

To add deployment after successful tests:

1. Add a new job after the `build-and-test` job
2. Configure the deployment steps according to your hosting environment
3. Use the `needs: build-and-test` property to ensure deployment only happens after successful tests

## Troubleshooting

### Common Issues

1. **Build Failures**: Check for syntax errors or missing dependencies
2. **Test Failures**: Examine the test output to identify which tests are failing
3. **Coverage Threshold Not Met**: Review the coverage report to find areas needing more tests

### Workflow Debugging

For detailed debugging:

1. Add the following step to your workflow to enable debug logging:
   ```yaml
   - name: Enable debug logging
     run: echo "::debug::on"
   ```

2. Use the `ACTIONS_STEP_DEBUG` secret in your repository settings to enable step debugging.

## Additional Resources

- [GitHub Actions Documentation](https://docs.github.com/en/actions)
- [.NET Testing with GitHub Actions](https://docs.github.com/en/actions/guides/building-and-testing-net)
- [Code Coverage Reporting](https://github.com/marketplace/actions/code-coverage-report)