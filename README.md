Q: Once the repo is cloned, how to:
1. generate csproj, sln?
2. test?
3. release a new version?

A:
1. You need a Unity Version 2019.2.0a12 or above (poke @nicklas for build). The root of this folder is a Unity project, so opening this folder with Unity will load everything up. The package code is located in the Packages folder, where it can be modified freely.
2. When opening Unity, with this root folder, testing can be done through the test runner. If needed the packages can also be moved to another project, the test project is not necessary in order to run the tests. But the testproject depend on the packages inside of the Packages folder.
3. Release is done through the release page on this github page. It packages the packages and publish these to a "staging" repo. Conflicts can happen if the same version is already there. So in order to get a new version of either the package or the test package, one needs to increase the number in the package.json files inside the package folders respectively.
For more information about how this works, one can look at the specifics inside the .yml files. The general idea is when a new Release is created, the CI gets triggered and takes whatever is in the current master branch of the repo, packages it, and sends it to this "staging" repo for packages. Which you can also target by modifying the `manifest.json` in a Unity Project.
From this point it is “public” in the sense that users could potentially target this one, but it won’t be “verified” (ask for more information).
