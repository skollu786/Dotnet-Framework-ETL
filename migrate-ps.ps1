# Define the path to the folder where you want to search for .csproj files
$folderPath = "C:\Personal\Projects\ChoETL-master-old"

# Get all .csproj files in the specified folder and its subfolders
$csprojFiles = Get-ChildItem -Path $folderPath -Recurse -Filter *.csproj

# Define a hashtable for package reference replacements
$packageMappings = @{
    'System.Data'                            = 'System.Data.Common'
    'System.Data.SqlClient'                  = 'Microsoft.Data.SqlClient'
'System.Configuration'                   = 'Microsoft.Extensions.Configuration'
'System.ComponentModel.Annotations'                   = 'notfound'
'System.Data.SQLite.EF6'   = 'notfound'
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


# Loop through each .csproj file and execute a process (e.g., dotnet build)
foreach ($csprojFile in $csprojFiles) {
if(-not $csprojFile.FullName.Contains("SourceGenerator.csproj")) {

    # Output the path of the current .csproj file
    #Write-Output "Processing file: $($csprojFile.FullName)"   

    
    # Execute the process for the .csproj file (e.g., dotnet build)
    # Replace the following command with your desired process
    & upgrade-assistant upgrade $csprojFile.FullName --targetFramework net7.0 --operation Inplace --non-interactive

    # Optional: Check the exit code or handle errors
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

    $xml.Save($csprojFile.FullName)

    # Select all <PackageReference> nodes
    $packageReferencesNodes = $xml.Project.ItemGroup.PackageReference
$content = Get-Content -Path $csprojFile.FullName -Raw
    
    foreach ($node in $packageReferencesNodes) {
        $packageName = $node.Include
        $packageVersion = $node.Version
if($packageName -ne $null -and $packageMappings[$packageName] -ne $null) {
        $newPackage = $packageMappings[$packageName]
     if ($newPackage -ne $null -and $newPackage -ne 'notfound') {
            Write-Output "Updating package reference: $packageName to $newPackage in $($csprojFile.FullName)"
            $content = $content -replace "<PackageReference Include=`"$packageName`".*?>", "<PackageReference Include=`"$newPackage`" Version=`"5.0.0`"/>"
        }
      if ($newPackage -ne $null -and $newPackage -eq 'notfound') {
            Write-Output "package reference not found removing: $packageName to $newPackage in $($csprojFile.FullName)"
            $content = $content -replace "<PackageReference Include=`"$packageName`".*?>", ""
        }	        
    }
}
# Save the updated content back to the .csproj file
$content = $content -replace "<UseWPF>true</UseWPF>", "<UseWPF>false</UseWPF>"
$content = $content -replace "<TargetFramework>net7.0-windows</TargetFramework>", "<TargetFramework>net7.0</TargetFramework>"
$content = $content -replace "<TargetFramework>net7.0</TargetFramework>", "<TargetFramework>net7.0</TargetFramework>`r`n<EmitCompilerGeneratedFiles>true</EmitCompilerGeneratedFiles>"
    Set-Content -Path $csprojFile.FullName -Value $content
    Write-Output "Successfully updated packages: $($csprojFile.FullName) with content $($content)"
    
}
}
# Define the directory containing the .cs files

# Define a mapping of old namespaces to new namespaces
$namespaceMapping = @{
    "System.ComponentModel.Annotations" = "System.ComponentModel.DataAnnotations"
    "System.Configuration.ConfigurationManager" = "System.Configuration.ConfigurationManager"    
    "System.CodeDom" = "System.CodeDom.Compiler"    
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
"System.Data" = "System.Data.Common"
    "NUnit" = "NUnit"
    "NUnit3TestAdapter" = "NUnit3TestAdapter"
    "System.Collections.Concurrent" = "System.Collections.Concurrent"
    "System.Collections.Generic" = "System.Collections.Generic"
"Microsoft.Data.Sqlite.EF6" = "Microsoft.EntityFrameworkCore.Sqlite"
"System.Configuration" = "Microsoft.Extensions.Configuration"
"System.Diagnostics.Contracts" = "System.Diagnostics"
"System.Runtime.Serialization.Formatters.Binary" = "System.Text.Json"
"Microsoft.CSharp" = "Microsoft.CSharp"
"Microsoft.CSharp.RuntimeBinder" = "Microsoft.CSharp.RuntimeBinder"
"Microsoft.Extensions.Configuration" = "Microsoft.Extensions.Configuration"
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
    
    # Read the content of the .cs file
    $cscontent = Get-Content -Path $file.FullName -Raw

$matches = [regex]::Matches($cscontent, $usingPattern)
    
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
Write-Output "namespace found : $usingNamespace to $newNamespace"
if ($newNamespace -ne $null -and $newNamespace.Trim() -ne "" ) {
            # Replace old namespaces with new namespaces
		Write-Output "namespace updated : $usingNamespace to $newNamespace"
            $cscontent = $cscontent -replace $usingNamespace, $newNamespace
        } else {
            Write-Output "No direct equivalent for namespace: $usingNamespace. Skipping replacement."
        }
    }  

    # Save the updated content back to the .cs file
    Set-Content -Path $file.FullName -Value $cscontent
Write-Output "Namespace update completed.$($file.FullName) with content $($cscontent)"
}

Write-Output "Namespaces are update completed."
foreach ($csprojFile in $csprojFiles) {
$content = Get-Content -Path $csprojFile.FullName -Raw
$outdatedPackages = dotnet list $csprojFile.FullName package --outdated

# Extract package names and new versions
$outdatedPackages | ForEach-Object {
    if ($_ -match '(\S+)\s+(\S+)\s+(\S+)') {
        $packageName = $matches[1]
        $newVersion = $matches[3]
        Write-Output "Updating packages $packageName to $newVersion"
	dotnet remove $csprojFile.FullName package $packageName 
        dotnet add $csprojFile.FullName package $packageName --version $newVersion
    }
}


Set-Content -Path $csprojFile.FullName -Value $content
    Write-Output "Successfully upgrade latest packages: $($csprojFile.FullName) with content $($content)"
# Restore packages after updating
&dotnet restore $csprojFile.FullName

#$projectDirectory = Split-Path $csprojFile.FullName
        #Set-Location -Path $projectDirectory
        # Run dotnet format on the .csproj file
        #&dotnet format $csprojFile.FullName
#&roslynator fix $csprojFile.FullName
#Write-Output "Successfully ran roslynator fix"
#&dotnet format $csprojFile.FullName
Write-Output "Successfully ran dotnet format"
}