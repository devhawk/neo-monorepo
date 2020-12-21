param([string]$branch = "master", [switch]$merge)

$currentBranch = git symbolic-ref -q --short HEAD 2> $null

if ($currentBranch -ne $branch) {
    throw "wrong branch $branch"
}

$projects = "core","devpack","modules","node" ,"vm"

git fetch --all
foreach ($prj in $projects) {
    git subtree pull --prefix $prj "official-$prj" $branch --squash
}

if ($merge) {
    git checkout "monorepo-$branch"
    git merge $branch
}
