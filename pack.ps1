$ScriptDir = Split-Path $script:MyInvocation.MyCommand.Path

$branch = git -C $ScriptDir symbolic-ref -q --short HEAD
if ($branch.startswith("monorepo-")) { $branch = "mono-" + $branch.substring(9) }
$suffix = "$branch-{0:d5}" -f [int](git -C $ScriptDir rev-list --count HEAD)
echo $suffix

$solutions = 
    "vm\neo-vm.sln", 
    "core\neo.sln", 
    "modules\neo-modules.sln", 
    "devpack\src\Neo.Compiler.CSharp\Neo.Compiler.CSharp.csproj",
    "devpack\src\Neo.SmartContract.Framework\Neo.SmartContract.Framework.csproj"

del "$ScriptDir/artifacts" -Recurse -Force -ErrorAction SilentlyContinue
$solutions | %{ 
    write-host $_ -ForegroundColor Cyan; 
    dotnet pack -o "$ScriptDir/artifacts" --version-suffix $suffix "$ScriptDir/$_" 
}