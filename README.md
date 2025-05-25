# BusBus - School Bus Helper Program, by Grok and Steve

BusBus is a Windows Forms application designed to streamline school bus route management, helping administrators, drivers, and school staff efficiently organize and monitor school bus operations.

## 🚀 Features

- **Route Management**: Create, edit, and delete bus routes
- **Driver Assignment**: Assign drivers to specific routes
- **Vehicle Tracking**: Maintain information about school buses and their status
- **User-Friendly Dashboard**: Visualize routes and bus status at a glance

## 🛠️ Technologies Used

- **Framework**: .NET 8.0 for Windows
- **UI**: Windows Forms
- **Data Access**: Entity Framework Core
- **Testing**: NUnit, Moq, NSubstitute
- **Code Coverage**: Coverlet

## 📋 Prerequisites

- Windows operating system
- .NET 8.0 SDK or later
- Visual Studio 2022 (recommended) or VS Code with C# extensions

## 🔧 Setup & Installation

1. **Clone the repository**
   ```
   git clone https://github.com/Bigessfour/BusBus.git
   cd BusBus
   ```

2. **Build the solution**
   ```
   dotnet build BusBus.sln
   ```

3. **Run the application**
   ```
   dotnet run
   ```

## 🧪 Testing

Run tests and generate coverage reports:

```
dotnet test --verbosity minimal --logger "console;verbosity=minimal"
```

Run tests with coverage:

```
dotnet test --collect:"XPlat Code Coverage" --settings ./CodeCoverage.runsettings --results-directory TestResults/Coverage --verbosity minimal
```

Generate HTML coverage report:

```
reportgenerator -reports:./TestResults/Coverage/coverage.cobertura.xml -targetdir:./TestResults/Coverage/Report -reporttypes:Html
```

## 🔄 Continuous Integration

This project uses GitHub Actions for continuous integration. The workflow automatically builds the project, runs tests, and generates code coverage reports for every push to the main branch and for all pull requests.

### CI Workflow Details

The CI pipeline performs the following steps:
- Builds the application using .NET 8.0
- Runs all tests with code coverage analysis
- Generates a detailed HTML coverage report
- Uploads the coverage report as an artifact

### Viewing Test Results

After each workflow run:
1. Go to the Actions tab in the GitHub repository
2. Select the completed workflow run
3. Download the coverage report artifact
4. Open the HTML report to see detailed code coverage information

### Running the CI Workflow Locally

You can run the same checks locally before pushing your changes:

```
dotnet restore
dotnet build --no-restore
dotnet test --verbosity minimal --logger "console;verbosity=minimal"
```

For more detailed information on working with GitHub Actions in this project, see the [GitHub Actions Guide](docs/github-actions-guide.md).

## 📁 Project Structure

- **Models/**: Contains data models for routes, drivers, and vehicles
- **Services/**: Business logic and service implementations
- **DataAccess/**: Database context and data access layer
- **UI/**: Windows Forms UI components
- **BusBus.Tests/**: Unit and integration tests

## 👥 Contributing

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'Add amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 📞 Contact

Project Link: [https://github.com/Bigessfour/BusBus](https://github.com/Bigessfour/BusBus)

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

### Third-party Dependencies

All dependencies use compatible open-source licenses:
- **Testing Frameworks**: NUnit, Moq, AutoFixture, NSubstitute (MIT/BSD)
- **Entity Framework**: Microsoft.EntityFrameworkCore (MIT)
- **Dependency Injection**: Microsoft.Extensions.DependencyInjection (MIT)

## Build Status

To build and test:
```bash
dotnet clean
dotnet build BusBus.sln
dotnet test BusBus.Tests/BusBus.Tests.csproj --verbosity minimal
```
