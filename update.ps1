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

# since NEON is moved to a different branch, need to handle it special
write-host devpack-msil -ForegroundColor Cyan;
git subtree pull --prefix devpack-msil official-devpack msil --squash 

if ($merge) {
    write-host $"Merging $branch into monorepo-$branch" -ForegroundColor Cyan; 
    git push
    git checkout "monorepo-$branch"
    git merge $branch
}
