# Prerequisites
The package can be used with Unity 2019.2.0a12+.
# How to test the package?
The root of this folder is a Unity project, so opening this folder with Unity will load everything up. The package code is located in the Packages folder, where it can be modified freely. When opening Unity, with this root folder, testing can be done through the test runner. If needed the packages can also be moved to another project, the test project is not necessary in order to run the tests. But the testproject depend on the packages inside of the Packages folder.
# Changelog
Refer to [Changelog](/Packages/com.unity.ide.rider/CHANGELOG.md)

# Contributing
This project welcomes contributions and suggestions. Please have a look at our [Guidelines](/Packages/com.unity.ide.rider/CONTRIBUTING.md) for contributing.

# Release workflow 
We have 2 release streams, one for `4.0.x` targeting U7, and we have `3.0.x` for LTS versions
The `4.0.x` stream has only the pack job and the rest of the release process has to be done manually by copying in the source code of U7. 

The `3.0.x` stayed the same, meaning:
- Creating a feature branch targeting `next/master-3.0` 
- Once the work has laneded in `next/master-3.0`, we will create a `release/x.y.z` with the new release version, the `package.json` and the changelog needs to be updated manually before the release can be kicked off.
- Create a new release stream in [PackageWorks](https://package-works.prd.cds.internal.unity3d.com/project?id=4779)
- Once the `release/x.y.z` is created the CI will trigger automatically to publish the new version in the internal artifactory. 
- Go to PackageWorks and trigger the promotio, this will create the branches to update the editor manifest in the different supported LTSs
- Once the PRs have landed we need to manually update the local-test-references and create a promotion PR in the [RM-PackagePromotion](https://github.cds.internal.unity3d.com/unity/rm-package-promotion)
- Once the package is promoted we can merge `next/master-3.0` into `master-3.0` 