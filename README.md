# WispStudio.Docker.ContainerPatcher

This app is designed for patching docker container images. It works by backing up the target container, 
moving the input files or directory files to the target containers path, and then saving the updated data
to the container image. This allows us to manipulate the container and save state, with the ability to roll
back when necessary. 

I designed this because of the difficulty of updating and debugging docker containers on remote machines.