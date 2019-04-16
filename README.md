Q: Once the repo is cloned, how to:
1. generate csproj, sln?
2. test?
3. release a new version?

A:
1. You need a Unity Version 2019.2.0a12 or above. The root of this folder is a Unity project, so opening this folder with Unity will load everything up. The package code is located in the Packages folder, where it can be modified freely.
2. When opening Unity, with this root folder, testing can be done through the test runner. If needed the packages can also be moved to another project, the test project is not necessary in order to run the tests. But the testproject depend on the packages inside of the Packages folder.
3. Release a new version is done by the release page on this github page. You can look at the specifics inside the .yml files. But basically, when a new Release is created in the CI gets triggered and takes whatever is in the current master branch of the repo, and package it and sends it to a “staging” repo for packages. Which you can also target by modifying the `manifest.json` in a Unity Project.
From this point it is “public” in the sense that users could potentially target this one, but it won’t be “verified” (which I can get more into).
