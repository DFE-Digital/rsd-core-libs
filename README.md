
DfE Core Libraries
==================

This repository consists of a .NET solution containing multiple class libraries, with each library published as a standalone NuGet package. The libraries follow the naming convention: `DfE.CoreLibs.{library_name}`.

Deployment and versioning process
--------------------------------------

![Nuget Package Deployment](./nuget-deployment.png)


Adding a New Library to the Repository
--------------------------------------

To add a new library to this repository and automatically publish it as a NuGet package, follow these steps:

1.  **Create a new library** in the `src` folder in the root of the solution.
2.  **Copy the two YAML workflow files** used for other libraries (e.g., from `Caching`) into your new library directory, and modify them as needed to match your new library.

### File 1: `build-test-{library_name}.yml`

For example, if your new library is called "FileService," name the file `build-test-FileService.yml`.

#### Example Content (Replace with your library name):

```yaml
        name: Build DfE.CoreLibs.FileService

        on:
          push:
            branches:
              - main
            paths:
              - 'src/DfE.CoreLibs.FileService/**'
          pull_request:
            branches:
              - main
            paths:
              - 'src/DfE.CoreLibs.FileService/**' 

        jobs:
          build-and-test:
            uses: ./.github/workflows/build-test-template.yml
            with:
              project_name: DfE.CoreLibs.FileService
              project_path: src/DfE.CoreLibs.FileService
              run_tests: false
            secrets:
              SONAR_TOKEN: ${{ secrets.SONAR_TOKEN }}
```
    

Make sure to:

*   Replace `DfE.CoreLibs.FileService` with your new library name.
*   Ensure the path to the new library is correct.

### File 2: `pack-{library_name}.yml`

For example, name the file `pack-FileService.yml` for your new library.

#### Example Content (Replace with your library name):

```yaml
        name: Pack DfE.CoreLibs.FileService

        on:
          workflow_run:
            workflows: ["Build DfE.CoreLibs.FileService"]
            types:
              - completed

        jobs:
          build-and-package:
            if: ${{ github.event.workflow_run.conclusion == 'success' && github.event.workflow_run.head_branch == 'main' && github.event.workflow_run.event != 'pull_request' }}
            uses: ./.github/workflows/nuget-package-template.yml
            with:
              project_name: DfE.CoreLibs.FileService
              project_path: src/DfE.CoreLibs.FileService
              nuget_package_name: DfE.CoreLibs.FileService

```
    

Workflows Explanation
---------------------

*   **Build and Test Workflow** (`build-test-{library_name}.yml`): This workflow is responsible for building and testing your library.
*   **Pack Workflow** (`pack-{library_name}.yml`): This workflow handles versioning and packaging of your library, but it only runs after the build and test workflow successfully completes.

Versioning and Auto-Publishing
------------------------------

*   **Initial Versioning:** The first time your library is published, the version will start at `1.0.0`.
*   **Automatic Increment:** With subsequent changes, the patch version will increment automatically (e.g., `1.0.1`, `1.0.2`, and so on).
*   **Custom Version Bumps:** To bump the **minor** or **major** version of your library, follow these steps:
    1.  Make the necessary changes in your library.
    2.  Commit your changes with a message like the following:
        
            (#update {Project_Name} package version to {version_number})
        
        Example:
        
            (#update DfE.CoreLibs.FileService package version to 1.1.0)
        

The packaging workflow will then automatically set the version to `1.1.0` and increment the patch part (`1.1.x`) with each further change.