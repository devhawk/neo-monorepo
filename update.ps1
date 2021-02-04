param([string]$branch = "preview5", [switch]$merge)

$currentBranch = git symbolic-ref -q --short HEAD 2> $null

if ($currentBranch -ne $branch) {
    throw "wrong branch $branch"
}

$projects = @{
    core = "v3.0.0-preview5";
    devpack = "4902638e16f2cfa9df0e75776b3f83b4197a3601";
    modules = "v3.0.0-preview5";
    node = "v3.0.0-preview5";
    vm = "v3.0.0-preview5"
}

git fetch --all
foreach ($kvp in $projects.GetEnumerator()) {
    $prj = $kvp.Key
    $commit = $kvp.Value
    git subtree pull --prefix $prj "official-$prj" $commit --squash
}

if ($merge) {
    git push
    git checkout "monorepo-$branch"
    git merge $branch
}
