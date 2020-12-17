param([switch]$merge)

$currentBranch = git symbolic-ref -q --short HEAD 2> $null

# if ($currentBranch -ne $branch) {
#     throw "wrong branch $branch"
# }

# vm project tagged preview 4 at 27dd6758291cf6cf64390598707f98faac3845c0
# core project tagged preview 4 at 414dab140025f46898a3afee3a59a4e18b72d2b2
# exclude from subtree pull for now
# $projects = "core","devpack","modules"#,"node" ,"vm"

$projects = @{ 
    core = "414dab140025f46898a3afee3a59a4e18b72d2b2"; 
    vm = "27dd6758291cf6cf64390598707f98faac3845c0"; 
    devpack = "master";
    modules = "master";
    node = "master" }

git fetch --all
foreach ($prj in $projects.keys) {
    git subtree pull --prefix $prj "official-$prj" "$($projects[$prj])" --squash
}

if ($merge) {
    git checkout "monorepo-$currentBranch"
    git merge $currentBranch
}
