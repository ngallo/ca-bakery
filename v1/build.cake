/********** ARGUMENTS **********/

readonly var target = Argument("target", "Default");
readonly var rootDir = Argument("root", "../../");
readonly var cake = Argument("recipe", "recipe.yml");

/********** TOOLS & ADDINS **********/

#addin "Cake.FileHelpers"

#addin "Cake.Yaml"

/********** TYPES **********/

public class EnvKeys
{
  public const string CakeBuildVersion = "CAKE_BUILD_VERSION";
}

public class CakeYamlBuild
{
  public string dist { get; set; }
  public string release_notes { get; set; }
}

public class CakeYamlComponent
{
  public string name { get; set; }
  public string path { get; set; }
  public CakeYamlComponentBuild build { get; set; }
}

public class CakeYamlComponentBuild
{
  public string type { get; set; }
  public string dist { get; set; }
}

public class CakeYamlBundler
{
  public string name { get; set; }
  public List<String> imports { get; set; }
  public List<CakeYamlBundlerOperationSteps> steps { get; set; }
}

public class CakeYamlBundlerOperationSteps
{
  public string operation { get; set; }
  public CakeYamlBundlerOperationTarget from { get; set; }
  public CakeYamlBundlerOperationTarget to { get; set; }
}

public class CakeYamlBundlerOperationTarget
{
  public string component { get; set; }
  public string context { get; set; }
  public string path { get; set; }
  public string extensions { get; set; }
}

public class CakeYamlArtifact
{
  public string name { get; set; }
  public string path { get; set; }
  public CakeYamlArtifactBundle bundle { get; set; }
}

public class CakeYamlArtifactBundle
{
  public string name { get; set; }
  public bool enable_compression { get; set; }
}

public class CakeYaml
{
    public int version { get; set; }
    public string name { get; set; }
    public Dictionary<string,string> environment { get; set; }
    public CakeYamlBuild build { get; set; }
    public List<CakeYamlComponent> components { get; set; }
    public List<CakeYamlBundler> bundlers { get; set; }
    public List<CakeYamlArtifact> artifacts { get; set; }
}

/********** FUNCTIONS **********/

Func<CakeYaml> cakeGetYaml = () => { return DeserializeYamlFromFile<CakeYaml>(rootDir + cake); };

Action cakeYamlValidateScript = () =>
{
  var yamlVersion = cakeGetYaml().version;
  if (yamlVersion != version) {
    throw new Exception(String.Format("The recipe version is not supported (current supported version is {0}).", version));
  }
};

Func<string> cakeGetBuildNumber = () =>
{
  return isAPPVEYOR ? EnvironmentVariable("APPVEYOR_BUILD_NUMBER") : EnvironmentVariable(EnvKeys.CakeBuildVersion);
};

Func<string> cakeGetBuildVersion = () =>
{
  return isAPPVEYOR ? EnvironmentVariable("APPVEYOR_BUILD_VERSION") : ParseReleaseNotes(cakeYamlGetReleaseNotes()).Version.ToString() + "." + cakeGetBuildNumber();
};

Func<String> cakeYamlGetReleaseNotes = () =>
{
  var file = "ReleaseNotes.md" ;
  if (cakeGetYaml().build != null)
  {
    var buildFile = cakeGetYaml().build.release_notes;
    file = String.IsNullOrWhiteSpace(buildFile) ? file : buildFile;
  }
  return rootDir + file;
};

Func<string[]> cakeYamlLoadEnvironment = () =>
{
  if(cakeGetYaml().environment == null) {
    return new String[0];
  }
  foreach(var item in cakeGetYaml().environment) {
    Environment.SetEnvironmentVariable(item.Key, item.Value);
  }
  return cakeGetYaml().environment.Keys.ToArray();
};

Action<String> BuildComponents = (String npmCommand) =>
{
  /*foreach (var component in cakeYml().components)
  {
    Information("component: " + component.name);
    if ((component.build.type ?? "").ToLower() == "npm") {
      Information("running " + npmCommand);
      StartProcess("cmd", new ProcessSettings {
        Arguments = "/c \""+ npmCommand +"\"",
        WorkingDirectory = MakeAbsolute(Directory(root + component.path))
      });
    }
    else {
      Information("invalid build type");
    }
  }*/
};

/********** GLOBAL VARIABLES **********/

readonly int version = 1;
readonly bool isAPPVEYOR = (EnvironmentVariable("APPVEYOR") ?? "").ToUpper() == "TRUE";

/********** SETUP / TEARDOWN **********/

Setup(context =>
{
    //Executed BEFORE the first task.
    // Validate the version of the recipe.yml file
    cakeYamlValidateScript();
    // Load the environment variables
    var envKeys = cakeYamlLoadEnvironment();
    Information("[ENVIRONMENT]");
    foreach(var envKey in envKeys) {
      Information("- {0}={1}", envKey, EnvironmentVariable(envKey));
    }
    // Logging of the settings
    Information("[SETUP] Build Version {0} of {1}", cakeGetBuildVersion(), cakeGetYaml().name);
    Information("[WORKING_DIRECTORY] {0}", MakeAbsolute(Directory(".")));
    Information("[ROOT_DIRECTORY] {0}", MakeAbsolute(Directory(rootDir)));
});

Teardown(context =>
{
  // Executed AFTER the last task.
  Information("[Teardown] Build Version {0} of {1}", cakeGetBuildVersion(), cakeGetYaml().name);
});

/********** TASK TARGETS **********/

Task("Clean")
  .Does(() =>
  {
    BuildComponents("npm run clean");
  });

Task("Setup")
  .Does(() =>
  {
    BuildComponents("npm install");
  });

Task("Build")
  .Does(() =>
  {
    BuildComponents("npm run build");
  });

Task("Test")
  .Does(() =>
  {
    BuildComponents("npm run test");
  });

Task("Package")
  .Does(() =>
  {
    /*foreach (var artifact in cakeYml().Artifacts)
    {
      Information("artifact: " + artifact.Name);
      if ((artifact.BundleType ?? "").ToLower() == "cmd") {
        var workingDirectory =  MakeAbsolute(Directory("."));
        Information("working directory: " + workingDirectory);
        var script = "bundle-" + artifact.Name + ".cmd";
        Information("running script: " + script);
        StartProcess("cmd", new ProcessSettings {
          Arguments = "/c \""+ script +"\"",
          WorkingDirectory = workingDirectory
        });
      }
      else {
        Information("invalid bundle type");
      }
    }*/
  });

Task("CI")
  .IsDependentOn("Clean")
  .IsDependentOn("Setup")
  .IsDependentOn("Build")
  .IsDependentOn("Test")
  .Does(() =>
  {
      Information("CI target completed");
  });

Task("RC")
  .IsDependentOn("Clean")
  .IsDependentOn("Setup")
  .IsDependentOn("Build")
  //.IsDependentOn("Test")
  .IsDependentOn("Package")
  .Does(() =>
  {
      Information("RC target completed");
  });

Task("Default")
  .Does(() =>
  {
      Information("Default target completed");
  });

RunTarget(target);
