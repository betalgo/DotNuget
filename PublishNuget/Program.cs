using PublishNuget;

var nugetData = new NugetDataPackage(
    Environment.GetEnvironmentVariable("INPUT_PROJECT_FILE_PATH")!,
    Environment.GetEnvironmentVariable("INPUT_PACKAGE_NAME") ?? Environment.GetEnvironmentVariable("PACKAGE_NAME")!,
    Environment.GetEnvironmentVariable("INPUT_VERSION_FILE_PATH") ?? Environment.GetEnvironmentVariable("VERSION_FILE_PATH") ?? Environment.GetEnvironmentVariable("INPUT_PROJECT_FILE_PATH")!,
    Environment.GetEnvironmentVariable("INPUT_VERSION_STATIC") ?? Environment.GetEnvironmentVariable("VERSION_STATIC")!,
    tagCommit: bool.Parse(Environment.GetEnvironmentVariable("INPUT_TAG_COMMIT") ?? Environment.GetEnvironmentVariable("TAG_COMMIT")!),
    tagFormat: (Environment.GetEnvironmentVariable("INPUT_TAG_FORMAT") ?? Environment.GetEnvironmentVariable("TAG_FORMAT"))!,
    nugetKey: Environment.GetEnvironmentVariable("INPUT_NUGET_KEY") ?? Environment.GetEnvironmentVariable("NUGET_KEY")!,
    nugetSource: Environment.GetEnvironmentVariable("INPUT_NUGET_SOURCE") ?? Environment.GetEnvironmentVariable("NUGET_SOURCE")!,
    includeSymbols: bool.Parse(Environment.GetEnvironmentVariable("INPUT_INCLUDE_SYMBOLS") ?? Environment.GetEnvironmentVariable("INCLUDE_SYMBOLS")!),
    noBuild: bool.Parse(Environment.GetEnvironmentVariable("INPUT_NO_BUILD") ?? Environment.GetEnvironmentVariable("NO_BUILD")!),
    certificatePath: Environment.GetEnvironmentVariable("CODESIGN_CERT_PATH"),
    certificatePassword: Environment.GetEnvironmentVariable("CODESIGN_CERT_PASSWORD"),
    versionRegexPattern: Environment.GetEnvironmentVariable("INPUT_VERSION_REGEX") ?? Environment.GetEnvironmentVariable("VERSION_REGEX")!
);

await nugetData.CheckForUpdate();