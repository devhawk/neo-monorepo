param([string]$branch = "master", [switch]$merge)

$currentBranch = git symbolic-ref -q --short HEAD 2> $null

if ($currentBranch -ne $branch) {
    throw "wrong branch $branch"
}

$projects = "core","devpack","modules","node" ,"vm"

git fetch --all
foreach ($prj in $projects) {
    write-host $prj -ForegroundColor Cyan;
    git subtree pull --prefix $prj "official-$prj" $branch --squash
}

if ($merge) {
    write-host "Merging $branch into monorepo-$branch" -ForegroundColor Cyan; 
    git push
    git checkout "monorepo-$branch"
    git merge $branch
}
