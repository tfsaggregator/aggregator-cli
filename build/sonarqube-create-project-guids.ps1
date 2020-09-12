# Project Guid Generator
# Creator: Unknown
# Editor: Isaac martinez
# Notes: A requirement of SonarCloud/SonarQube is that all projects have a unique
#   ProjectGuid.  This script takes care of that.  This script will recursively
#   look for projects in subfolders and apply the necessary changes.
# Version Date: 05/19/2020

$paths = Get-ChildItem -Path $PSScriptRoot -include *proj -Recurse
Write-Host $paths.count "project(s) found"
$projectGuids = @()
foreach ($pathobject in $paths) {
    $path = $pathobject.fullname
    $doc = New-Object System.Xml.XmlDocument
    $doc.Load($path)
    $node = $doc.SelectSingleNode("//Project/PropertyGroup/ProjectGuid")
    if (!$node) {
        $child = $doc.CreateElement("ProjectGuid")
        $child.InnerText = [guid]::NewGuid().ToString().ToUpper()
        $node = $doc.SelectSingleNode("//Project/PropertyGroup")
        if (!$node){        
            Write-Host "Error:" $path "- Project file is missing //Project/PropertyGroup node"
        }
        else {
            $node.AppendChild($child)
            $doc.Save($path)
            $projectGuids += $child.InnerText.ToUpper()
        }
    }
    elseif($projectGuids.Contains($node.InnerText)){
        Write-Host "Duplicate found"
        $node.InnerText = [guid]::NewGuid().ToString().ToUpper()
        $doc.Save($path)        
        $projectGuids += $node.InnerText.ToUpper();
    }
    else{
        $projectGuids += $node.InnerText;
        Write-Host $path "already has a valid ProjectGuid"
    }
}
