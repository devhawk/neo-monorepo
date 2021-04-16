$solutions = "vm\neo-vm.sln",
    "core\neo.sln",
    "modules\neo-modules.sln",
    "devpack\neo-devpack-dotnet.sln".
    "devpack-msil\neo-devpack-dotnet.sln",
    "node\neo-node.sln"

$solutions | %{write-host $_ -ForegroundColor Cyan; dotnet build $_}