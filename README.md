1. To use this package, you need a Unity Version 2019.2.0a12+. Use [this instruction](https://github.com/JetBrains/resharper-unity/issues/1208#issuecomment-502211659) to substitute current version of package in your project with this one.

2. How to test the package?

The root of this folder is a Unity project, so opening this folder with Unity will load everything up. The package code is located in the Packages folder, where it can be modified freely. When opening Unity, with this root folder, testing can be done through the test runner. If needed the packages can also be moved to another project, the test project is not necessary in order to run the tests. But the testproject depend on the packages inside of the Packages folder.

3. When new version is released? 

Release is done through the release page on this github page. It packages the packages and publish these to a "staging" repo. Conflicts can happen if the same version is already there. So in order to get a new version of either the package or the test package, one needs to increase the number in the package.json files inside the package folders respectively.
For more information about how this works, one can look at the specifics inside the .yml files. The general idea is when a new Release is created, the CI gets triggered and takes whatever is in the current master branch of the repo, packages it, and sends it to this "staging" repo for packages. Which you can also target by modifying the `manifest.json` in a Unity Project.
From this point it is “public” in the sense that users could potentially target this one, but it won’t be “verified” (ask for more information).
