$solutions = "vm\neo-vm.sln","core\neo.sln","modules\neo-modules.sln","devpack\neo-devpack-dotnet.sln" ,"node\neo-node.sln"
$solutions | %{dotnet build $_}