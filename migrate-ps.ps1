# Define the path to the folder where you want to search for .csproj files
$folderPath = "C:\Personal\Projects\ChoETL-master-old"

# Get all .csproj files in the specified folder and its subfolders
$csprojFiles = Get-ChildItem -Path $folderPath -Recurse -Filter *.csproj

# Define a hashtable for package reference replacements
$packageMappings = @{
'System.Data'                            = 'System.Data.Common'
'System.Data.SqlClient'                  = 'Microsoft.Data.SqlClient'
'System.Configuration'                   = 'Microsoft.Extensions.Configuration'
'System.CodeDom' = 'System.CodeDom'
'EntityFramework' = 'Microsoft.EntityFrameworkCore'
'System.Data.SQLite'                   = 'Microsoft.Data.Sqlite'
'System.Data.SQLite.Core'                   = 'Microsoft.Data.Sqlite'
'System.ServiceModel'                   = 'System.ServiceModel.Primitives'
'System.Web'                             = 'Microsoft.AspNetCore.*'
'System.Web.Mvc'                         = 'Microsoft.AspNetCore.Mvc'
'System.Web.Http'                        = 'Microsoft.AspNetCore.Mvc'
'System.Web.WebPages'                    = 'Microsoft.AspNetCore.Mvc.Razor'
'Microsoft.AspNet.WebApi.Client'         = 'Microsoft.AspNetCore.Http'
'Microsoft.AspNet.WebApi.Core'           = 'Microsoft.AspNetCore.Mvc'
'Microsoft.AspNet.WebApi.OData'          = 'Microsoft.AspNetCore.OData'
'Microsoft.Owin'                        = 'Microsoft.AspNetCore.Authentication'
'Microsoft.Owin.Security'                = 'Microsoft.AspNetCore.Authentication'
'System.Threading.Tasks.Dataflow'        = 'System.Threading.Channels'
'Microsoft.Practices.EnterpriseLibrary.Data' = 'Dapper or Entity Framework Core'
'Microsoft.EnterpriseLibrary.TransientFaultHandling' = 'Polly'
'System.Web.Razor'                      = 'Microsoft.AspNetCore.Razor'
'System.Web.Helpers'                    = 'Microsoft.AspNetCore.WebUtilities'
'System.Runtime.Serialization'           = 'System.Text.Json or Newtonsoft.Json'
'System.Xml.Serialization'               = 'System.Xml'
'System.Drawing'                        = 'System.Drawing.Common'
'System.Net.Http.Formatting'             = 'Microsoft.AspNetCore.Mvc.Formatters.Json'
}


# Loop through each .csproj file and execute a process
foreach ($csprojFile in $csprojFiles) {
    if(-not $csprojFile.FullName.Contains("SourceGenerator.csproj")) {
    
    # Execute the process for the .csproj file (e.g., dotnet build)
    # Replace the following command with your desired process
    & upgrade-assistant upgrade $csprojFile.FullName --targetFramework net7.0 --operation Inplace --non-interactive

    # Replace package references based on the mappings

    [xml]$xml = Get-Content -Path $csprojFile.FullName


    $itemGroup = $xml.CreateElement("ItemGroup")
    $xml.Project.AppendChild($itemGroup) | Out-Null

    # Create a new <ProjectReference> element
    $projectReference = $xml.CreateElement("ProjectReference")
    $projectReference.SetAttribute("Include", "..\SourceGenerator\SourceGenerator.csproj")
    $projectReference.SetAttribute("OutputItemType", "Analyzer")

    # Append the <ProjectReference> element to the <ItemGroup>
    $itemGroup.AppendChild($projectReference) | Out-Null
    Write-Output "Successfully updated Project reference $($itemGroup)"

    $conditionsToRemove = @(
        "'Debug|AnyCPU",
        "Release|AnyCPU"
        # Add more conditions here if needed
    )

    # Find all PropertyGroup elements with conditions matching Debug or Release
    $propertyGroupsToRemove = $xml.Project.PropertyGroup | Where-Object {
        $condition = $_.Condition
        $conditionsToRemove -contains $condition
    }

    # Remove the found PropertyGroup elements
    foreach ($propertyGroup in $propertyGroupsToRemove) {
        $propertyGroup.ParentNode.RemoveChild($propertyGroup) | Out-Null
    }

    $xml.Save($csprojFile.FullName)

    # Select all <PackageReference> nodes
    $packageReferencesNodes = $xml.Project.ItemGroup.PackageReference
    $content = Get-Content -Path $csprojFile.FullName -Raw
    
    foreach ($node in $packageReferencesNodes) {
        $packageName = $node.Include
        $packageVersion = $node.Version
        Write-Output "package name $packageName found with version $packageVersion"
        if($packageName -ne $null -and $packageMappings[$packageName] -ne $null) {
            $newPackage = $packageMappings[$packageName]
            $version = "7.0.0"
            if($newPackage -ne '' -and $newPackage.Contains("Microsoft.Data.SqlClient")) { 
                $version = "5.2.2" 
            }
            if ($newPackage -ne '' -and -not $content.Contains("<PackageReference Include=`"$newPackage`" Version=`"$version`"/>")) {      
                Write-Output "Updating package reference: $packageName to $newPackage in $($csprojFile.FullName) with version $version"
                $content = $content -replace "<PackageReference Include=`"$packageName`".*?>", "<PackageReference Include=`"$newPackage`" Version=`"$version`"/>"
            }
            if ($newPackage -eq '') {
                Write-Output "package reference not found removing: $packageName to $newPackage in $($csprojFile.FullName)"
                $content = $content -replace "<PackageReference Include=`"$packageName`".*?>", ""
            }	        
        }
    }
    # Save the updated content back to the .csproj file
    $content = $content -replace "<UseWPF>true</UseWPF>", "<UseWPF>false</UseWPF>"
    $content = $content -replace "<TargetFramework>net7.0-windows</TargetFramework>", "<TargetFramework>net7.0</TargetFramework>"
    $content = $content -replace "<TargetFramework>net7.0</TargetFramework>", "<TargetFramework>net7.0</TargetFramework>`r`n<EmitCompilerGeneratedFiles>true</EmitCompilerGeneratedFiles>`r`n<EnableUnsafeBinaryFormatterSerialization>true</EnableUnsafeBinaryFormatterSerialization>`r`n<AllowUnsafeBlocks>true</AllowUnsafeBlocks>"
    Set-Content -Path $csprojFile.FullName -Value $content
    Write-Output "Successfully updated packages: $($csprojFile.FullName) with content $($content)"    
    }
}
# Define the directory containing the .cs files

# Define a mapping of old namespaces to new namespaces
$namespaceMapping = @{
"System.ComponentModel.Annotations" = "System.ComponentModel.DataAnnotations"
"System.Configuration.ConfigurationManager" = "System.Configuration.ConfigurationManager"    
"System.CodeDom" = "System.CodeDom" 
"System.Data.SQLite.Core" = "Microsoft.Data.Sqlite"
"System.Data.Entity.Core.Common" = "Microsoft.EntityFrameworkCore"
"System.Data.Entity.ModelConfiguration.Conventions" = "Microsoft.EntityFrameworkCore"
"System.Data.Entity" = "Microsoft.EntityFrameworkCore"
"System.Data.Common.Entity" = "System.Data.Common.Entity"
"System.Data.Common.Entity.Core.Common" = "System.Data.Common.Entity.Core.Common"
"System.Data.Common.SqlClient" = "System.Data.Common.SqlClient"
"System.Data.Common.SQLite" = "System.Data.Common.SQLite"
"System.Data.Common" = "System.Data.Common"
"System.Data.SQLite.EF6" = "Microsoft.EntityFrameworkCore.Sqlite"
"System.Data.SQLite" = "Microsoft.Data.Sqlite"
"System.Data.SqlClient" = "Microsoft.Data.SqlClient"
"System.Data" = "System.Data"
"NUnit" = "NUnit"
"NUnit3TestAdapter" = "NUnit3TestAdapter"
"System.Collections.Concurrent" = "System.Collections.Concurrent"
"System.Collections.Generic" = "System.Collections.Generic"
"Microsoft.Data.Sqlite.EF6" = "Microsoft.EntityFrameworkCore.Sqlite"
"System.Diagnostics.Contracts" = "System.Diagnostics"
"System.Runtime.Serialization.Formatters.Binary" = "System.Text.Json"
"Microsoft.CSharp" = "Microsoft.CSharp"
"Microsoft.CSharp.RuntimeBinder" = "Microsoft.CSharp.RuntimeBinder"
"Microsoft.VisualBasic" = "Microsoft.VisualBasic"
"System" = "System"
"System.CodeDom.Compiler" = "System.CodeDom.Compiler"
"System.Collections.ObjectModel" = "System.Collections.ObjectModel"
"System.Collections.Specialized" = "System.Collections.Specialized"
"System.Collections" = "System.Collections"
"System.ComponentModel.DataAnnotations" = "System.ComponentModel.DataAnnotations"
"System.ComponentModel.DataAnnotations.Schema" = "System.ComponentModel.DataAnnotations.Schema"
"System.ComponentModel" = "System.ComponentModel"
"System.Diagnostics" = "System.Diagnostics"
"System.Dynamic" = "System.Dynamic"
"System.Globalization" = "System.Globalization"
"System.IO" = "System.IO"
"System.IO.MemoryMappedFiles" = "System.IO.MemoryMappedFiles"
"System.Linq" = "System.Linq"
"System.Linq.Expressions" = "System.Linq.Expressions"
"System.Numerics" = "System.Numerics"
"System.Reflection" = "System.Reflection"
"System.Reflection.Emit" = "System.Reflection.Emit"
"System.Runtime.CompilerServices" = "System.Runtime.CompilerServices"
"System.Runtime.InteropServices" = "System.Runtime.InteropServices"
"System.Runtime.InteropServices.ComTypes" = "System.Runtime.InteropServices.ComTypes"
"System.Runtime.Serialization" = "System.Runtime.Serialization"
"System.Runtime.Serialization.Json" = "System.Runtime.Serialization.Json"
"System.Security" = "System.Security"
"System.Security.Cryptography" = "System.Security.Cryptography"
"System.Security.Permissions" = "System.Security.Permissions"
"System.Text" = "System.Text"
"System.Text.RegularExpressions" = "System.Text.RegularExpressions"
"System.Threading" = "System.Threading"
"System.Threading.Tasks" = "System.Threading.Tasks"
"System.Web" = "System.Web"
"System.Windows.Data" = "System.Windows.Data"
"System.Xml" = "System.Xml"
"System.Xml.Linq" = "System.Xml.Linq"
"System.Xml.Schema" = "System.Xml.Schema"
"System.Xml.Serialization" = "System.Xml.Serialization"
"System.Xml.XPath" = "System.Xml.XPath"
}

# Get all .cs files in the specified directory and its subdirectories
$csFiles = Get-ChildItem -Path $folderPath -Recurse -Filter *.cs
$usingPattern = 'using\s+([^\s;]+)'

foreach ($file in $csFiles) {
Write-Output "Processing file: $($file.FullName)"

if($file.FullName.Contains("ChoETLSQLiteConfiguration.cs")) {
Set-Content -Path $file.FullName -Value ""
}

if($file.FullName.Contains("ChoETLSQLiteDbContext.cs")) {

Set-Content -Path $file.FullName -Value 'using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.Sqlite;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace ChoETL
{
    internal class ChoETLSqliteDbContext<T> : DbContext
    {
        public Action<string> Log = Console.WriteLine;

        public ChoETLSqliteDbContext(string connectionString) { }
    }
}'
}
    
# Read the content of the .cs file
$cscontent = Get-Content -Path $file.FullName -Raw

$matches = [regex]::Matches($cscontent, $usingPattern)

$additonalPackages = @{}
    
foreach ($match in $matches) {
    # Add each using directive to the hash set
    $usingNamespace = $match.Groups[1].Value
    $newNamespace = $namespaceMapping[$usingNamespace]
    Write-Output "namespace found : $usingNamespace to $newNamespace"
    if ($newNamespace -ne $null -and $newNamespace.Trim() -ne "" ) {
        # Replace old namespaces with new namespaces
	Write-Output "namespace updated : \b$usingNamespace\b; to $newNamespace;"
        $cscontent = $cscontent -replace "\b$usingNamespace\b;", "$newNamespace;"
    } else {
        Write-Output "No direct equivalent for namespace: $usingNamespace. Skipping replacement."
    }
}
Write-Output "checking for remaining namespace references"
foreach ($match in $matches) {
    # Add each using directive to the hash set
    $usingNamespace = $match.Groups[1].Value
    $newNamespace = $namespaceMapping[$usingNamespace]
    Write-Output "remanining namespace found : $usingNamespace to $newNamespace"
    if ($newNamespace -ne $null -and $newNamespace.Trim() -ne "" ) {        
	Write-Output "remaining namespace updated : $usingNamespace to $newNamespace"
    $cscontent = $cscontent -replace "\b$usingNamespace\b", $newNamespace
    } else {
        Write-Output "No direct equivalent for namespace: $usingNamespace. Skipping replacement."
    }
}

# Save the updated content back to the .cs file
$cscontent = $cscontent -replace "NETSTANDARD2_0", "NET7_0_OR_GREATER"
$cscontent = $cscontent -replace "SQLite", "Sqlite"
$cscontent = $cscontent -replace "System.Data.SqlClient", "Microsoft.Data.SqlClient"
$cscontent = $cscontent -replace "using Microsoft.EntityFrameworkCore.Sqlite;", ""
$cscontent = $cscontent -replace "new BinaryFormatter", "new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter"
if(-not $cscontent.Contains("Contracts.Contract.Requires")){
$cscontent = $cscontent -replace "Contract.Requires", "System.Diagnostics.Contracts.Contract.Requires"}


Set-Content -Path $file.FullName -Value $cscontent
Write-Output "Namespace update completed.$($file.FullName) with content $($cscontent)"
}

Write-Output "All Namespaces are updated."

foreach ($csprojFile in $csprojFiles) {
    dotnet add $csprojFile.FullName package  "Microsoft.Data.SqlClient" --version "5.2.2"
    dotnet add $csprojFile.FullName package  "System.CodeDom" --version "8.0.0"
$content = Get-Content -Path $csprojFile.FullName -Raw
$outdatedPackages = dotnet list $csprojFile.FullName package --outdated

# Extract package names and new versions
    $outdatedPackages | ForEach-Object {
        if ($_ -match '(\S+)\s+(\S+)\s+(\S+)') {
            $packageName = $matches[1]
            $newVersion = $matches[3]
            Write-Output "Updating packages $packageName to $newVersion"
            if($content.Contains("<PackageReference Include=`"$packageName`"")) {
            $content = $content -replace "<PackageReference Include=`"$packageName`".*?>", "<PackageReference Include=`"$packageName`" Version=`"$newVersion`"/>"
            }
    }
}
    Set-Content -Path $csprojFile.FullName -Value $content
    Write-Output "Successfully upgrade latest packages: $($csprojFile.FullName) with content $($content)"
    # Restore packages after updating
    &dotnet restore $csprojFile.FullName
    &dotnet clean $csprojFile.FullName
    &dotnet build $csprojFile.FullName
    Write-Output "Successfully restored latest packages: $($csprojFile.FullName)"
}