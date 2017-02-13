<Query Kind="Program">
  <NuGetReference>Microsoft.Build</NuGetReference>
  <NuGetReference>NuGet.Core</NuGetReference>
  <Namespace>Microsoft.Build.Construction</Namespace>
  <Namespace>Microsoft.Web.XmlTransform</Namespace>
  <Namespace>NuGet</Namespace>
  <Namespace>NuGet.Resources</Namespace>
  <Namespace>NuGet.Runtime</Namespace>
</Query>

const string NugetRepo = "https://nuget.planetos.com/";

// The package you want to update
const string PackageId = "KingInTheNorth";

// The location of the solution you want to update
const string SolutionPath = @"C:\git\sevenKingdoms\north.sln";

// Optional. The specific version of the package, e.g "1.0.0.6" or "0.0.0.216-bugfix".
const string Version = @"";

void Main()
{
	var start = DateTime.Now;

	var repo = PackageRepositoryFactory.Default.CreateRepository(NugetRepo);
	var updatePackage = string.IsNullOrWhiteSpace(Version) ? GetLatestPackage(repo) : GetVersionOfPackage(repo, Version);
	var packageVersion = updatePackage.Version.ToFullString();
	
	var assemblies = updatePackage.AssemblyReferences.Cast<PhysicalPackageAssemblyReference>().ToList();
	var assemblyVersions = assemblies.Select(a =>
	{
		var sourcePath = a.SourcePath;
		var details = System.Reflection.Assembly.LoadFrom(sourcePath).GetName();
		return new KeyValuePair<string, string>(details.Name, details.Version.ToString());
	});
	assemblyVersions.Dump();

	var solution = SolutionFile.Parse(SolutionPath);
	var projects = solution.ProjectsInOrder.Where(x => x.RelativePath.EndsWith("proj"));

	Console.WriteLine($"About to update package {PackageId} for all projects in {SolutionPath} to version {packageVersion}");

	foreach (var project in projects)
	{
		var directory = Path.GetDirectoryName(project.AbsolutePath);

		UpdatePackageVersionInConfigs(directory, PackageId, packageVersion);
		UpdateVersionsInProjFiles(directory, PackageId, packageVersion, assemblyVersions);
	}
	
	var end = DateTime.Now;
	var taken = end.Subtract(start).TotalSeconds;
	Console.WriteLine($"Finished updating in {taken} seconds.");
}

public IPackage GetLatestPackage(IPackageRepository repo)
{
	// This regex makes sure we only get the latest master branch build, even though a feature branch build could be more recent
	var masterPattern = @"(?:\d+\.)+\d+$";
	var masterRegex = new Regex(masterPattern);
	return repo.FindPackagesById(PackageId).OrderByDescending(x => x.Version).First(x => masterRegex.IsMatch(x.Version.ToString()));
}

public IPackage GetVersionOfPackage(IPackageRepository repo, string version) 
{
	return repo.FindPackagesById(PackageId).Single(x => string.Equals(x.Version.ToFullString(), version, StringComparison.CurrentCultureIgnoreCase));
}

public void UpdatePackageVersionInConfigs(string directoryPath, string packageName, string newVersion)
{
	var pattern = $@"""{packageName}"" version=""(?:\d+\.)+\d+""";
	var replacement = $@"""{packageName}"" version=""{newVersion}""";
	
	foreach (var file in Directory.EnumerateFiles(directoryPath, "*.config", SearchOption.AllDirectories))
	{
		FindAndReplace(file, pattern, replacement);
	}
}

public void UpdateVersionsInProjFiles(string directoryPath, string packageName, string newVersion, 
	IEnumerable<KeyValuePair<string, string>> assemblyVersions) 
{
	var packagePattern = $@"{packageName}\.(?:\d+\.)+\d+";
	var packageReplacement = $@"{packageName}.{newVersion}";

	foreach (var file in Directory.EnumerateFiles(directoryPath, "*.*proj", SearchOption.AllDirectories))
	{
		FindAndReplace(file, packagePattern, packageReplacement);

		foreach (var assembly in assemblyVersions)
		{
			var name = assembly.Key;
			var version = assembly.Value;

			var assemblyPattern = $@"{name}, Version=(?:\d+\.)+\d+";
			var assemblyReplacement = $@"{name}, Version={version}";

			FindAndReplace(file, assemblyPattern, assemblyReplacement);
		}
	}
}

public void FindAndReplace(string file, string findPattern, string replacement)
{
	var regex = new Regex(findPattern);
	var fileText = File.ReadAllText(file);
	var matches = regex.Matches(fileText);
	if (matches.Count == 0)
	{
		return;
	}
	var currentVersion = matches[0].ToString();

	if (currentVersion == replacement)
	{
		Console.WriteLine($"Skipping {replacement} for {file}: already up to date");
		return;
	}

	Console.Write($"Updating {file}: changing {currentVersion} to {replacement}... ");

	var newText = Regex.Replace(fileText, findPattern, replacement);
	File.WriteAllText(file, newText, Encoding.UTF8);

	Console.WriteLine("Done");
}