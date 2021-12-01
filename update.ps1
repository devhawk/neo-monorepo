param([string]$branch = "rel-303", [switch]$merge)

$currentBranch = git symbolic-ref -q --short HEAD 2> $null

if ($currentBranch -ne $branch) {
    throw "wrong branch $branch"
}

$projects = @{
    vm = "v3.0.1"
    core = "v3.0.3";
    devpack = "v3.0.3";
    modules = "v3.0.3";
    node = "v3.0.3";
}

git fetch --all
foreach ($kvp in $projects.GetEnumerator()) {
    $prj = $kvp.Key
    $commit = $kvp.Value
    git subtree pull --prefix $prj "official-$prj" $commit --squash
}

if ($merge) {
    write-host "Merging $branch into monorepo-$branch" -ForegroundColor Cyan; 
    git push
    git checkout "monorepo-$branch"
    git merge $branch
}
