/********** ARGUMENTS **********/

var target = Argument("target", "Default");
var rootDir = Argument("root", "../../");
var cake = Argument("recipe", "recipe.yml");

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

Func<string> cakeGetBuildNumber = () =>
  {
    return isAPPVEYOR ? EnvironmentVariable("APPVEYOR_BUILD_NUMBER") : EnvironmentVariable(EnvKeys.CakeBuildVersion);
  };

Func<string> cakeGetBuildVersion = () =>
  {
    return isAPPVEYOR ? EnvironmentVariable("APPVEYOR_BUILD_VERSION") : ParseReleaseNotes(cakeYamlGetReleaseNotes()).Version.ToString() + "." + cakeGetBuildNumber();
  };

/********** GLOBAL VARIABLES **********/

readonly int version = 1;
readonly bool isAPPVEYOR = (EnvironmentVariable("APPVEYOR") ?? "").ToUpper() == "TRUE";

/********** SETUP / TEARDOWN **********/

Setup(context =>
{
});

Teardown(context =>
{
});

/********** TASK TARGETS **********/

Task("Default")
  .Does(() =>
  {
      Information("Default target completed");
  });

RunTarget(target);
