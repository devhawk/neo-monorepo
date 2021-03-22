param([string]$branch = "rc1", [switch]$merge)

$currentBranch = git symbolic-ref -q --short HEAD 2> $null

if ($currentBranch -ne $branch) {
    throw "wrong branch $branch"
}

$projects = @{
    core = "v3.0.0-rc1";
    devpack = "v3.0.0-rc1";
    modules = "v3.0.0-rc1";
    node = "v3.0.0-rc1";
    vm = "v3.0.0-rc1"
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
