using Docker.DotNet.Models;
using Docker.DotNet;
using System.Text;
using ICSharpCode.SharpZipLib.Tar;
using WispStudios.Docker.ContainerPatcher.Core.Options;
using WispStudios.Docker.ContainerPatcher.Core.Interfaces;
using WispStudios.Docker.ContainerPatcher.Core.Resources;

namespace WispStudios.Docker.ContainerPatcher.Core.Agents
{
    public class ContainerPatchAgent : IContainerPatchAgent
    {
        private readonly ILogger _logger;

        public ContainerPatchAgent(ILogger logger)
        {
            _logger = logger;
        }

        public async Task ExecuteAsync(ExecutionProfile profile)
        {
            var host = string.IsNullOrEmpty(profile.Host) ? "npipe://./pipe/docker_engine" : profile.Host;
            Uri endpoint;
            try
            {
                endpoint = new Uri(host);
            }
            catch (UriFormatException)
            {
                _logger.LogError(Errors.ContainerPatchAgent_ExecuteAsync_HostIsNotValidURIError, host);
                return;
            }

            if (string.IsNullOrWhiteSpace(profile.Input))
            {  
                _logger.LogWarn(Errors.InputNullOrEmpty, profile);
                return;
            }

            if (string.IsNullOrWhiteSpace(profile.Output))
            {
                _logger.LogWarn(Errors.OutputNullOrEmpty, profile);
                return;
            }

            if (string.IsNullOrWhiteSpace(profile.Target))
            {
                _logger.LogWarn(Errors.TargetNullOrEmpty, profile);
                return;
            }

            try
            {
                using var client = new DockerClientConfiguration(
                        endpoint)
                    .CreateClient();

                _logger.LogInfo(Strings.ContainerPatchAgent_ExecuteAsync_ConnectedMessage, profile.Target);

                var containers = await client.Containers.ListContainersAsync(
                    new ContainersListParameters { All = true });

                var container = containers.FirstOrDefault(c =>
                    c.Names.Contains($"/{profile.Target}") || c.ID.StartsWith(profile.Target));

                if (container == null)
                {
                    _logger.LogError(Errors.ContainerPatchAgent_ExecuteAsync_ContainerNotFoundError, profile.Target);
                    return;
                }

                var containerInfo = await client.Containers.InspectContainerAsync(container.ID);
                var imageName = containerInfo.Config.Image;
                var repository = imageName.Contains(":") ? imageName.Split(':')[0] : imageName;
                var currentTag = imageName.Contains(":") ? imageName.Split(':')[1] : "latest";

                _logger.LogInfo(Strings.ContainerPatchAgent_ExecuteAsync_FoundContainerMessage, container.ID, string.Join(", ", container.Names));
                _logger.LogInfo(Strings.ContainerPatchAgent_ExecuteAsync_CurrentImageMessage, imageName);

                if (!string.IsNullOrEmpty(profile.ReplaceTag))
                {
                    await ReplaceMode(client, profile, container, containerInfo, repository, currentTag);
                }
                else if (!string.IsNullOrEmpty(profile.RestoreTag))
                {
                    await RestoreMode(client, profile, container, containerInfo, repository);
                }
                else
                {
                    _logger.LogError(Errors.ContainerPatchAgent_ExecuteAsync_ReplaceTagOrRestoreTagUnspecifiedError);
                }

                _logger.LogInfo(Strings.ContainerPatchAgent_ExecuteAsync_OperationCompletedSuccessfully);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(Errors.ContainerPatchAgent_ExecuteAsync_HttpRequestException, endpoint, ex.Message);
            }
            catch (DockerApiException ex)
            {
                _logger.LogError(Errors.ContainerPatchAgent_ExecuteAsync_DockerApiException, ex.Message, ex.StatusCode);
            }
            catch (Exception ex)
            {
                _logger.LogError(Errors.ContainerPatchAgent_ExecuteAsync_GeneralException, ex.Message);
            }
        }
          
        private async Task ReplaceMode(IDockerClient client, ExecutionProfile opts, ContainerListResponse container,
            ContainerInspectResponse containerInfo, string repository, string currentTag)
        {
            _logger.LogInfo(Strings.ContainerPatchAgent_ReplaceMode_RunningReplaceModeMessage, opts.ReplaceTag!);

            if (string.IsNullOrEmpty(opts.Input) || string.IsNullOrEmpty(opts.Output))
            {
                _logger.LogError(Errors.ContainerPatchAgent_ReplaceMode_InputOutputRequiredError);
                return;
            }

            if (container.State == "running")
            {
                _logger.LogInfo(Strings.ContainerPatchAgent_ReplaceMode_StoppingContainerMessage);
                await client.Containers.StopContainerAsync(container.ID, new ContainerStopParameters());
                _logger.LogInfo(Strings.ContainerPatchAgent_ReplaceMode_ContainerStoppedMessage);
            }

            _logger.LogInfo(Strings.ContainerPatchAgent_ReplaceMode_CreatingBackupMessage, repository, opts.ReplaceTag!);
            await client.Images.TagImageAsync(
                containerInfo.Image,
                new ImageTagParameters
                {
                    RepositoryName = repository,
                    Tag = opts.ReplaceTag
                });

            _logger.LogInfo(Strings.ContainerPatchAgent_ReplaceMode_BackupCreatedMessage);

            var inputPaths = ParseInputPaths(opts.Input);
            if (!inputPaths.Any())
            {
                _logger.LogError(Errors.ContainerPatchAgent_ReplaceMode_NoValidInputPathsFoundError);
                return;
            }

            _logger.LogInfo(Strings.ContainerPatchAgent_ReplaceMode_ParsedInputPathsMessage, inputPaths.Count);

            await CopyFilesToContainer(client, container.ID, inputPaths, opts.Output);

            _logger.LogInfo(Strings.ContainerPatchAgent_ReplaceMode_CommittingChangesMessage, repository, currentTag);
            await client.Images.CommitContainerChangesAsync(new CommitContainerChangesParameters
            {
                ContainerID = container.ID,
                Tag = currentTag,
                RepositoryName = repository,
                Comment = $"Modified by WispStudios.Docker.ContainerPatcher at {DateTime.Now}"
            });

            _logger.LogInfo(Strings.ContainerPatchAgent_ReplaceMode_ContainerChangesCommittedMessage, repository, currentTag);

            if (container.State == "running")
            {
                _logger.LogInfo(Strings.ContainerPatchAgent_ReplaceMode_RestartingContainerMessage);
                await client.Containers.StartContainerAsync(container.ID, new ContainerStartParameters());
                _logger.LogInfo(Strings.ContainerPatchAgent_ReplaceMode_ContainerRestartedMessage);
            }
        }

        private async Task RestoreMode(IDockerClient client, ExecutionProfile opts, ContainerListResponse container,
            ContainerInspectResponse containerInfo, string repository)
        {
            _logger.LogInfo(Strings.ContainerPatchAgent_RestoreMode_RunningInRestoreModeMessage, opts.RestoreTag!);

            if (container.State == "running")
            {
                _logger.LogInfo(Strings.ContainerPatchAgent_ReplaceMode_StoppingContainerMessage);
                await client.Containers.StopContainerAsync(container.ID, new ContainerStopParameters());
                _logger.LogInfo(Strings.ContainerPatchAgent_ReplaceMode_ContainerStoppedMessage);
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
                _logger.LogError(Errors.ContainerPatchAgent_RestoreMode_BackupImageNotFoundError, repository, opts.RestoreTag!);
                return;
            }

            var config = containerInfo.Config;

            _logger.LogInfo(Strings.ContainerPatchAgent_RestoreMode_CreatingNewContainerFromBackupMessage, repository, opts.RestoreTag!);

            _logger.LogInfo(Strings.ContainerPatchAgent_RestoreMode_RemovingCurrentContainerMessage);
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

            _logger.LogInfo(Strings.ContainerPatchAgent_RestoreMode_NewContainerCreatedMessage, response.ID);

            if (container.State == "running")
            {
                _logger.LogInfo(Strings.ContainerPatchAgent_RestoreMode_StartingRestoredContainerMessage);
                await client.Containers.StartContainerAsync(response.ID, new ContainerStartParameters());
                _logger.LogInfo(Strings.ContainerPatchAgent_RestoreMode_ContainerStartedMessage);
            }

            _logger.LogInfo(Strings.ContainerPatchAgent_RestoreMode_ContainerSuccessfullyRestoredFromBackupMessage);
        }

        private List<string> ParseInputPaths(string input)
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
                    _logger.LogError(Errors.ContainerPatchAgent_ParseInputPaths_WarningPathNotFound, path);
                }
            }

            return result;
        }

        private async Task CopyFilesToContainer(IDockerClient client, string containerId,
            List<string> sourcePaths, string targetPath)
        {
            _logger.LogInfo(Strings.ContainerPatchAgent_CopyFilesToContainer_CopyingFilesToContainerAtPathMessage, sourcePaths.Count, targetPath);

            var tempTarPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName() + ".tar");
            try
            {
                await using (var tarOutputStream = new TarOutputStream(File.Create(tempTarPath), Encoding.Unicode))
                {
                    foreach (var sourcePath in sourcePaths)
                    { 
                        var relativePath = Path.GetFileName(sourcePath);

                        var entry = TarEntry.CreateEntryFromFile(sourcePath);
                        entry.Name = relativePath;

                        tarOutputStream.PutNextEntry(entry);

                        await using (var fileStream = File.OpenRead(sourcePath))
                        {
                            await fileStream.CopyToAsync(tarOutputStream);
                        }

                        tarOutputStream.CloseEntry();

                        _logger.LogInfo(Strings.ContainerPatchAgent_CopyFilesToContainer_AddedToTarMessage, sourcePath, relativePath);
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

                _logger.LogInfo(Strings.ContainerPatchAgent_CopyFilesToContainer_FilesCopiedSuccessfullyMessage);
            }
            finally
            {
                if (File.Exists(tempTarPath))
                {
                    File.Delete(tempTarPath);
                    _logger.LogInfo(Strings.ContainerPatchAgent_CopyFilesToContainer_TemporaryTarFileDeletedMessage);
                }
            }
        }
    }
}
