using System.Reflection;
using System.Text;
using CommandLine;
using CommandLine.Text;
using Docker.DotNet;
using Docker.DotNet.Models;
using ICSharpCode.SharpZipLib.Tar;
using Newtonsoft.Json;

namespace WispStudios.Docker.ContainerPatcher
{
    internal class Program
    {
        public class Options
        {
            [Option('i', "input", Required = false, HelpText = "CSV list of files or directories to transfer")]
            public string Input { get; set; }

            [Option('o', "output", Required = false, HelpText = "Target path in the container")]
            public string Output { get; set; }

            [Option('t', "target", Required = true, HelpText = "Target container name or ID")]
            public string Target { get; set; }


            [Option('h', "host", Required = true, HelpText = "Target host address")]
            public string Host { get; set; }

            [Option("replace-tag", Required = false, HelpText = "Backup tag to save the original container state")] 
            public string ReplaceTag { get; set; }

            [Option("restore-tag", Required = false, HelpText = "Tag to restore (reverting to the backup version)")]
            public string RestoreTag { get; set; }

            [Option("save-profile", Required = false, HelpText = "Save the parameters passed as a profile for future execution.")]
            public string SaveProfile { get; set; }

            [Option("load-profile", Required = false, HelpText = "Load an existing profile file and execute it.")]
            public string ExecuteProfile { get; set; }

            [Option("list-profiles", Required = false, HelpText = "List profiles that currently exist.")]
            public bool? ListProfiles { get; set; }

            [Usage(ApplicationAlias = "WispStudios.Docker.ContainerPatcher")]
            public static IEnumerable<Example> Examples =>
                new List<Example>()
                {
                    new("Backup and modify a container",
                        new Options
                        {
                            Input = "C:\\path\\to\\file.txt,C:\\another\\directory",
                            Output = "/app/data",
                            Target = "my-container",
                            Host = "tcp://my-host:port",
                            ReplaceTag = "backup-20250426"
                        }),
                    new("Restore a container from backup",
                        new Options
                        {
                            Target = "my-container",
                            Host = "tcp://my-host:port",
                            RestoreTag = "backup-20250426"
                        })
                };
        }

        private static async Task Main(string[] args)
        {
            await Parser.Default.ParseArguments<Options>(args)
                .WithParsedAsync(async opts => await RunWithOptions(opts));
        }
        private const string ProfilesDirectory = "./profiles";
        private static async Task RunWithOptions(Options opts)
        {
            if (TryGetProfilesDirectory(out var profilesDir))
            {
                if (string.IsNullOrEmpty(profilesDir))
                {
                    Console.WriteLine("Access to profiles directory failed.");
                    return;
                }

                if (opts.ListProfiles == true)
                {
                    var files = Directory.GetFiles(profilesDir, "*.json", SearchOption.TopDirectoryOnly);

                    if (files.Length == 0)
                    {
                        Console.WriteLine("No profiles found.");
                        return;
                    }

                    var fileNames = files.Select(Path.GetFileNameWithoutExtension).OrderBy(t => t).ToList();
                    Console.WriteLine("Current Profiles:");
                    foreach (var file in fileNames)
                    {
                        Console.WriteLine(file);
                    }

                    return;
                }

                var optsSaveProfile = opts.SaveProfile;
                if (!string.IsNullOrEmpty(optsSaveProfile))
                {
                    opts.SaveProfile = string.Empty;
                    await File.WriteAllTextAsync(Path.Combine(profilesDir, "./" + optsSaveProfile + ".json"),
                        JsonConvert.SerializeObject(opts, Formatting.Indented,
                            new JsonSerializerSettings()
                            {
                                NullValueHandling = NullValueHandling.Ignore, TypeNameHandling = TypeNameHandling.All
                            }));
                }

                if (!string.IsNullOrEmpty(opts.ExecuteProfile))
                {
                    var execProfilePath = Path.Combine(profilesDir, "./" + optsSaveProfile + ".json");
                    if (!File.Exists(execProfilePath))
                    {
                        Console.WriteLine($"The profile '{opts.ExecuteProfile}' does not exist.");
                        return;
                    }

                    try
                    {
                        var contents = await File.ReadAllTextAsync(execProfilePath);
                        var optsProfile = JsonConvert.DeserializeObject<Options>(contents);
                        if (optsProfile != null)
                        {
                            opts = optsProfile;
                        }
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine($"There was a problem loading profile '{opts.ExecuteProfile}'.");
                        return;
                    }
                }
            }
             
            var client = new DockerClientConfiguration(
                    new Uri(string.IsNullOrEmpty(opts.Host) ? "npipe://./pipe/docker_engine" : opts.Host))
                .CreateClient();

            Console.WriteLine($"Connected to Docker. Target container: {opts.Target}");

            try
            {
                var containers = await client.Containers.ListContainersAsync(
                    new ContainersListParameters { All = true });

                var container = containers.FirstOrDefault(c =>
                    c.Names.Contains($"/{opts.Target}") || c.ID.StartsWith(opts.Target));

                if (container == null)
                {
                    Console.WriteLine($"Error: Container '{opts.Target}' not found.");
                    return;
                }

                var containerInfo = await client.Containers.InspectContainerAsync(container.ID);
                var imageName = containerInfo.Config.Image;
                var repository = imageName.Contains(":") ? imageName.Split(':')[0] : imageName;
                var currentTag = imageName.Contains(":") ? imageName.Split(':')[1] : "latest";

                Console.WriteLine($"Found container: {container.ID} ({string.Join(", ", container.Names)})");
                Console.WriteLine($"Current image: {imageName}");

                if (!string.IsNullOrEmpty(opts.ReplaceTag))
                {
                    await ReplaceMode(client, opts, container, containerInfo, repository, currentTag);
                }
                else if (!string.IsNullOrEmpty(opts.RestoreTag))
                {
                    await RestoreMode(client, opts, container, containerInfo, repository);
                }
                else
                {
                    Console.WriteLine("Error: Either --replace-tag or --restore-tag must be specified.");
                }

                Console.WriteLine("Operation completed successfully");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
            }
        }

        private static bool TryGetProfilesDirectory(out string? profilesDir)
        {
            try
            {
                var executableDir = Path.GetDirectoryName(
                    Assembly.GetExecutingAssembly().Location);

                profilesDir = Path.Combine(executableDir ?? AppDomain.CurrentDomain.BaseDirectory, ProfilesDirectory);

                Directory.CreateDirectory(profilesDir);
                return true;
            }
            catch (Exception e)
            {
                profilesDir = null;
                return false;
            }
        }

        private static async Task ReplaceMode(IDockerClient client, Options opts, ContainerListResponse container,
            ContainerInspectResponse containerInfo, string repository, string currentTag)
        {
            Console.WriteLine($"Running in REPLACE mode with backup tag: {opts.ReplaceTag}");

            if (string.IsNullOrEmpty(opts.Input) || string.IsNullOrEmpty(opts.Output))
            {
                Console.WriteLine("Error: --input and --output are required in replace mode");
                return;
            }

            if (container.State == "running")
            {
                Console.WriteLine("Stopping container...");
                await client.Containers.StopContainerAsync(container.ID, new ContainerStopParameters());
                Console.WriteLine("Container stopped");
            }

            Console.WriteLine($"Creating backup image {repository}:{opts.ReplaceTag}");
            await client.Images.TagImageAsync(
                containerInfo.Image,
                new ImageTagParameters
                {
                    RepositoryName = repository,
                    Tag = opts.ReplaceTag
                });

            Console.WriteLine("Backup image created");

            var inputPaths = ParseInputPaths(opts.Input);
            if (!inputPaths.Any())
            {
                Console.WriteLine("Error: No valid input paths found.");
                return;
            }

            Console.WriteLine($"Parsed {inputPaths.Count} input paths");

            await CopyFilesToContainer(client, container.ID, inputPaths, opts.Output);

            Console.WriteLine($"Committing changes to {repository}:{currentTag}");
            await client.Images.CommitContainerChangesAsync(new CommitContainerChangesParameters
            {
                ContainerID = container.ID,
                Tag = currentTag,
                RepositoryName = repository,
                Comment = $"Modified by WispStudios.Docker.ContainerPatcher at {DateTime.Now}"
            });

            Console.WriteLine($"Container changes committed to {repository}:{currentTag}");

            if (container.State == "running")
            {
                Console.WriteLine("Restarting container...");
                await client.Containers.StartContainerAsync(container.ID, new ContainerStartParameters());
                Console.WriteLine("Container restarted");
            }
        }

        private static async Task RestoreMode(IDockerClient client, Options opts, ContainerListResponse container,
            ContainerInspectResponse containerInfo, string repository)
        {
            Console.WriteLine($"Running in RESTORE mode with tag: {opts.RestoreTag}");

            if (container.State == "running")
            {
                Console.WriteLine("Stopping container...");
                await client.Containers.StopContainerAsync(container.ID, new ContainerStopParameters());
                Console.WriteLine("Container stopped");
            }

            var images = await client.Images.ListImagesAsync(new ImagesListParameters
            {
                Filters = new Dictionary<string, IDictionary<string, bool>>
                {
                    {
                        "reference",
                        new Dictionary<string, bool>
                        {
                            { $"{repository}:{opts.RestoreTag}", true }
                        }
                    }
                }
            });

            if (!images.Any())
            {
                Console.WriteLine($"Error: Backup image {repository}:{opts.RestoreTag} not found.");
                return;
            }

            var config = containerInfo.Config;

            Console.WriteLine($"Creating new container from backup image {repository}:{opts.RestoreTag}");

            Console.WriteLine("Removing current container...");
            await client.Containers.RemoveContainerAsync(container.ID, new ContainerRemoveParameters
            {
                Force = true
            });

            var createParams = new CreateContainerParameters
            {
                Image = $"{repository}:{opts.RestoreTag}",
                Name = container.Names.First().TrimStart('/'),
                Hostname = config.Hostname,
                ExposedPorts = config.ExposedPorts,
                Env = config.Env,
                Cmd = config.Cmd,
                Entrypoint = config.Entrypoint,
                WorkingDir = config.WorkingDir,
                Labels = config.Labels,
                HostConfig = new HostConfig
                {
                    Binds = containerInfo.HostConfig.Binds,
                    PortBindings = containerInfo.HostConfig.PortBindings,
                    RestartPolicy = containerInfo.HostConfig.RestartPolicy,
                    VolumeDriver = containerInfo.HostConfig.VolumeDriver,
                    VolumesFrom = containerInfo.HostConfig.VolumesFrom,
                    NetworkMode = containerInfo.HostConfig.NetworkMode
                }
            };

            var response = await client.Containers.CreateContainerAsync(createParams);

            Console.WriteLine($"New container created: {response.ID}");

            if (container.State == "running")
            {
                Console.WriteLine("Starting restored container...");
                await client.Containers.StartContainerAsync(response.ID, new ContainerStartParameters());
                Console.WriteLine("Container started");
            }

            Console.WriteLine("Container successfully restored from backup");
        }

        private static List<string> ParseInputPaths(string input)
        {
            var result = new List<string>();

            var paths = input.Split(',').Select(p => p.Trim());

            foreach (var path in paths)
            {
                if (File.Exists(path))
                {
                    result.Add(path);
                }
                else if (Directory.Exists(path))
                {
                    result.AddRange(Directory.GetFiles(path, "*", SearchOption.AllDirectories));
                }
                else
                {
                    Console.WriteLine($"Warning: Path not found: {path}");
                }
            }

            return result;
        }

        private static async Task CopyFilesToContainer(IDockerClient client, string containerId,
            List<string> sourcePaths, string targetPath)
        {
            Console.WriteLine($"Copying {sourcePaths.Count} files to container at path: {targetPath}");

            var tempTarPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName() + ".tar");
            try
            {
                await using (var tarOutputStream = new TarOutputStream(File.Create(tempTarPath), Encoding.Unicode))
                {
                    foreach (var sourcePath in sourcePaths)
                    {
                        var fileInfo = new FileInfo(sourcePath);
                        var relativePath = Path.GetFileName(sourcePath);

                        var entry = TarEntry.CreateEntryFromFile(sourcePath);
                        entry.Name = relativePath;

                        tarOutputStream.PutNextEntry(entry);

                        await using (var fileStream = File.OpenRead(sourcePath))
                        {
                            await fileStream.CopyToAsync(tarOutputStream);
                        }

                        tarOutputStream.CloseEntry();

                        Console.WriteLine($"Added to tar: {sourcePath} -> {relativePath}");
                    }
                }

                await using (var tarStream = File.OpenRead(tempTarPath))
                {
                    await client.Containers.ExtractArchiveToContainerAsync(
                        containerId,
                        new ContainerPathStatParameters
                        {
                            Path = targetPath,
                            AllowOverwriteDirWithFile = true
                        },
                        tarStream);
                }

                Console.WriteLine("Files copied successfully");
            }
            finally
            {
                if (File.Exists(tempTarPath))
                {
                    File.Delete(tempTarPath);
                    Console.WriteLine("Temporary tar file deleted");
                }
            }
        }
    }
}