# WSL2 + choco
Enable-WindowsOptionalFeature -Online -FeatureName 'Microsoft-Hyper-V-All','Containers','Microsoft-Windows-Subsystem-Linux','VirtualMachinePlatform' -All
Set-ExecutionPolicy Bypass -Scope Process -Force; [System.Net.ServicePointManager]::SecurityProtocol = [System.Net.ServicePointManager]::SecurityProtocol -bor 3072; iex ((New-Object System.Net.WebClient).DownloadString('https://chocolatey.org/install.ps1'))
choco install -y wsl2 # this is the Linux Kernel update
wsl --set-default-version 2
choco install -y --ignore-dependencies wsl-ubuntu-1804 wsl-alpine
wsl --list --all -v

# dev tools
Set-ExecutionPolicy -ExecutionPolicy RemoteSigned -Scope LocalMachine
choco install -y firacode notepad3 microsoft-windows-terminal 7zip git microsoft-edge powershell-core poshgit beyondcompare git-fork docker-desktop vscode visualstudio2019community postman
Restart-Computer

#AzDOS
curl 'https://download.visualstudio.microsoft.com/download/pr/5f41da5f-d4ba-4824-99a6-5d7e417e8286/c6766b37cc7118ce149abd2f76e14af8/azuredevopsserver2020.iso' -o azuredevopsserver2020.iso
$iso = Mount-DiskImage -ImagePath (Resolve-Path "azuredevopsserver2020.iso")
$isoDrive = ($iso | Get-Volume).DriveLetter
Start-Process "${isoDrive}:\AzureDevOpsServer2020.exe" -ArgumentList '/Passive','/NoRestart' -Wait
Dismount-DiskImage -ImagePath  $iso.ImagePath
Restart-Computer

& 'C:\Program Files\Azure DevOps Server 2020\Tools\tfsconfig.exe' unattend /configure /unattendfile:basic.ini
Restart-Computer

# code
md C:\src\github.com\tfsaggregator
cd C:\src\github.com\tfsaggregator
git clone https://github.com/tfsaggregator/aggregator-cli.git
git clone https://github.com/tfsaggregator/aggregator-docs.git

choco install nodejs
npm install -g tfx-cli
