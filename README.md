Q: Once the repo is cloned, how to:
1. generate csproj, sln?
2. test?
3. release a new version?

A:
1. So you need the code inside a unity project, inside the package folder. The Unity version running should be 2019.2.0a12 or above. Otherwise it won’t contain the public APIs or interfaces that this package is utilizing. (Also it is not in trunk)
2. Testing can be done either by using the testproject that is in the repo. Or by constructing your own test project.
3. Release a new version is done by the release page on this github page. You can look at the specifics inside the .yml files. But basically, when a new Release is created in the CI gets triggered and takes whatever is in the current master branch of the repo, and package it and sends it to a “staging” repo for packages. Which you can also target by modifying the `manifest.json` in a Unity Project.
From this point it is “public” in the sense that users could potentially target this one, but it won’t be “verified” (which I can get more into).
