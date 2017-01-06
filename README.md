# Bulk Nuget Updater

This is a handy little [Linqpad](https://www.linqpad.net/) script that updates every existing occurrance of a nuget package in a solution to the latest version.

This came out of working in a .net solution consisting of more than 250 projects. In a solution of that size, updating a package that's used in a lot of those projects using either the Visual Studio Nuget UI or the Nuget command line tool takes a painfully long time (like 2 or 3 hours long!). I asked [this question](http://stackoverflow.com/questions/41489327/how-to-manually-update-a-nuget-package-without-using-the-nuget-ui-or-command-lin) on Stackoverflow which helped work out what would need to be done to do this update manually. Then I wrote this script because I could see trying to actually do it manually would probably end in disaster.

## Usage

* Download [Linqpad](https://www.linqpad.net/)
* If you have the solution open in Visual Studio, close it. Otherwise, it will take an excruciatingly long time trying to reload everything that changed outside the IDE.
* Change the `NugetRepo`, `PackageId` and `SolutionPath` variables at the top of the script as necessary
* Run the script
* Open the solution in Visual Studio and build it