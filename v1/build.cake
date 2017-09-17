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

  public const string AppVeyor = "APPVEYOR";
  public const string AppVeyorBuildNumber = "APPVEYOR_BUILD_NUMBER";
  public const string AppVeyorBuildVersion = "APPVEYOR_BUILD_VERSION";
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
  return isAPPVEYOR ? EnvironmentVariable(EnvKeys.AppVeyorBuildNumber) : EnvironmentVariable(EnvKeys.CakeBuildVersion);
};

Func<string> cakeGetBuildVersion = () =>
{
  return isAPPVEYOR ? EnvironmentVariable(EnvKeys.AppVeyorBuildVersion) : ParseReleaseNotes(cakeYamlGetReleaseNotes()).Version.ToString() + "." + cakeGetBuildNumber();
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
  foreach (var component in cakeGetYaml().components)
  {
    Information("component: " + component.name);
    if ((component.build.type ?? "").ToLower() == "npm") {
      Information("running " + npmCommand);
      StartProcess("cmd", new ProcessSettings {
        Arguments = "/c \""+ npmCommand +"\"",
        WorkingDirectory = MakeAbsolute(Directory(rootDir + component.path))
      });
    }
    else {
      Information("invalid build type");
    }
  }
};

/********** GLOBAL VARIABLES **********/

readonly int version = 1;

readonly bool isAPPVEYOR = (EnvironmentVariable(EnvKeys.AppVeyor) ?? "").ToUpper() == "TRUE";

readonly DirectoryPath workingDirPath = MakeAbsolute(Directory("."));
readonly DirectoryPath rootDirPath = MakeAbsolute(Directory(rootDir));
readonly string distDir = rootDir + cakeGetYaml().build.dist;
readonly DirectoryPath distDirPath = MakeAbsolute(Directory(distDir));

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
    Information("[WORKING_DIRECTORY] {0}", workingDirPath);
    Information("[ROOT_DIRECTORY] {0}", rootDirPath);
    Information("[DIST_DIRECTORY] {0}", distDirPath);
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
    Information("Cleaning the dist directory");
    if (DirectoryExists(distDir)) {
      DeleteDirectory(distDir, new DeleteDirectorySettings {
        Recursive = true,
        Force = true
      });
    }
    BuildComponents("npm run clean");
  })
  .ReportError(exception =>
  {
    Error("Clean Error: "+ exception.Message);
  });

Task("Setup")
  .Does(() =>
  {
    BuildComponents("npm install");
  })
  .ReportError(exception =>
  {
    Error("Setup Error: "+ exception.Message);
  });

Task("Build")
  .Does(() =>
  {
    BuildComponents("npm run build");
  })
  .ReportError(exception =>
  {
    Error("Build Error: "+ exception.Message);
  });

Task("Test")
  .Does(() =>
  {
    BuildComponents("npm run test");
  })
  .ReportError(exception =>
  {
    Error("Test Error: "+ exception.Message);
  });

Task("Package")
  .Does(() =>
  {
    foreach (var artifact in cakeGetYaml().artifacts)
    {
      Information("creating artifact " + artifact.name);
      var artifactDir = distDir + "/" + artifact.path;
      var artifactDirPath = MakeAbsolute(Directory(artifactDir));
      CreateDirectory(artifactDirPath);
      if (artifact.bundle == null || String.IsNullOrWhiteSpace(artifact.bundle.name))
      {
        Error("invalid bundle!");
      }
      else
      {
        var bundler = cakeGetYaml().bundlers.FirstOrDefault(m => m.name == artifact.bundle.name);
        if (bundler.steps == null || bundler.steps.Count() == 0)
        {
          Information("there are no steps to be executed for the bundler " + bundler.name);
        }
        else
        {
          foreach(var step in bundler.steps)
          {
            var operation = step.operation;
            if(operation != "copy")
            {
              Error("unkown operation " + operation);
              continue;
            }
            var componentName = step.from.component;
            var component = cakeGetYaml().components.FirstOrDefault(m => m.name == componentName);
            if (component == null)
            {
              Error("unkown component " + componentName);
              continue;
            }
            var fromPath = rootDir + component.path;
            if (step.from.context == "dist")
            {
              fromPath += "/" + component.build.dist;
            }
            if (!String.IsNullOrWhiteSpace(step.from.path))
            {
              fromPath += "/" + step.from.path;
            }
            var toPath = artifactDir;
            if (!String.IsNullOrWhiteSpace(step.to.path)) {
              toPath += "/" + step.to.path;
            }

            if (!String.IsNullOrWhiteSpace(step.from.extensions))
            {
              fromPath += "/**/" + step.from.extensions;
              Information("copy files " + fromPath + " to " + toPath);
              CopyFiles(GetFiles(fromPath), toPath);
            }
            else
            {
              Information("copy from " + fromPath + " to " + toPath);
              CopyDirectory(fromPath, toPath);
            }
          }
        }
        if (artifact.bundle.enable_compression) {
          var zipFile = artifactDir + ".zip";
          Zip(artifactDir, zipFile);
          Information("Creating SHA256 hash for {0} artifact", zipFile);
          var zipFileHashPath = MakeAbsolute(File(zipFile + ".hash"));
          FileWriteText(zipFileHashPath, CalculateFileHash(zipFile).ToHex());
        }
      }
    }
  })
  .ReportError(exception =>
  {
    Error("Package Error: "+ exception.Message);
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
