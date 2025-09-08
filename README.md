
DfE Core Libraries
==================

This repository consists of a .NET solution containing multiple class libraries, with each library published as a standalone NuGet package. The libraries follow the naming convention: `GovUK.Dfe.CoreLibs.{library_name}`.

Deployment and versioning process
--------------------------------------

![Nuget Package Deployment](./nuget-deployment.png)


Adding a New Library to the Repository
--------------------------------------

To add a new library to this repository and automatically publish it as a NuGet package, follow these steps:

1.  **Create a new library** in the `src` folder in the root of the solution.
2.  **Copy the YAML workflow file** used for other libraries (e.g., from `Caching`) into **.github/workflows** , and modify them as needed to match your new library.

### File: `build-deploy-{library_name}.yml`

For example, if your new library is called "FileService," name the file `build-deploy-FileService.yml`.

#### Example Content (Replace with your library name):

```yaml
        name: CI & Pack GovUK.Dfe.CoreLibs.FileService

        on:
          push:
            branches: [ main ]
            paths:
              - "src/GovUK.Dfe.CoreLibs.FileService/**"
          pull_request:
            branches: [ main ]
            paths:
              - "src/GovUK.Dfe.CoreLibs.FileService/**"

        jobs:
          build-and-test:
            uses: ./.github/workflows/build-test-template.yml
            with:
              project_name: GovUK.Dfe.CoreLibs.FileService
              project_path: src/GovUK.Dfe.CoreLibs.FileService
              run_tests: false
            secrets:
              SONAR_TOKEN: ${{ secrets.SONAR_TOKEN }}

          pack-and-release:
            needs: build-and-test
            if: needs.build-and-test.result == 'success'
            uses: ./.github/workflows/pack-template.yml
            with:
              project_name: GovUK.Dfe.CoreLibs.FileService
              project_path: src/GovUK.Dfe.CoreLibs.FileService/GovUK.Dfe.CoreLibs.FileService.csproj

```
    

Make sure to:

*   Replace `GovUK.Dfe.CoreLibs.FileService` with your new library name.
*   Ensure the path to the new library is correct.


Workflows Explanation
---------------------

*   **Build, Test and Pack Workflow** (`build-deploy-{library_name}.yml`): This workflow is responsible for building, testing, version, pack and release your library.

Versioning and Auto-Publishing
------------------------------

- **GitVersion-driven SemVer**  
  A single `GitVersion.yml` at the repository root defines the versioning strategy (v5’s **ContinuousDelivery** mode). Tags of the form  

 ```
GovUK.Dfe.CoreLibs.<LibraryName>-X.Y.Z
GovUK.Dfe.CoreLibs.<LibraryName>-X.Y.Z-prerelease…
 ```

 serve as the source of truth for all version calculations.

- **Pull-Request (Prerelease) Builds**  
On each PR build, GitVersion locates the highest existing `X.Y.Z` tag, reuses that base version, and appends `-prerelease-N`.  
The workflow then publishes `X.Y.Z-prerelease-N` to GitHub Packages and marks the GitHub Release as a prerelease.

- **Main-Branch (Production) Builds**  
When a PR is merged into `main`, GitVersion drops the prerelease suffix (or increments the patch if the prerelease already matches production) to produce a clean `X.Y.(Z+1)`.  
The workflow publishes that package version to the production feed and creates a non-prerelease GitHub Release.

- **Minor or Major Version Bumps**  
1. On the feature branch, apply a tag for the new base version:  
   ```bash
   git tag GovUK.Dfe.CoreLibs.<LibraryName>-2.0.0
   git push origin feature/XYZ --tags
   ```  
2. Open the PR: CI will package `2.0.0-prerelease-1`.  
3. Merge into `main`: CI publishes `2.0.0` (or `2.0.1` on subsequent changes).

- **Publication Workflow**  
1. **Build & Test** via `build-test-template.yml`.  
2. **Determine Version** using GitVersion plus a small shell snippet to assemble `Major.Minor.Patch[-prerelease-N]`.  
3. **Pack & Push** with `dotnet pack -p:PackageVersion=…` and `dotnet nuget push … --skip-duplicate`.  
4. **GitHub Release** is created automatically, extracting `%release-note:…%` tokens from the PR description or merge commit.



Release and Release Note
------------------------------

Each time a package is published succesfully, a new tag and release is created in the repository.

*   **Custom Release Note:** To add a Release Note to the release, simply include the following to your commit messages:
        
            (%release-note: {note} %)
        
        Example:
        
            (%release-note: Example Message to be Added in the Release Note %)
        

