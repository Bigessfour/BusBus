# MSTest & dotnet test Usage for BusBus

## Running Tests in Parallel
```
dotnet test --parallel
```

## Filtering Tests
- By Category (e.g., UI):
  ```
  dotnet test --filter TestCategory=UI
  ```
- By Name (e.g., CoreTest):
  ```
  dotnet test --filter Name~CoreTest
  ```

## Collecting Code Coverage
```
dotnet test --collect:"Code Coverage"
```

## Generating TRX Report
```
dotnet test --logger "trx;LogFileName=TestResults.trx"
```

## Combining Options (Example)
```
dotnet test --filter TestCategory=UI --collect:"Code Coverage" --logger "trx;LogFileName=TestResults.trx" --parallel
```

---

- Use these commands in your terminal or CI pipeline.
- You can combine `--filter`, `--collect`, `--logger`, and `--parallel` as needed.
- For more info, see:
  - [dotnet test documentation](https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-test)
  - [MSTest ParallelizeAttribute](https://learn.microsoft.com/en-us/dotnet/api/microsoft.visualstudio.testtools.unittesting.parallelizeattribute)
