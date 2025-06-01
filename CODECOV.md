# Codecov Integration Guide

1. **Sign up at [Codecov](https://about.codecov.io/)** and connect your GitHub repository.
2. **Add the Codecov upload step to your GitHub Actions workflow:**

Add this after your test and coverage steps in `.github/workflows/ci.yml`:

```yaml
- name: Upload coverage to Codecov
  uses: codecov/codecov-action@v4
  with:
    files: CoverageReport/**/coverage.cobertura.xml
    fail_ci_if_error: true
```

3. **Add a coverage badge to your `README.md`:**

After your first successful upload, get the badge markdown from your Codecov dashboard and add it to the top of your `README.md`:

```
[![codecov](https://codecov.io/gh/OWNER/REPO/branch/main/graph/badge.svg)](https://codecov.io/gh/OWNER/REPO)
```

Replace `OWNER` and `REPO` with your GitHub username and repository name.

---

**Summary:**
- Codecov will comment on PRs with coverage changes.
- The badge will show live coverage status.
- You can review detailed coverage and CRAP metrics on the Codecov dashboard.
